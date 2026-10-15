using System;
using System.Buffers.Binary;
using System.IO;
using NUnit.Framework;

namespace Whisparr.Fingerprints.Test;

[TestFixture]
public class OsHashFixture
{
    [Test]
    public void sums_head_tail_and_size()
    {
        // 192 KiB of words equal to their index, so head and tail sums are known
        var data = new byte[192 * 1024];

        for (var i = 0; i < data.Length / 8; i++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(i * 8), (ulong)i);
        }

        const ulong words = 64 * 1024 / 8;
        const ulong total = 192 * 1024 / 8;
        var head = words * (words - 1) / 2;
        var tail = (total * (total - 1) / 2) - ((total - words) * (total - words - 1) / 2);

        var expected = (head + tail + (ulong)data.Length).ToString("x16");

        Assert.That(OsHash.Compute(new MemoryStream(data)), Is.EqualTo(expected));
    }

    [Test]
    public void small_file_hashes_whole_length_rounded_to_words()
    {
        // 20 bytes: chunk is 16, so head = words 0-1 and tail = the last 16 bytes
        var data = new byte[20];
        data[0] = 1;
        data[19] = 0x80;

        // head word0 = 1; tail starts at byte 4: word0 = 0, word1 = bytes 12..19 => 0x80 << 56
        var expected = unchecked(1UL + (0x80UL << 56) + 20UL).ToString("x16");

        Assert.That(OsHash.Compute(new MemoryStream(data)), Is.EqualTo(expected));
    }

    [Test]
    public void wraps_on_overflow()
    {
        var data = new byte[16];
        Array.Fill(data, (byte)0xff);

        // head = 2 * max, tail = 2 * max, + 16, all mod 2^64
        var expected = unchecked((ulong.MaxValue * 4) + 16).ToString("x16");

        Assert.That(OsHash.Compute(new MemoryStream(data)), Is.EqualTo(expected));
    }

    [TestCase(0)]
    [TestCase(8)]
    public void rejects_eight_bytes_or_fewer(int size)
    {
        Assert.Throws<ArgumentException>(() => OsHash.Compute(new MemoryStream(new byte[size])));
    }

    [Test]
    public void sample_clip_matches_reference()
    {
        var expected = Vectors.Load("clips", "h264_sample.json").GetProperty("oshash").GetString();

        Assert.That(OsHash.Compute(Vectors.Path("clips", "h264_sample.mp4")), Is.EqualTo(expected));
    }
}
