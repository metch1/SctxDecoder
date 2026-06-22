namespace SctxDecoder;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: SctxDecoder <input.sctx> [output.png]");
            Console.WriteLine("       SctxDecoder <input1.sctx> <input2.sctx> ... -o <outputDir>");
            return;
        }

        if (args.Length > 2 || (args.Length == 2 && args[1] == "-o"))
        {
            var files = new List<string>();
            string outputDir = null;
            int i;
            for (i = 0; i < args.Length; i++)
            {
                if (args[i] == "-o") { outputDir = args[i + 1]; break; }
                if (File.Exists(args[i])) files.Add(args[i]);
                else if (Directory.Exists(args[i]))
                    files.AddRange(Directory.GetFiles(args[i], "*.sctx"));
            }
            ProcessBatch(files, outputDir);
        }
        else
        {
            string input = args[0];
            string output = args.Length > 1 ? args[1] : Path.GetFileNameWithoutExtension(input) + ".png";
            ProcessSingle(input, output);
        }
    }

    static void ProcessSingle(string input, string output)
    {
        try
        {
            var sctx = new SCTX(input);
            Texture tex = sctx.Texture ?? sctx.StreamingTexture;
            if (tex == null) { /* */ }

            var (imageData, mode) = sctx.DecodeTexture(tex);
            if (imageData == null && sctx.DecompressedPayload != null)
            {
                (imageData, mode) = sctx.DecodeTexture(tex, true);
            }
            if (imageData != null)
            {

                if (mode == "RGBA")
                {
                    using var img = Image.LoadPixelData<Rgba32>(imageData, tex.Width, tex.Height);
                    img.SaveAsPng(output);
                }
                else if (mode == "RGB")
                {
                    var rgba = new byte[imageData.Length / 3 * 4];
                    for (int i = 0, j = 0; i < imageData.Length; i += 3, j += 4)
                    {
                        rgba[j] = imageData[i];
                        rgba[j + 1] = imageData[i + 1];
                        rgba[j + 2] = imageData[i + 2];
                        rgba[j + 3] = 255;
                    }
                    using var img = Image.LoadPixelData<Rgba32>(rgba, tex.Width, tex.Height);
                    img.SaveAsPng(output);
                }
                Console.WriteLine($"Saved: {output}");
            }
        }
        catch (Exception) { /* */ }
    }

    static void ProcessBatch(List<string> files, string outputDir)
    {
        if (outputDir != null) Directory.CreateDirectory(outputDir);
        _ = Parallel.ForEach(files, file =>
        {
            string outName = Path.GetFileNameWithoutExtension(file) + ".png";
            string outPath = outputDir != null ? Path.Combine(outputDir, outName) : outName;
            try
            {
                var sctx = new SCTX(file);
                Texture tex = sctx.Texture ?? sctx.StreamingTexture;
                var (imgData, mode) = sctx.DecodeTexture(tex);
                if (imgData == null && sctx.DecompressedPayload != null)
                    (imgData, mode) = sctx.DecodeTexture(tex, true);
                if (imgData != null)
                {
                    if (mode == "RGBA")
                    {
                        using var img = Image.LoadPixelData<Rgba32>(imgData, tex.Width, tex.Height);
                        img.SaveAsPng(outPath);
                    }
                    else if (mode == "RGB")
                    {
                        var rgba = new byte[imgData.Length / 3 * 4];
                        for (int i = 0, j = 0; i < imgData.Length; i += 3, j += 4)
                        {
                            rgba[j] = imgData[i];
                            rgba[j + 1] = imgData[i + 1];
                            rgba[j + 2] = imgData[i + 2];
                            rgba[j + 3] = 255;
                        }
                        using var img = Image.LoadPixelData<Rgba32>(rgba, tex.Width, tex.Height);
                        img.SaveAsPng(outPath);
                    }
                }
            }
            catch (Exception) { /*  */ }
        });
    }
}
