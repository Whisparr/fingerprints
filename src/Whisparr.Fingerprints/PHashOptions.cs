using System;

namespace Whisparr.Fingerprints;

public sealed class PHashOptions
{
    /// <summary>
    /// Path to ffmpeg. Defaults to the binary this package ships next to the application.
    /// </summary>
    public string FFmpegPath { get; init; } = FFmpegLocator.BundledPath;

    /// <summary>
    /// Limit for each of the 25 frame grabs, not the whole hash.
    /// </summary>
    public TimeSpan FrameTimeout { get; init; } = TimeSpan.FromSeconds(60);
}
