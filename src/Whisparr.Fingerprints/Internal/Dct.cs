using System;

namespace Whisparr.Fingerprints.Internal;

/// <summary>
/// Port of goimagehash v1.1.0 <c>transforms.DCT2DFast64</c> (BSD-2-Clause,
/// Copyright 2017 The goimagehash Authors): DCT-II, unscaled, Byeong Gi Lee's algorithm.
/// </summary>
/// <remarks>
/// Operation order is kept exactly as upstream; reordering changes the low bits of the
/// result, and the hash compares against the median of those results.
/// </remarks>
internal static class Dct
{
    public const int Size = 64;

    public static void Transform2D(double[] pixels)
    {
        if (pixels.Length != Size * Size)
        {
            throw new ArgumentException($"Expected {Size * Size} pixels", nameof(pixels));
        }

        for (var i = 0; i < Size; i++)
        {
            Transform1D(pixels.AsSpan(i * Size, Size));
        }

        Span<double> row = stackalloc double[Size];

        for (var i = 0; i < Size; i++)
        {
            for (var j = 0; j < Size; j++)
            {
                row[j] = pixels[i + (j * Size)];
            }

            Transform1D(row);

            for (var j = 0; j < Size; j++)
            {
                pixels[i + (j * Size)] = row[j];
            }
        }
    }

    // DCT1DFast64
    public static void Transform1D(Span<double> input)
    {
        Span<double> temp = stackalloc double[64];

        for (var i = 0; i < 32; i++)
        {
            double x = input[i], y = input[63 - i];
            temp[i] = x + y;
            temp[i + 32] = (x - y) / DctTables.Dct64[i];
        }

        Forward32(temp[..32]);
        Forward32(temp[32..]);

        for (var i = 0; i < 32 - 1; i++)
        {
            input[(i * 2) + 0] = temp[i];
            input[(i * 2) + 1] = temp[i + 32] + temp[i + 32 + 1];
        }

        input[62] = temp[31];
        input[63] = temp[63];
    }

    private static void Forward32(Span<double> input)
    {
        Span<double> temp = stackalloc double[32];

        for (var i = 0; i < 16; i++)
        {
            double x = input[i], y = input[31 - i];
            temp[i] = x + y;
            temp[i + 16] = (x - y) / DctTables.Dct32[i];
        }

        Forward16(temp[..16]);
        Forward16(temp[16..]);

        for (var i = 0; i < 16 - 1; i++)
        {
            input[(i * 2) + 0] = temp[i];
            input[(i * 2) + 1] = temp[i + 16] + temp[i + 16 + 1];
        }

        input[30] = temp[15];
        input[31] = temp[31];
    }

    private static void Forward16(Span<double> input)
    {
        Span<double> temp = stackalloc double[16];

        for (var i = 0; i < 8; i++)
        {
            double x = input[i], y = input[15 - i];
            temp[i] = x + y;
            temp[i + 8] = (x - y) / DctTables.Dct16[i];
        }

        Forward8(temp[..8]);
        Forward8(temp[8..]);

        for (var i = 0; i < 8 - 1; i++)
        {
            input[(i * 2) + 0] = temp[i];
            input[(i * 2) + 1] = temp[i + 8] + temp[i + 8 + 1];
        }

        input[14] = temp[7];
        input[15] = temp[15];
    }

    private static void Forward8(Span<double> input)
    {
        Span<double> temp = stackalloc double[8];
        double x0 = input[0], y0 = input[7];
        double x1 = input[1], y1 = input[6];
        double x2 = input[2], y2 = input[5];
        double x3 = input[3], y3 = input[4];

        temp[0] = x0 + y0;
        temp[1] = x1 + y1;
        temp[2] = x2 + y2;
        temp[3] = x3 + y3;
        temp[4] = (x0 - y0) / DctTables.Dct8[0];
        temp[5] = (x1 - y1) / DctTables.Dct8[1];
        temp[6] = (x2 - y2) / DctTables.Dct8[2];
        temp[7] = (x3 - y3) / DctTables.Dct8[3];

        Forward4(temp[..4]);
        Forward4(temp[4..]);

        input[0] = temp[0];
        input[1] = temp[4] + temp[5];
        input[2] = temp[1];
        input[3] = temp[5] + temp[6];
        input[4] = temp[2];
        input[5] = temp[6] + temp[7];
        input[6] = temp[3];
        input[7] = temp[7];
    }

    private static void Forward4(Span<double> input)
    {
        double x0 = input[0], y0 = input[3];
        double x1 = input[1], y1 = input[2];

        var t0 = x0 + y0;
        var t1 = x1 + y1;
        var t2 = (x0 - y0) / DctTables.Dct4[0];
        var t3 = (x1 - y1) / DctTables.Dct4[1];

        double x = t0, y = t1;
        t0 += t1;
        t1 = (x - y) / DctTables.Dct2[0];

        x = t2;
        y = t3;
        t2 += t3;
        t3 = (x - y) / DctTables.Dct2[0];

        input[0] = t0;
        input[1] = t2 + t3;
        input[2] = t1;
        input[3] = t3;
    }
}
