using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Whisparr.Fingerprints.Internal;

namespace Whisparr.Fingerprints;

/// <summary>
/// The video perceptual hash as stash computes it (<c>pkg/hash/videophash</c>): 25 frames
/// spread over the middle 90% of the video, tiled 5x5, then goimagehash's PerceptionHash.
/// Matches the PHASH fingerprints stored by stash and StashDB.
/// </summary>
public static class PerceptualHash
{
    private const int ScreenshotWidth = 160;

    /// <param name="durationSeconds">
    /// The container duration as stash reads it: ffprobe <c>format.duration</c> rounded to
    /// 0.01s. A stream or "best" runtime moves the sampled frames and changes the hash.
    /// </param>
    /// <returns>The hash in stash's format: lowercase hex, not zero-padded.</returns>
    public static async Task<string> ComputeAsync(string path, double durationSeconds, PHashOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (!(durationSeconds > 0) || double.IsInfinity(durationSeconds))
        {
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), durationSeconds, "Duration must be positive");
        }

        options ??= new PHashOptions();
        FFmpegLocator.EnsureExecutable(options.FFmpegPath);

        var frames = new List<Frame>(Sprite.FrameCount);
        var slowSeek = false;

        foreach (var time in FrameTimes(durationSeconds))
        {
            Frame frame;

            try
            {
                frame = await GrabFrameAsync(options, path, time, slowSeek, cancellationToken).ConfigureAwait(false);
            }
            catch (FFmpegException) when (!slowSeek)
            {
                // stash: a failed fast seek switches to accurate seeking for the rest
                slowSeek = true;
                frame = await GrabFrameAsync(options, path, time, slowSeek, cancellationToken).ConfigureAwait(false);
            }

            frames.Add(frame);
        }

        var sprite = Sprite.Combine(frames);

        return Format(PerceptionHash.Compute(sprite.Pixels, sprite.Width, sprite.Height));
    }

    /// <summary>
    /// stash <c>utils.PhashToString</c>: <c>strconv.FormatUint(hash, 16)</c>.
    /// </summary>
    public static string Format(ulong hash) => hash.ToString("x", CultureInfo.InvariantCulture);

    internal static double[] FrameTimes(double duration)
    {
        var offset = 0.05 * duration;
        var stepSize = 0.9 * duration / Sprite.FrameCount;
        var times = new double[Sprite.FrameCount];

        for (var i = 0; i < times.Length; i++)
        {
            times[i] = offset + (i * stepSize);
        }

        return times;
    }

    /// <summary>
    /// Go <c>fmt.Sprint(float64)</c>, i.e. <c>strconv.FormatFloat(v, 'g', -1, 64)</c>:
    /// shortest round-trip digits, in exponent form (<c>1.5e+06</c>) when the decimal
    /// exponent is below -4 or at least 6.
    /// </summary>
    internal static string FormatSeconds(double seconds)
    {
        var (negative, digits, exponent) = ShortestDigits(seconds);

        if (digits == "0")
        {
            return negative ? "-0" : "0";
        }

        var sign = negative ? "-" : string.Empty;

        if (exponent < -4 || exponent >= 6)
        {
            var mantissa = digits.Length > 1 ? $"{digits[0]}.{digits[1..]}" : digits;

            return $"{sign}{mantissa}e{(exponent < 0 ? '-' : '+')}{Math.Abs(exponent):00}";
        }

        // digits d1d2d3... with the decimal point after (exponent + 1) digits
        var point = exponent + 1;

        if (point <= 0)
        {
            return $"{sign}0.{new string('0', -point)}{digits}";
        }

        if (point >= digits.Length)
        {
            return $"{sign}{digits}{new string('0', point - digits.Length)}";
        }

        return $"{sign}{digits[..point]}.{digits[point..]}";
    }

    // .NET's "R" is shortest round-trip too; only its layout differs from Go's
    private static (bool Negative, string Digits, int Exponent) ShortestDigits(double value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        var negative = text.StartsWith('-');

        if (negative)
        {
            text = text[1..];
        }

        var exponent = 0;
        var e = text.IndexOf('E', StringComparison.Ordinal);

        if (e >= 0)
        {
            exponent = int.Parse(text[(e + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            text = text[..e];
        }

        var dot = text.IndexOf('.', StringComparison.Ordinal);
        var integerDigits = dot >= 0 ? dot : text.Length;
        var all = text.Replace(".", string.Empty, StringComparison.Ordinal);
        var leadingZeros = all.Length - all.TrimStart('0').Length;
        var significant = all.Trim('0');

        if (significant.Length == 0)
        {
            return (negative, "0", 0);
        }

        return (negative, significant, exponent + integerDigits - leadingZeros - 1);
    }

    internal static IReadOnlyList<string> FrameArguments(string path, double time, bool slowSeek)
    {
        var args = new List<string> { "-v", "error", "-y" };
        var seek = FormatSeconds(time);

        if (!slowSeek)
        {
            args.AddRange(["-ss", seek]);
        }

        args.AddRange(["-i", path]);

        if (slowSeek)
        {
            args.AddRange(["-ss", seek]);
        }

        args.AddRange(["-frames:v", "1", "-vf", $"scale={ScreenshotWidth}:-2", "-c:v", "bmp", "-f", "rawvideo", "-"]);

        return args;
    }

    private static async Task<Frame> GrabFrameAsync(PHashOptions options, string path, double time, bool slowSeek, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(options.FFmpegPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in FrameArguments(path, time, slowSeek))
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.FrameTimeout);

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            throw new FileNotFoundException($"ffmpeg not found at '{options.FFmpegPath}'", options.FFmpegPath, ex);
        }

        using var stdout = new MemoryStream();
        var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(stdout, timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync(timeout.Token)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);

            cancellationToken.ThrowIfCancellationRequested();

            throw new FFmpegException($"ffmpeg timed out after {options.FrameTimeout} grabbing frame at {time}s");
        }

        if (process.ExitCode != 0)
        {
            throw new FFmpegException($"ffmpeg exited with {process.ExitCode} grabbing frame at {time}s: {(await stderrTask.ConfigureAwait(false)).Trim()}");
        }

        try
        {
            return Bmp.Decode(stdout.GetBuffer().AsSpan(0, (int)stdout.Length));
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException)
        {
            // stash treats an undecodable screenshot like a failed seek
            throw new FFmpegException($"Unreadable frame at {time}s: {ex.Message}", ex);
        }
    }
}
