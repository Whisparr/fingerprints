using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using NUnit.Framework;
using Whisparr.Fingerprints.Internal;

namespace Whisparr.Fingerprints.Test;

[TestFixture]
public class PerceptionHashFixture
{
    [TestCase("dct64")]
    [TestCase("dct32")]
    [TestCase("dct16")]
    [TestCase("dct8")]
    [TestCase("dct4")]
    [TestCase("dct2")]
    public void dct_tables_match_amd64_go(string table)
    {
        var expected = Vectors.Load("tables.json").GetProperty(table).EnumerateArray().Select(e => e.GetString()).ToArray();

        var actual = table switch
        {
            "dct64" => DctTables.Dct64,
            "dct32" => DctTables.Dct32,
            "dct16" => DctTables.Dct16,
            "dct8" => DctTables.Dct8,
            "dct4" => DctTables.Dct4,
            _ => DctTables.Dct2
        };

        Assert.That(actual.Select(Vectors.Bits), Is.EqualTo(expected));
    }

    [Test]
    public void synthetic_sprite_matches_every_stage()
    {
        var expected = Vectors.Load("synthetic.json");

        AssertStages(SyntheticSprite(), 800, 450, expected);
    }

    [Test]
    public void sample_clip_frames_match_every_stage()
    {
        var expected = Vectors.Load("clips", "h264_sample.json");

        var frames = Enumerable.Range(0, Sprite.FrameCount)
            .Select(i => Bmp.Decode(System.IO.File.ReadAllBytes(Vectors.Path("clips", "h264_sample", $"{i:00}.bmp"))))
            .ToList();

        Assert.That(frames[0].Width, Is.EqualTo(expected.GetProperty("frameWidth").GetInt32()));
        Assert.That(frames[0].Height, Is.EqualTo(expected.GetProperty("frameHeight").GetInt32()));

        var sprite = Sprite.Combine(frames);

        AssertStages(sprite.Pixels, sprite.Width, sprite.Height, expected);
    }

    // Expected values from goimagehash etcs.MedianOfPixelsFast64
    [TestCase(37, 0, 64, 31.5)]
    [TestCase(0, 13, 101, 43.5)]
    public void median_matches_goimagehash(int linear, int quadratic, int modulo, double expected)
    {
        double Value(int i) => (((i * i * quadratic) + (i * linear) + (quadratic == 0 ? 0 : 7)) % modulo);

        var values = Enumerable.Range(0, 64).Select(Value).ToArray();

        Assert.That(PerceptionHash.Median(values), Is.EqualTo(expected));
        Assert.That(values, Is.EqualTo(Enumerable.Range(0, 64).Select(Value)), "input must not be reordered");
    }

    [Test]
    public void format_is_unpadded_lowercase_hex()
    {
        Assert.That(PerceptualHash.Format(0x00d500d500d545d7), Is.EqualTo("d500d500d545d7"));
        Assert.That(PerceptualHash.Format(0xffd500d500d545d7), Is.EqualTo("ffd500d500d545d7"));
        Assert.That(PerceptualHash.Format(0), Is.EqualTo("0"));
    }

    private static void AssertStages(byte[] sprite, int width, int height, JsonElement expected)
    {
        Assert.That(width, Is.EqualTo(expected.GetProperty("spriteWidth").GetInt32()));
        Assert.That(height, Is.EqualTo(expected.GetProperty("spriteHeight").GetInt32()));
        Assert.That(Convert.ToHexStringLower(SHA256.HashData(sprite)), Is.EqualTo(expected.GetProperty("spriteSha256").GetString()), "sprite");

        var resized = BilinearResize.Resize(sprite, width, height, 64, 64);
        Assert.That(Convert.ToHexStringLower(resized), Is.EqualTo(expected.GetProperty("resizedRgbaHex").GetString()), "resize");

        var gray = PerceptionHash.Gray(resized);
        AssertBits(gray, expected.GetProperty("grayBits"), "gray");

        Dct.Transform2D(gray);
        AssertBits(gray, expected.GetProperty("dctBits"), "dct");

        var flattened = PerceptionHash.Flatten(gray);
        AssertBits(flattened, expected.GetProperty("flattenedBits"), "flatten");

        Assert.That(Vectors.Bits(PerceptionHash.Median(flattened)), Is.EqualTo(expected.GetProperty("medianBits").GetString()), "median");

        var hash = PerceptualHash.Format(PerceptionHash.Compute(sprite, width, height));
        Assert.That(hash, Is.EqualTo(expected.GetProperty("phash").GetString()), "phash");
        Assert.That(hash, Is.EqualTo(expected.GetProperty("phashFromGoimagehash").GetString()), "phash vs goimagehash");
    }

    private static void AssertBits(double[] actual, JsonElement expected, string stage)
    {
        var expectedBits = expected.EnumerateArray().Select(e => e.GetString()).ToArray();
        var actualBits = actual.Select(Vectors.Bits).ToArray();

        var mismatches = actualBits.Zip(expectedBits).Count(p => p.First != p.Second);

        Assert.That(mismatches, Is.Zero, $"{stage}: {mismatches} of {expectedBits.Length} values differ from Go");
    }

    // Same formula as tools/vectors syntheticSprite
    private static byte[] SyntheticSprite()
    {
        var pixels = new byte[800 * 450 * 4];

        for (var y = 0; y < 450; y++)
        {
            for (var x = 0; x < 800; x++)
            {
                var p = ((y * 800) + x) * 4;
                pixels[p + 0] = (byte)(((x * 7) + (y * 3)) % 256);
                pixels[p + 1] = (byte)(((x * x) + (y * 5)) % 256);
                pixels[p + 2] = (byte)((x ^ y) % 256);
                pixels[p + 3] = 255;
            }
        }

        return pixels;
    }
}
