using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;

namespace Whisparr.Fingerprints;

/// <summary>
/// The OpenSubtitles hash as stash computes it (<c>pkg/hash/oshash</c>): file size plus the
/// 64-bit little-endian word sums of the first and last 64 KiB, as 16 lowercase hex digits.
/// Matches the OSHASH fingerprints stored by stash and StashDB.
/// </summary>
public static class OsHash
{
    private const int ChunkSize = 64 * 1024;

    public static string Compute(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1, FileOptions.RandomAccess);

        return Compute(stream);
    }

    /// <param name="stream">A readable, seekable stream; its full length is hashed.</param>
    public static string Compute(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
        {
            throw new ArgumentException("Stream must be seekable", nameof(stream));
        }

        var size = stream.Length;

        if (size <= 8)
        {
            throw new ArgumentException($"Cannot calculate oshash where size <= 8 ({size})", nameof(stream));
        }

        // Files under 64 KiB hash their whole length (rounded down to 8 bytes), twice
        var chunk = size < ChunkSize ? (int)(size / 8 * 8) : ChunkSize;
        var buffer = new byte[chunk];

        stream.Seek(0, SeekOrigin.Begin);
        stream.ReadExactly(buffer);
        var head = Sum(buffer);

        stream.Seek(-chunk, SeekOrigin.End);
        stream.ReadExactly(buffer);
        var tail = Sum(buffer);

        return unchecked(head + tail + (ulong)size).ToString("x16", CultureInfo.InvariantCulture);
    }

    private static ulong Sum(ReadOnlySpan<byte> buffer)
    {
        ulong sum = 0;

        for (var i = 0; i < buffer.Length; i += 8)
        {
            sum = unchecked(sum + BinaryPrimitives.ReadUInt64LittleEndian(buffer[i..]));
        }

        return sum;
    }
}
