using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace Whisparr.Fingerprints.Test;

/// <summary>
/// Reference values from the Go implementations, produced on linux/amd64 by
/// <c>tools/make-fixtures.sh</c>.
/// </summary>
internal static class Vectors
{
    public static string Path(params string[] parts) =>
        System.IO.Path.Combine([TestContext.CurrentContext.TestDirectory, "Vectors", .. parts]);

    public static JsonElement Load(params string[] parts)
    {
        using var stream = File.OpenRead(Path(parts));
        using var document = JsonDocument.Parse(stream);

        return document.RootElement.Clone();
    }

    public static double[] Doubles(JsonElement bits) =>
        bits.EnumerateArray().Select(b => Double(b.GetString()!)).ToArray();

    public static double Double(string bits) =>
        BitConverter.Int64BitsToDouble(long.Parse(bits, NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    public static string Bits(double value) =>
        BitConverter.DoubleToInt64Bits(value).ToString("x16", CultureInfo.InvariantCulture);
}
