using System.Linq;
using NUnit.Framework;

namespace Whisparr.Fingerprints.Test;

[TestFixture]
public class FrameSamplingFixture
{
    [TestCase("634")]
    [TestCase("10.56")]
    [TestCase("1.5")]
    [TestCase("3599.99")]
    [TestCase("7322.43")]
    public void seek_strings_match_go(string duration)
    {
        var expected = Vectors.Load("seeks.json").GetProperty(duration).EnumerateArray().Select(e => e.GetString()).ToArray();

        var actual = PerceptualHash.FrameTimes(double.Parse(duration, System.Globalization.CultureInfo.InvariantCulture))
            .Select(PerceptualHash.FormatSeconds)
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected));
    }

    // Go 1.27 fmt.Sprint(float64) outputs
    [TestCase(0.00001, "1e-05")]
    [TestCase(0.000099, "9.9e-05")]
    [TestCase(0.0001, "0.0001")]
    [TestCase(0.00012, "0.00012")]
    [TestCase(31.7, "31.7")]
    [TestCase(99999.5, "99999.5")]
    [TestCase(999999.5, "999999.5")]
    [TestCase(1000000.0, "1e+06")]
    [TestCase(1234567.5, "1.2345675e+06")]
    [TestCase(12345678.0, "1.2345678e+07")]
    [TestCase(123456789.125, "1.23456789125e+08")]
    [TestCase(1e15, "1e+15")]
    [TestCase(1e21, "1e+21")]
    [TestCase(0.07500000000000001, "0.07500000000000001")]
    [TestCase(100.0, "100")]
    [TestCase(0.0, "0")]
    public void format_seconds_matches_go_edges(double value, string expected)
    {
        Assert.That(PerceptualHash.FormatSeconds(value), Is.EqualTo(expected));
    }

    [Test]
    public void fast_seek_puts_ss_before_input()
    {
        var args = PerceptualHash.FrameArguments("in.mp4", 31.7, slowSeek: false);

        Assert.That(args, Is.EqualTo(new[] { "-v", "error", "-y", "-ss", "31.7", "-i", "in.mp4", "-frames:v", "1", "-vf", "scale=160:-2", "-c:v", "bmp", "-f", "rawvideo", "-" }));
    }

    [Test]
    public void slow_seek_puts_ss_after_input()
    {
        var args = PerceptualHash.FrameArguments("in.mp4", 31.7, slowSeek: true);

        Assert.That(args, Is.EqualTo(new[] { "-v", "error", "-y", "-i", "in.mp4", "-ss", "31.7", "-frames:v", "1", "-vf", "scale=160:-2", "-c:v", "bmp", "-f", "rawvideo", "-" }));
    }
}
