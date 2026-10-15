namespace Whisparr.Fingerprints.Internal;

/// <summary>
/// Port of goimagehash v1.1.0 <c>PerceptionHash</c> (BSD-2-Clause, Copyright 2017 The
/// goimagehash Authors): 64x64 bilinear resize, luminance, 2D DCT, then one bit per
/// low-frequency 8x8 coefficient, set when it is above the median.
/// </summary>
internal static class PerceptionHash
{
    private const int Size = 64;
    private const int LowFrequency = 8;

    public static ulong Compute(byte[] nrgba, int width, int height)
    {
        var resized = BilinearResize.Resize(nrgba, width, height, Size, Size);

        var pixels = Gray(resized);
        Dct.Transform2D(pixels);

        var flattened = Flatten(pixels);
        var median = Median(flattened);

        ulong hash = 0;

        for (var idx = 0; idx < flattened.Length; idx++)
        {
            if (flattened[idx] > median)
            {
                hash |= 1UL << (64 - idx - 1);
            }
        }

        return hash;
    }

    /// <summary>
    /// <c>rgb2GrayRGBA</c>. Upstream divides 16-bit channels by 257, 257 and 256; for 8-bit
    /// sources widened by 257 all three give back the 8-bit value, so they're used directly.
    /// Evaluated left to right without fused multiply-adds, which is what amd64 Go does.
    /// </summary>
    public static double[] Gray(byte[] rgba)
    {
        var pixels = new double[Size * Size];

        for (var i = 0; i < pixels.Length; i++)
        {
            var p = i * 4;
            pixels[i] = (0.299 * rgba[p]) + (0.587 * rgba[p + 1]) + (0.114 * rgba[p + 2]);
        }

        return pixels;
    }

    // FlattenPixelsFast64(pixels, 8, 8)
    public static double[] Flatten(double[] pixels)
    {
        var flattened = new double[LowFrequency * LowFrequency];

        for (var i = 0; i < LowFrequency; i++)
        {
            for (var j = 0; j < LowFrequency; j++)
            {
                flattened[(LowFrequency * i) + j] = pixels[(i * Size) + j];
            }
        }

        return flattened;
    }

    /// <summary>
    /// <c>etcs.MedianOfPixelsFast64</c>. The quickselect only places element 32; element
    /// 31 is whatever the partitioning left there, so pivot choice and swap order must
    /// match upstream exactly.
    /// </summary>
    public static double Median(double[] pixels)
    {
        var sequence = (double[])pixels.Clone();
        var low = 0;
        var hi = sequence.Length - 1;
        var k = sequence.Length / 2;

        if (low == hi)
        {
            return sequence[k];
        }

        while (low < hi)
        {
            var pivot = (low / 2) + (hi / 2);
            var pivotValue = sequence[pivot];
            var storeIdx = low;
            (sequence[pivot], sequence[hi]) = (sequence[hi], sequence[pivot]);

            for (var i = low; i < hi; i++)
            {
                if (sequence[i] < pivotValue)
                {
                    (sequence[storeIdx], sequence[i]) = (sequence[i], sequence[storeIdx]);
                    storeIdx++;
                }
            }

            (sequence[hi], sequence[storeIdx]) = (sequence[storeIdx], sequence[hi]);

            if (k <= storeIdx)
            {
                hi = storeIdx;
            }
            else
            {
                low = storeIdx + 1;
            }
        }

        if (sequence.Length % 2 == 0)
        {
            return (sequence[k - 1] / 2) + (sequence[k] / 2);
        }

        return sequence[k];
    }
}
