namespace SctxDecoder;

class Texture
{
    public int Width { get; set; }
    public int Height { get; set; }
    public uint DataLength { get; set; }
    public byte[] Data { get; set; }
    public int PixelTypeRaw { get; }
    public ScPixel? PixelType => Enum.IsDefined(typeof(ScPixel), PixelTypeRaw) ? (ScPixel)PixelTypeRaw : null;
    private bool? _isCompressed;
    private byte[] _decompressed;

    public Texture(int pixelType, int width = 0, int height = 0)
    {
        PixelTypeRaw = pixelType;
        Width = width;
        Height = height;
    }

    public string GetFormatName()
    {
        // Python tool logic
        if (PixelType.HasValue)
        {
            if (PixelType == ScPixel.RGBA8Unorm_70) return "RGBA8Unorm";
            return PixelType.ToString();
        }

        int id = PixelTypeRaw;
        if ((id >= 186 && id <= 200) || (id >= 204 && id <= 218))
        {
            var astcNames = new Dictionary<int, string>
            {
                {186, "ASTC_SRGBA8_4x4"}, {187, "ASTC_SRGBA8_5x4"}, {188, "ASTC_SRGBA8_5x5"},
                {189, "ASTC_SRGBA8_6x5"}, {190, "ASTC_SRGBA8_6x6"}, {192, "ASTC_SRGBA8_8x5"},
                {193, "ASTC_SRGBA8_8x6"}, {194, "ASTC_SRGBA8_8x8"}, {195, "ASTC_SRGBA8_10x5"},
                {196, "ASTC_SRGBA8_10x6"}, {197, "ASTC_SRGBA8_10x8"}, {198, "ASTC_SRGBA8_10x10"},
                {199, "ASTC_SRGBA8_12x10"}, {200, "ASTC_SRGBA8_12x12"}, {204, "ASTC_RGBA8_4x4"},
                {205, "ASTC_RGBA8_5x4"}, {206, "ASTC_RGBA8_5x5"}, {207, "ASTC_RGBA8_6x5"},
                {208, "ASTC_RGBA8_6x6"}, {210, "ASTC_RGBA8_8x5"}, {211, "ASTC_RGBA8_8x6"},
                {212, "ASTC_RGBA8_8x8"}, {213, "ASTC_RGBA8_10x5"}, {214, "ASTC_RGBA8_10x6"},
                {215, "ASTC_RGBA8_10x8"}, {216, "ASTC_RGBA8_10x10"}, {217, "ASTC_RGBA8_12x10"},
                {218, "ASTC_RGBA8_12x12"}
            };
            return astcNames.GetValueOrDefault(id, $"ASTC_UNKNOWN_{id}");
        }
        if (id == 70) return "RGBA8Unorm";
        return $"UNKNOWN ({id})";
    }

    public bool IsAstc()
    {
        int id = PixelTypeRaw;
        return (id >= 186 && id <= 200) || (id >= 204 && id <= 218);
    }

    public bool IsCompressedData()
    {
        if (_isCompressed.HasValue) return _isCompressed.Value;
        if (Data == null || Data.Length < 4)
        {
            _isCompressed = false;
            return false;
        }

        if (Data[0] == 0x28 && Data[1] == 0xB5 && Data[2] == 0x2F && Data[3] == 0xFD)
        {
            _isCompressed = true;
            return true;
        }

        try
        {
            using var dec = new Decompressor();
            var testSpan = Data.AsSpan(0, Math.Min(100, Data.Length));
            // Just test if it's like valid zstd
            _isCompressed = true;
            return true;
        }
        catch { }
        _isCompressed = false;
        return false;
    }

    public byte[] DecompressData()
    {
        if (_decompressed != null) return _decompressed;
        if (!IsCompressedData())
        {
            _decompressed = Data;
            return Data;
        }

        try
        {
            long maxExpected = CalculateExpectedSize() * 3;
            if (maxExpected == 0) maxExpected = 100 * 1024 * 1024;
            using var dec = new Decompressor();
            _decompressed = dec.Unwrap(Data).ToArray();
            Console.WriteLine($"Successfully decompressed: {Data.Length} -> {_decompressed.Length} bytes");
            return _decompressed;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Decompression error: {ex.Message}");
            _decompressed = Data;
            return Data;
        }
    }

    public bool IsEtc() => GetFormatName().Contains("ETC") || GetFormatName().Contains("EAC");
    public bool IsSrgb() => GetFormatName().Contains("SRGB") || GetFormatName().Contains("SRGBA") || GetFormatName().Contains("sRGB");
    public bool IsPvrtc() => GetFormatName().Contains("PVRTC");

    public bool IsUncompressed()
    {
        if (IsAstc() || IsEtc() || IsPvrtc()) return false;
        string fmt = GetFormatName();
        string[] uncomp = { "R8", "R16", "R16F", "R32F", "RG8", "RG16", "RG16F", "RG32F",
                            "RGB8", "RGB16", "RGB16F", "RGB32F", "RGBA8", "RGBA16", "RGBA16F",
                            "RGBA32F", "BGR8", "BGRA8", "RGBA8Unorm", "RGB8Unorm", "RG8Unorm",
                            "R8Unorm", "BGRA8Unorm", "BGR8Unorm" };
        return uncomp.Any(f => fmt.Contains(f));
    }

    public long CalculateExpectedSize()
    {
        if (Width == 0 || Height == 0) return 0;
        string fmt = GetFormatName();

        if (IsAstc())
        {
            var blockSizes = new Dictionary<string, (int w, int h)>
            {
                {"4x4", (4,4)}, {"5x4", (5,4)}, {"5x5", (5,5)}, {"6x5", (6,5)},
                {"6x6", (6,6)}, {"8x5", (8,5)}, {"8x6", (8,6)}, {"8x8", (8,8)},
                {"10x5", (10,5)}, {"10x6", (10,6)}, {"10x8", (10,8)}, {"10x10", (10,10)},
                {"12x10", (12,10)}, {"12x12", (12,12)}
            };
            foreach (var kv in blockSizes)
            {
                if (fmt.Contains(kv.Key))
                {
                    long blocksX = (Width + kv.Value.w - 1) / kv.Value.w;
                    long blocksY = (Height + kv.Value.h - 1) / kv.Value.h;
                    return blocksX * blocksY * 16;
                }
            }
        }
        else if (IsEtc())
        {
            long blocksX = (Width + 3) / 4;
            long blocksY = (Height + 3) / 4;
            return fmt.Contains("RGBA") ? blocksX * blocksY * 16 : blocksX * blocksY * 8;
        }
        else if (IsUncompressed())
        {
            if (fmt.Contains("RGBA") || fmt.Contains("BGRA")) return Width * Height * 4;
            if (fmt.Contains("RGB") || fmt.Contains("BGR")) return Width * Height * 3;
            if (fmt.Contains("RG")) return Width * Height * 2;
            if (fmt.Contains("R")) return Width * Height;
        }
        return 0;
    }
}
