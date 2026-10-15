using System;
using System.IO;

namespace Whisparr.Fingerprints;

public static class FFmpegLocator
{
    /// <summary>
    /// Where the package's native asset lands for a RID-specific build or publish.
    /// </summary>
    public static string BundledPath { get; } = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

    public static bool IsBundledAvailable => File.Exists(BundledPath);
}
