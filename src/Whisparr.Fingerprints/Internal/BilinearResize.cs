using System;

namespace Whisparr.Fingerprints.Internal;

/// <summary>
/// Port of nfnt/resize <c>83c6a99</c> (ISC, Copyright 2012 Jan Schlicht) for the one
/// path phash uses: <c>Resize(w, h, *image.NRGBA, Bilinear)</c>.
/// </summary>
/// <remarks>
/// Two separable passes with 8-bit fixed-point weights (<c>createWeights8</c>). The first
/// pass reads the NRGBA source and writes a transposed RGBA temp image; the second
/// resizes that temp image the other way. Integer division truncates in both languages.
/// </remarks>
internal static class BilinearResize
{
    private const int Taps = 2;
    private const double Blur = 1.0;

    /// <param name="src">Non-premultiplied RGBA, 4 bytes per pixel, rows packed.</param>
    /// <returns>Premultiplied RGBA (Go <c>*image.RGBA</c>), <paramref name="width"/> x <paramref name="height"/>.</returns>
    public static byte[] Resize(byte[] src, int srcWidth, int srcHeight, int width, int height)
    {
        if (src.Length != srcWidth * srcHeight * 4)
        {
            throw new ArgumentException("Pixel buffer does not match dimensions", nameof(src));
        }

        if (width == srcWidth && height == srcHeight)
        {
            return (byte[])src.Clone();
        }

        var scaleX = (double)srcWidth / width;
        var scaleY = (double)srcHeight / height;

        // temp is srcHeight wide and width tall: x indexes source rows, y the new columns
        var temp = new byte[srcHeight * width * 4];
        var (coeffs, offset, filterLength) = CreateWeights8(width, scaleX);
        ResizePass(src, srcWidth, srcWidth * 4, temp, srcHeight, width, coeffs, offset, filterLength, premultiply: true);

        var result = new byte[width * height * 4];
        (coeffs, offset, filterLength) = CreateWeights8(height, scaleY);
        ResizePass(temp, srcHeight, srcHeight * 4, result, width, height, coeffs, offset, filterLength, premultiply: false);

        return result;
    }

    private static (short[] Coeffs, int[] Offset, int FilterLength) CreateWeights8(int dy, double scale)
    {
        var filterLength = Taps * (int)Math.Max(Math.Ceiling(Blur * scale), 1);
        var filterFactor = Math.Min(1.0 / (Blur * scale), 1);

        var coeffs = new short[dy * filterLength];
        var start = new int[dy];

        for (var y = 0; y < dy; y++)
        {
            var interpX = (scale * (y + 0.5)) - 0.5;
            start[y] = (int)interpX - (filterLength / 2) + 1;
            interpX -= start[y];

            for (var i = 0; i < filterLength; i++)
            {
                var input = (interpX - i) * filterFactor;
                coeffs[(y * filterLength) + i] = (short)(Linear(input) * 256);
            }
        }

        return (coeffs, start, filterLength);
    }

    private static double Linear(double input)
    {
        input = Math.Abs(input);
        return input <= 1 ? 1 - input : 0;
    }

    // resizeNRGBA (premultiply) / resizeRGBA. inWidth is the source row length in pixels.
    private static void ResizePass(byte[] input, int inWidth, int inStride, byte[] output, int outWidth, int outHeight, short[] coeffs, int[] offset, int filterLength, bool premultiply)
    {
        var maxX = inWidth - 1;
        var outStride = outWidth * 4;

        for (var x = 0; x < outWidth; x++)
        {
            var row = x * inStride;

            for (var y = 0; y < outHeight; y++)
            {
                int r = 0, g = 0, b = 0, a = 0, sum = 0;
                var start = offset[y];
                var ci = y * filterLength;

                for (var i = 0; i < filterLength; i++)
                {
                    int coeff = coeffs[ci + i];

                    if (coeff == 0)
                    {
                        continue;
                    }

                    var xi = start + i;

                    if ((uint)xi < (uint)maxX)
                    {
                        xi *= 4;
                    }
                    else if (xi >= maxX)
                    {
                        xi = 4 * maxX;
                    }
                    else
                    {
                        xi = 0;
                    }

                    int pr = input[row + xi + 0];
                    int pg = input[row + xi + 1];
                    int pb = input[row + xi + 2];
                    int pa = input[row + xi + 3];

                    if (premultiply)
                    {
                        pr = pr * pa / 0xff;
                        pg = pg * pa / 0xff;
                        pb = pb * pa / 0xff;
                    }

                    r += coeff * pr;
                    g += coeff * pg;
                    b += coeff * pb;
                    a += coeff * pa;
                    sum += coeff;
                }

                var xo = (y * outStride) + (x * 4);

                output[xo + 0] = ClampUint8(r / sum);
                output[xo + 1] = ClampUint8(g / sum);
                output[xo + 2] = ClampUint8(b / sum);
                output[xo + 3] = ClampUint8(a / sum);
            }
        }
    }

    private static byte ClampUint8(int value)
    {
        if ((uint)value < 256)
        {
            return (byte)value;
        }

        return value > 255 ? (byte)255 : (byte)0;
    }
}
