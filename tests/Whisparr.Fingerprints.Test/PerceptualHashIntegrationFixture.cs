using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Whisparr.Fingerprints.Test;

/// <summary>
/// Runs the full pipeline through a real ffmpeg. The expected hash came from frames that
/// tools/make-fixtures.sh extracted with the same ffmpeg build, so point
/// <c>FINGERPRINTS_FFMPEG</c> at that build (CI regenerates the vectors with its own).
/// </summary>
[TestFixture]
[Category("IntegrationTest")]
public class PerceptualHashIntegrationFixture
{
    private string _ffmpeg = null!;

    [OneTimeSetUp]
    public void FindFFmpeg()
    {
        _ffmpeg = Environment.GetEnvironmentVariable("FINGERPRINTS_FFMPEG")
                  ?? (FFmpegLocator.IsBundledAvailable ? FFmpegLocator.BundledPath : OnPath("ffmpeg"))
                  ?? string.Empty;

        if (!File.Exists(_ffmpeg))
        {
            Assert.Ignore("ffmpeg not found; set FINGERPRINTS_FFMPEG");
        }
    }

    [Test]
    public async Task sample_clip_matches_oracle()
    {
        var expected = Vectors.Load("clips", "h264_sample.json");

        var hash = await PerceptualHash.ComputeAsync(
            Vectors.Path("clips", "h264_sample.mp4"),
            expected.GetProperty("duration").GetDouble(),
            new PHashOptions { FFmpegPath = _ffmpeg });

        Assert.That(hash, Is.EqualTo(expected.GetProperty("phash").GetString()));
        Assert.That(hash, Is.EqualTo(expected.GetProperty("oracle").GetProperty("phash").GetString()), "stash via videohashes");
    }

    [Test]
    public void missing_ffmpeg_throws_file_not_found()
    {
        var options = new PHashOptions { FFmpegPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) };

        Assert.ThrowsAsync<FileNotFoundException>(() => PerceptualHash.ComputeAsync(Vectors.Path("clips", "h264_sample.mp4"), 10.56, options));
    }

    [Test]
    public void unreadable_input_throws_ffmpeg_exception()
    {
        var notVideo = Vectors.Path("tables.json");

        Assert.ThrowsAsync<FFmpegException>(() => PerceptualHash.ComputeAsync(notVideo, 10, new PHashOptions { FFmpegPath = _ffmpeg }));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void rejects_invalid_duration(double duration)
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => PerceptualHash.ComputeAsync("x.mp4", duration));
    }

    private static string? OnPath(string name) =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(dir => Path.Combine(dir, OperatingSystem.IsWindows() ? name + ".exe" : name))
            .FirstOrDefault(File.Exists);
}
