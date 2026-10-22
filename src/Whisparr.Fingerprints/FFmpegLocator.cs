using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Whisparr.Fingerprints;

public static class FFmpegLocator
{
    private static readonly string FileName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

    /// <summary>
    /// The package's ffmpeg: next to the application for a RID-specific build or publish,
    /// otherwise under <c>runtimes/&lt;rid&gt;/native</c>. Falls back to the first location
    /// when neither exists, so errors name the expected path.
    /// </summary>
    public static string BundledPath { get; } = Find();

    public static bool IsBundledAvailable => File.Exists(BundledPath);

    private static string Find()
    {
        var root = Path.Combine(AppContext.BaseDirectory, FileName);

        if (File.Exists(root))
        {
            return root;
        }

        var rid = RuntimeInformation.RuntimeIdentifier;

        // the linux builds are static and serve musl too, as the RID graph falls back
        foreach (var candidate in new[] { rid, rid.Replace("-musl-", "-", StringComparison.Ordinal) })
        {
            var path = Path.Combine(AppContext.BaseDirectory, "runtimes", candidate, "native", FileName);

            if (File.Exists(path))
            {
                return path;
            }
        }

        return root;
    }

    /// <summary>
    /// NuGet doesn't keep Unix file modes, so a restored ffmpeg can lack its exec bit when
    /// the package's build targets didn't run (e.g. copied by hand). Best effort.
    /// </summary>
    internal static void EnsureExecutable(string path)
    {
        if (OperatingSystem.IsWindows() || !File.Exists(path))
        {
            return;
        }

        try
        {
            var mode = File.GetUnixFileMode(path);
            const UnixFileMode exec = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

            if ((mode & UnixFileMode.UserExecute) == 0)
            {
                File.SetUnixFileMode(path, mode | exec);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // not ours to change; starting it will report the real problem
        }
    }
}
