namespace SctxDecoder
{
    public static class TextureDecoder
    {
        public static bool DecodeDXT1(byte[] data, int width, int height, byte[] image)
            => DxtDecoder.DecompressDXT1<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeDXT5(byte[] data, int width, int height, byte[] image)
            => DxtDecoder.DecompressDXT5<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodePVRTC(byte[] data, int width, int height, byte[] image, bool is2bpp)
            => PvrtcDecoder.DecompressPVRTC<ColorBGRA<byte>, byte>(data, width, height, is2bpp, image) >= 0;

        public static bool DecodeETC1(byte[] data, int width, int height, byte[] image)
            => EtcDecoder.DecompressETC<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeETC2(byte[] data, int width, int height, byte[] image)
            => EtcDecoder.DecompressETC2<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeETC2A1(byte[] data, int width, int height, byte[] image)
            => EtcDecoder.DecompressETC2A1<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeETC2A8(byte[] data, int width, int height, byte[] image)
            => EtcDecoder.DecompressETC2A8<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeEACR(byte[] data, int width, int height, byte[] image)
            => EtcDecoder.DecompressEACRUnsigned<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeEACRSigned(byte[] data, int width, int height, byte[] image)
            => EtcDecoder.DecompressEACRSigned<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeEACRG(byte[] data, int width, int height, byte[] image)
            => EtcDecoder.DecompressEACRGUnsigned<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeEACRGSigned(byte[] data, int width, int height, byte[] image)
            => EtcDecoder.DecompressEACRGSigned<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeBC4(byte[] data, int width, int height, byte[] image)
            => Bc4.Decompress<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeBC5(byte[] data, int width, int height, byte[] image)
            => Bc5.Decompress<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeBC6(byte[] data, int width, int height, byte[] image)
            => Bc6h.Decompress<ColorBGRA<byte>, byte>(data, width, height, false, image) >= 0;

        public static bool DecodeBC7(byte[] data, int width, int height, byte[] image)
            => Bc7.Decompress<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeATCRGB4(byte[] data, int width, int height, byte[] image)
            => AtcDecoder.DecompressAtcRgb4<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeATCRGBA8(byte[] data, int width, int height, byte[] image)
            => AtcDecoder.DecompressAtcRgba8<ColorBGRA<byte>, byte>(data, width, height, image) >= 0;

        public static bool DecodeASTC(byte[] data, int width, int height, int blockWidth, int blockHeight, byte[] image)
            => AstcDecoder.DecodeASTC<ColorBGRA<byte>, byte>(data, width, height, blockWidth, blockHeight, image) >= 0;
    }
}
