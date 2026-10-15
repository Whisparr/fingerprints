using System;

namespace Whisparr.Fingerprints.Internal;

/// <summary>
/// goimagehash's static DCT tables, <c>cos((i + 0.5) * pi / n) * 2</c>, stored as the exact
/// IEEE-754 bits a linux/amd64 Go build produces.
/// </summary>
/// <remarks>
/// Not computed at runtime: Go constant-folds the argument at arbitrary precision and
/// evaluates <c>math.Cos</c> in pure Go, so .NET's <see cref="Math.Cos"/> can land one ulp
/// away. arm64 Go fuses multiply-adds inside <c>math.Cos</c> and gets <c>Dct16[6]</c> one
/// ulp lower; amd64 is canonical. Regenerate with <c>tools/make-fixtures.sh</c>.
/// </remarks>
internal static class DctTables
{
    public static readonly double[] Dct64 = FromBits(
        0x3ffffd886084cd0c, 0x3fffe9cdad01883a, 0x3fffc26470e19fd4, 0x3fff8764fa714ba9,
        0x3fff38f3ac64e589, 0x3ffed740e7684963, 0x3ffe6288ec48e112, 0x3ffddb13b6ccc23c,
        0x3ffd4134d14dc93a, 0x3ffc954b213411f5, 0x3ffbd7c0ac6f9529, 0x3ffb090a58150200,
        0x3ffa29a7a0462782, 0x3ff93a22499263fc, 0x3ff83b0e0bff976e, 0x3ff72d0837efff97,
        0x3ff610b7551d2cdf, 0x3ff4e6cabbe3e5ea, 0x3ff3affa292050ba, 0x3ff26d054cdd12e0,
        0x3ff11eb3541b4b24, 0x3fef8ba4dbf89abc, 0x3fecc66e9931c45e, 0x3fe9ef7943a8ed89,
        0x3fe7088530fa45a1, 0x3fe4135c94176602, 0x3fe111d262b1f678, 0x3fdc0b826a7e4f62,
        0x3fd5e214448b3fcb, 0x3fcf564e56a97314, 0x3fc2d52092ce19f8, 0x3fa92155f7a36678);

    public static readonly double[] Dct32 = FromBits(
        0x3ffff621e3796d7e, 0x3fffa7557f08a517, 0x3fff0a7efb9230d8, 0x3ffe212104f686e5,
        0x3ffced7af43cc773, 0x3ffb728345196e3e, 0x3ff9b3e047f38741, 0x3ff7b5df226aafaf,
        0x3ff57d69348ceca0, 0x3ff30ff7fce17036, 0x3ff073879922ffee, 0x3feb5d1009e15cc2,
        0x3fe58f9a75ab1fdd, 0x3fdf19f97b215f1e, 0x3fd2c8106e8e613a, 0x3fb91f65f10dd825);

    public static readonly double[] Dct16 = FromBits(
        0x3fffd88da3d12525, 0x3ffe9f4156c62dda, 0x3ffc38b2f180bdb1, 0x3ff8bc806b151741,
        0x3ff44cf325091dd7, 0x3fee2b5d3806f63e, 0x3fe294062ed59f04, 0x3fc917a6bc29b438);

    public static readonly double[] Dct8 = FromBits(
        0x3fff6297cff75cb0, 0x3ffa9b66290ea1a4, 0x3ff1c73b39ae68c9, 0x3fd8f8b83c69a60c);

    public static readonly double[] Dct4 = FromBits(0x3ffd906bcf328d46, 0x3fe87de2a6aea964);

    public static readonly double[] Dct2 = FromBits(0x3ff6a09e667f3bcd);

    private static double[] FromBits(params long[] bits)
    {
        var values = new double[bits.Length];

        for (var i = 0; i < bits.Length; i++)
        {
            values[i] = BitConverter.Int64BitsToDouble(bits[i]);
        }

        return values;
    }
}
