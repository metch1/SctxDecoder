namespace SctxDecoder;

class SCTX
{
    public int Width, Height;
    public Texture StreamingTexture { get; private set; }
    public Texture Texture { get; private set; }
    public byte[] OriginalFileData { get; }
    public byte[] CompressedPayload { get; private set; }
    public byte[] DecompressedPayload { get; private set; }

    public SCTX(string filePath)
    {
        OriginalFileData = File.ReadAllBytes(filePath);
        FindAndDecompressPayload();

        var reader = new DataReader(OriginalFileData);
        uint streamingLength = reader.ReadUInt();
        if (streamingLength > OriginalFileData.Length - reader.Position)
            streamingLength = (uint)(OriginalFileData.Length - reader.Position);
        byte[] streamingData = reader.ReadBytes((int)streamingLength);
        ReadStreamingData(streamingData);

        if (reader.CanRead(4))
        {
            uint dataLength = reader.ReadUInt();
            if (dataLength > OriginalFileData.Length - reader.Position)
                dataLength = (uint)(OriginalFileData.Length - reader.Position);
            byte[] data = reader.ReadBytes((int)dataLength);
            ReadTexture(data);

            if (Texture != null && Texture.DataLength > 0)
            {
                if (reader.CanRead((int)Texture.DataLength))
                    Texture.Data = reader.ReadBytes((int)Texture.DataLength);
                else
                    Texture.Data = reader.ReadBytes((int)(OriginalFileData.Length - reader.Position));
            }
        }

        // If main texture data is too small, try streaming
        if (Texture != null && (Texture.Data == null || Texture.Data.Length < 16))
        {
            if (StreamingTexture?.Data != null)
            {
                Texture.Data = StreamingTexture.Data;
                Texture.DataLength = StreamingTexture.DataLength;
            }
        }
    }

