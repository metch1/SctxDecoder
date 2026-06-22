

# SctxDecoder

**SctxDecoder** is a lightweight C# CLI tool for decoding **Supercell texture `.sctx` files** into PNG images.

While a C++ decoder/encoder already exists, this project was built with **macOS** and **Linux** users in mind especially for those who may have difficulty compiling the original tool on their system

## Features

-   Decode `.sctx` texture files into `.png`
-   Cross-platform support:
    -   Linux
    -   macOS
    -   Windows
-   Simple CLI
-   Self contained builds available

----------



## How to use

Download the latest binary for your operating system from the GitHub Releases page:
[https://github.com/metch1/SctxDecoder/releases](https://github.com/metch1/SctxDecoder/releases)

Place the executable inside your working directory or add it to your system PATH.

-----

run the following commad:

```
./SctxDecoder <input.sctx> [output.png]
```

If no output file is specified, the tool will generate one automatically.

### Export to a specific directory

```
./SctxDecoder <input.sctx> -o <directory>
```

----

>[!NOTE]
>Currently, **SctxDecoder only supports converting** `**.sctx**` **files to** `**.png**`.


## Credits

This project uses code forked from the original **Sctx-Decoder** repository:

[https://github.com/FearForest/Sctx-Decoder](https://github.com/FearForest/Sctx-Decoder)
