using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Whisparr.Fingerprints;

// Prints the phash of a file, reading its duration the way stash does.
// usage: phash <file>   (FINGERPRINTS_FFMPEG / FINGERPRINTS_FFPROBE override the binaries)
var file = args[0];
var ffmpeg = Environment.GetEnvironmentVariable("FINGERPRINTS_FFMPEG") ?? FFmpegLocator.BundledPath;
var ffprobe = Environment.GetEnvironmentVariable("FINGERPRINTS_FFPROBE")
              ?? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ffmpeg)!, OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe");

var probe = Process.Start(new ProcessStartInfo(ffprobe, ["-v", "error", "-show_entries", "format=duration", "-of", "json", file]) { RedirectStandardOutput = true })!;
var json = JsonDocument.Parse(probe.StandardOutput.ReadToEnd());
probe.WaitForExit();

var duration = double.Parse(json.RootElement.GetProperty("format").GetProperty("duration").GetString()!, CultureInfo.InvariantCulture);
duration = Math.Round(duration * 100) / 100;

Console.WriteLine(await PerceptualHash.ComputeAsync(file, duration, new PHashOptions { FFmpegPath = ffmpeg }));
