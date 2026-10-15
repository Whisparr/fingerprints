using System;
using System.Buffers.Binary;

namespace Whisparr.Fingerprints.Internal;

/// <summary>
/// Minimal BMP reader covering what ffmpeg's bmp encoder emits for video frames:
/// uncompressed 24- and 32-bit rows. Mirrors golang.org/x/image/bmp, which stash uses
/// to decode the same bytes.
/// </summary>
internal static class Bmp
{
    private const int FileHeaderSize = 14;
    private const int BiRgb = 0;
    private const int BiBitfields = 3;

    /// <returns>Non-premultiplied RGBA (alpha forced opaque), rows packed top-down.</returns>
    public static Frame Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length < FileHeaderSize + 40 || data[0] != 'B' || data[1] != 'M')
        {
            throw new FormatException("Not a BMP image");
        }

        var pixelOffset = BinaryPrimitives.ReadInt32LittleEndian(data[10..]);
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(data[14..]);
        var width = BinaryPrimitives.ReadInt32LittleEndian(data[18..]);
        var height = BinaryPrimitives.ReadInt32LittleEndian(data[22..]);
        var bpp = BinaryPrimitives.ReadUInt16LittleEndian(data[28..]);
        var compression = BinaryPrimitives.ReadInt32LittleEndian(data[30..]);

        if (headerSize < 40)
        {
            throw new FormatException($"Unsupported BMP header size {headerSize}");
        }

        // A negative height means rows are stored top-down
        var topDown = height < 0;
        height = Math.Abs(height);

        if (width <= 0 || height == 0)
        {
            throw new FormatException($"Invalid BMP dimensions {width}x{height}");
        }

        var bytesPerPixel = (bpp, compression) switch
        {
            (24, BiRgb) => 3,
            (32, BiRgb) => 4,
            (32, BiBitfields) => 4,
            _ => throw new NotSupportedException($"Unsupported BMP format: {bpp} bpp, compression {compression}")
        };

        // Rows are padded to 4 bytes
        var rowSize = ((bytesPerPixel * width) + 3) & ~3;

        if (pixelOffset < FileHeaderSize + headerSize || data.Length < pixelOffset + ((long)rowSize * height))
        {
            throw new FormatException("Truncated BMP image");
        }

        var pixels = new byte[width * height * 4];

        for (var row = 0; row < height; row++)
        {
            var y = topDown ? row : height - 1 - row;
            var source = data.Slice(pixelOffset + (row * rowSize), bytesPerPixel * width);
            var target = pixels.AsSpan(y * width * 4, width * 4);

            for (int i = 0, j = 0; i < target.Length; i += 4, j += bytesPerPixel)
            {
                // BMP stores BGR(A)
                target[i + 0] = source[j + 2];
                target[i + 1] = source[j + 1];
                target[i + 2] = source[j + 0];
                target[i + 3] = 0xff;
            }
        }

        return new Frame(width, height, pixels);
    }
}

internal sealed record Frame(int Width, int Height, byte[] Pixels);
