using System;
using System.Collections.Generic;

namespace Whisparr.Fingerprints.Internal;

/// <summary>
/// stash <c>combineImages</c>: tiles frames into a 5x5 grid on a transparent NRGBA
/// canvas sized from the first frame, as <c>imaging.Paste</c> does (clipped to the canvas).
/// </summary>
internal static class Sprite
{
    public const int Columns = 5;
    public const int Rows = 5;
    public const int FrameCount = Columns * Rows;

    public static Frame Combine(IReadOnlyList<Frame> frames)
    {
        if (frames.Count == 0)
        {
            throw new ArgumentException("No frames to combine", nameof(frames));
        }

        var width = frames[0].Width;
        var height = frames[0].Height;
        var canvasWidth = width * Columns;
        var canvasHeight = height * Rows;
        var canvas = new byte[canvasWidth * canvasHeight * 4];

        for (var index = 0; index < frames.Count; index++)
        {
            // upstream divides by rows, not columns; identical for a square grid
            var x = width * (index % Columns);
            var y = height * (index / Rows);
            Paste(canvas, canvasWidth, canvasHeight, frames[index], x, y);
        }

        return new Frame(canvasWidth, canvasHeight, canvas);
    }

    private static void Paste(byte[] canvas, int canvasWidth, int canvasHeight, Frame frame, int x, int y)
    {
        var copyWidth = Math.Min(frame.Width, canvasWidth - x);
        var copyHeight = Math.Min(frame.Height, canvasHeight - y);

        if (copyWidth <= 0 || copyHeight <= 0)
        {
            return;
        }

        for (var row = 0; row < copyHeight; row++)
        {
            frame.Pixels.AsSpan(row * frame.Width * 4, copyWidth * 4)
                .CopyTo(canvas.AsSpan((((y + row) * canvasWidth) + x) * 4));
        }
    }
}