    private void FindAndDecompressPayload()
    {
        byte[] magic = { 0x28, 0xB5, 0x2F, 0xFD };
        int pos = IndexOf(OriginalFileData, magic, 0);
        if (pos == -1) return;

        CompressedPayload = OriginalFileData[pos..];
        try
        {
            using var dec = new Decompressor();
            DecompressedPayload = dec.Unwrap(CompressedPayload).ToArray();
        }
        catch (Exception) { /* ignored */}
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int start = 0)
    {
        for (int i = start; i <= haystack.Length - needle.Length; i++)
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle))
                return i;
        return -1;
    }

    private void ReadStreamingData(byte[] data)
    {
        if (data.Length < 4) return;
        var reader = new DataReader(data);
        uint headerLen = reader.ReadUInt();
        if (headerLen > data.Length - 4) return;
        reader.Skip((int)headerLen);
        if (!reader.CanRead(14)) return;

        int pixelType = (int)reader.ReadUInt();
        int width = reader.ReadUShort();
        int height = reader.ReadUShort();
        reader.ReadInt(); // skip

        if (IsAstcId(pixelType) || pixelType == 70)
        {
            Texture = Enum.IsDefined(typeof(ScPixel), pixelType)
                ? new Texture(pixelType, width, height)
                : new Texture(pixelType, width, height);
        }
        else
        {
            Texture = new Texture(pixelType, width, height);
        }

        if (reader.CanRead(4))
        {
            Texture.DataLength = reader.ReadUInt();
        }

        if (reader.CanRead(20))
        {
            reader.Skip(16);
            if (reader.CanRead(4))
            {
                uint streamTexLen = reader.ReadUInt();
                if (streamTexLen > 0 && reader.CanRead((int)streamTexLen))
                    ReadStreamingTexture(reader.ReadBytes((int)streamTexLen));
            }
            if (reader.CanRead(4))
                reader.ReadUInt(); // StreamingId
        }
    }

    private void ReadStreamingTexture(byte[] data)
    {
        if (data.Length < 40) return; // 28 + 12 minimum
        var reader = new DataReader(data);
        reader.Skip(28);
        int width = reader.ReadUShort();
        int height = reader.ReadUShort();
        int pixelType = (int)reader.ReadUInt();
        reader.ReadInt();

        if (IsAstcId(pixelType) || pixelType == 70)
            StreamingTexture = new Texture(pixelType, width, height);
        else
            StreamingTexture = new Texture(pixelType, width, height);

        if (reader.CanRead(4))
        {
            StreamingTexture.DataLength = reader.ReadUInt();
            if (StreamingTexture.DataLength > 0 && reader.CanRead((int)StreamingTexture.DataLength))
                StreamingTexture.Data = reader.ReadBytes((int)StreamingTexture.DataLength);
        }
    }

    private void ReadTexture(byte[] data)
    {
        if (data.Length < 34) return; // 24 + 10
        var reader = new DataReader(data);
        reader.Skip(24);
        if (Texture == null) return; // should not happen
        Texture.Width = reader.ReadUShort();
        Texture.Height = reader.ReadUShort();
        reader.ReadUInt();
        if (reader.CanRead(4))
        {
            uint hashLen = reader.ReadUInt();
            if (hashLen > 0 && reader.CanRead((int)hashLen))
                reader.ReadBytes((int)hashLen); // skip hash
        }
    }

    private static bool IsAstcId(int id) => (id >= 186 && id <= 200) || (id >= 204 && id <= 218);

    public (byte[] imageData, string mode) DecodeTexture(Texture texture, bool useDecompressedPayload = false)
    {
        byte[] textureData = useDecompressedPayload ? DecompressedPayload : texture?.Data;
        if (textureData == null || textureData.Length < 16)
        {
            return (null, null);
        }

        if (!useDecompressedPayload && texture.IsCompressedData())
            textureData = texture.DecompressData();

        int width = texture.Width, height = texture.Height;
        string format = texture.GetFormatName();

        try
        {
            if (texture.IsAstc())
            {
                var match = Regex.Match(format, @"(\d+)x(\d+)");
                if (match.Success)
                {
                    int bw = int.Parse(match.Groups[1].Value), bh = int.Parse(match.Groups[2].Value);
                    byte[] image = new byte[width * height * 4];
                    TextureDecoder.DecodeASTC(textureData, width, height, bw, bh, image);
                    // Convert RGBA to BGRA (python tool reference)
                    byte[] bgra = new byte[image.Length];
                    for (int i = 0; i < image.Length; i += 4)
                    {
                        bgra[i] = image[i + 2];     // B
                        bgra[i + 1] = image[i + 1]; // G
                        bgra[i + 2] = image[i];     // R
                        bgra[i + 3] = image[i + 3]; // A
                    }
                    return (bgra, "RGBA");
                }
            }
            else if (texture.IsEtc())
            {
                byte[] image = new byte[width * height * 4];
                byte[] rgba = null;

                if (format.Contains("ETC1") || format.Contains("ETC2_RGB8") || format.Contains("ETC2_SRGB8"))
                {
                    TextureDecoder.DecodeETC1(textureData, width, height, image);
                    rgba = image;
                }
                else if (format.Contains("ETC2_EAC_RGBA8") || format.Contains("ETC2_EAC_SRGBA8"))
                {
                    TextureDecoder.DecodeETC2(textureData, width, height, image);
                    rgba = image;
                }
                else if (format.Contains("ETC2_RGB8_PUNCHTHROUGH_ALPHA1") || format.Contains("ETC2_SRGB8_PUNCHTHROUGH_ALPHA1"))
                {
                    TextureDecoder.DecodeETC2A1(textureData, width, height, image);
                    rgba = image;
                }
                else if (format.Contains("EAC_SIGNED_R11"))
                {
                    TextureDecoder.DecodeEACRSigned(textureData, width, height, image);
                    rgba = new byte[image.Length * 4]; // EACR outputs width*height bytes
                    for (int i = 0; i < image.Length && i * 4 < rgba.Length; i++)
                    {
                        rgba[i * 4] = image[i];
                        rgba[i * 4 + 1] = image[i];
                        rgba[i * 4 + 2] = image[i];
                        rgba[i * 4 + 3] = 255;
                    }
                }
                else if (format.Contains("EAC_R11"))
                {
                    TextureDecoder.DecodeEACR(textureData, width, height, image);
                    rgba = new byte[image.Length * 4];
                    for (int i = 0; i < image.Length && i * 4 < rgba.Length; i++)
                    {
                        rgba[i * 4] = image[i];
                        rgba[i * 4 + 1] = image[i];
                        rgba[i * 4 + 2] = image[i];
                        rgba[i * 4 + 3] = 255;
                    }
                }
                else if (format.Contains("EAC_SIGNED_RG11"))
                {
                    TextureDecoder.DecodeEACRGSigned(textureData, width, height, image);
                    rgba = new byte[image.Length / 2 * 4];
                    for (int i = 0; i < image.Length; i += 2)
                    {
                        int idx = i / 2 * 4;
                        rgba[idx] = image[i];
                        rgba[idx + 1] = image[i + 1];
                        rgba[idx + 2] = 0;
                        rgba[idx + 3] = 255;
                    }
                }
                else if (format.Contains("EAC_RG11"))
                {
                    TextureDecoder.DecodeEACRG(textureData, width, height, image);
                    rgba = new byte[image.Length / 2 * 4];
                    for (int i = 0; i < image.Length; i += 2)
                    {
                        int idx = i / 2 * 4;
                        rgba[idx] = image[i];
                        rgba[idx + 1] = image[i + 1];
                        rgba[idx + 2] = 0;
                        rgba[idx + 3] = 255;
                    }
                }

                if (rgba != null)
                {
                    byte[] bgra = new byte[rgba.Length];
                    for (int i = 0; i < rgba.Length; i += 4)
                    {
                        bgra[i] = rgba[i + 2];
                        bgra[i + 1] = rgba[i + 1];
                        bgra[i + 2] = rgba[i];
                        bgra[i + 3] = rgba[i + 3];
                    }
                    return (bgra, "RGBA");
                }
            }
            else if (texture.IsUncompressed())
            {
                if (format.Contains("RGBA8Unorm")) return (textureData, "RGBA");
                if (format.Contains("RGBA8") && !format.Contains("Unorm"))
                {
                    byte[] bgra = new byte[textureData.Length];
                    for (int i = 0; i < textureData.Length; i += 4)
                    {
                        bgra[i] = textureData[i + 2];
                        bgra[i + 1] = textureData[i + 1];
                        bgra[i + 2] = textureData[i];
                        bgra[i + 3] = textureData[i + 3];
                    }
                    return (bgra, "RGBA");
                }
                if (format.Contains("BGRA8")) return (textureData, "RGBA");
                if (format.Contains("RGB8") && !format.Contains("SRGB"))
                {
                    byte[] rgb = new byte[textureData.Length];
                    for (int i = 0; i < textureData.Length; i += 3)
                    {
                        rgb[i] = textureData[i];
                        rgb[i + 1] = textureData[i + 1];
                        rgb[i + 2] = textureData[i + 2];
                    }
                    return (rgb, "RGB");
                }
                if (format.Contains("BGR8"))
                {
                    byte[] rgb = new byte[textureData.Length];
                    for (int i = 0; i < textureData.Length; i += 3)
                    {
                        rgb[i] = textureData[i + 2];
                        rgb[i + 1] = textureData[i + 1];
                        rgb[i + 2] = textureData[i];
                    }
                    return (rgb, "RGB");
                }
                if (format.Contains("RG8"))
                {
                    byte[] rgb = new byte[textureData.Length / 2 * 3];
                    for (int i = 0; i < textureData.Length; i += 2)
                    {
                        int idx = i / 2 * 3;
                        rgb[idx] = textureData[i];
                        rgb[idx + 1] = textureData[i + 1];
                        rgb[idx + 2] = 0;
                    }
                    return (rgb, "RGB");
                }
                if (format.Contains("R8"))
                {
                    byte[] rgb = new byte[textureData.Length * 3];
                    for (int i = 0; i < textureData.Length; i++)
                    {
                        rgb[i * 3] = textureData[i];
                        rgb[i * 3 + 1] = textureData[i];
                        rgb[i * 3 + 2] = textureData[i];
                    }
                    return (rgb, "RGB");
                }
            }
            else if (texture.IsPvrtc())
            {
                bool is2bpp = format.Contains("2");
                byte[] image = new byte[width * height * 4];
                TextureDecoder.DecodePVRTC(textureData, width, height, image, is2bpp);
                byte[] bgra = new byte[image.Length];
                for (int i = 0; i < image.Length; i += 4)
                {
                    bgra[i] = image[i + 2];
                    bgra[i + 1] = image[i + 1];
                    bgra[i + 2] = image[i];
                    bgra[i + 3] = image[i + 3];
                }
                return (bgra, "RGBA");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{ex}");
        }
        return (null, null);
    }
}
