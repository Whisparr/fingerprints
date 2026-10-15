using System;

namespace Whisparr.Fingerprints;

public class FFmpegException : Exception
{
    public FFmpegException()
    {
    }

    public FFmpegException(string message)
        : base(message)
    {
    }

    public FFmpegException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
