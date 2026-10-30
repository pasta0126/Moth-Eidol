using System.Text;

namespace MothEidol.Core.Logs;

internal readonly record struct ReadResult(
    List<string> Lines, long NewOffset, LineEncoding Encoding, long BytesRead, bool ReachedEnd);

/// <summary>Reads complete lines from a byte offset, handling BOM detection, UTF-8/UTF-16 and CRLF/LF.</summary>
internal static class LineReader
{
    /// <summary>A line with no break inside a chunk at least this large is emitted split instead of waiting forever.</summary>
    public const int MinForcedSplit = 64 * 1024;

    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    public static ReadResult Read(FileStream stream, long offset, LineEncoding encoding, int maxBytes, bool flushPartial)
    {
        var length = stream.Length;
        if (offset == 0 && length > 0)
        {
            (encoding, offset) = DetectBom(stream);
        }

        var available = length - offset;
        if (available <= 0) return new ReadResult([], offset, encoding, 0, ReachedEnd: true);

        var toRead = (int)Math.Min(available, maxBytes);
        if (encoding != LineEncoding.Utf8) toRead &= ~1;
        if (toRead == 0) return new ReadResult([], offset, encoding, 0, ReachedEnd: maxBytes > 0 && available <= 1);

        var buffer = new byte[toRead];
        toRead = FileFingerprint.ReadAt(stream, offset, buffer, toRead);

        var reachedEnd = offset + toRead >= length;
        var complete = LastLineEnd(buffer, toRead, encoding);

        if (complete == 0)
        {
            if (reachedEnd)
            {
                // Trailing text without a line break: wait for it unless the file will not grow any more.
                if (!flushPartial) return new ReadResult([], offset, encoding, 0, reachedEnd);
                complete = toRead;
            }
            else if (maxBytes >= MinForcedSplit)
            {
                complete = toRead;
            }
            else
            {
                return new ReadResult([], offset, encoding, 0, ReachedEnd: false);
            }
        }
        else if (reachedEnd && flushPartial)
        {
            complete = toRead;
        }

        var text = Decode(encoding).GetString(buffer, 0, complete);
        return new ReadResult(Split(text), offset + complete, encoding, complete, reachedEnd);
    }

    /// <summary>Position right after the BOM (if any) and the encoding it announces.</summary>
    public static (LineEncoding Encoding, long Offset) DetectBom(FileStream stream)
    {
        var head = new byte[Math.Min(3, stream.Length)];
        FileFingerprint.ReadAt(stream, 0, head, head.Length);

        if (head.Length >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF) return (LineEncoding.Utf8, 3);
        if (head.Length >= 2 && head[0] == 0xFF && head[1] == 0xFE) return (LineEncoding.Utf16Le, 2);
        if (head.Length >= 2 && head[0] == 0xFE && head[1] == 0xFF) return (LineEncoding.Utf16Be, 2);
        return (LineEncoding.Utf8, 0);
    }

    private static Encoding Decode(LineEncoding encoding) => encoding switch
    {
        LineEncoding.Utf16Le => Encoding.Unicode,
        LineEncoding.Utf16Be => Encoding.BigEndianUnicode,
        _ => Utf8,
    };

    /// <summary>Number of bytes up to and including the last line feed, or 0 when there is none.</summary>
    private static int LastLineEnd(byte[] buffer, int count, LineEncoding encoding)
    {
        switch (encoding)
        {
            case LineEncoding.Utf16Le:
                for (var i = count - 2; i >= 0; i -= 2)
                    if (buffer[i] == 0x0A && buffer[i + 1] == 0) return i + 2;
                return 0;
            case LineEncoding.Utf16Be:
                for (var i = count - 2; i >= 0; i -= 2)
                    if (buffer[i] == 0 && buffer[i + 1] == 0x0A) return i + 2;
                return 0;
            default:
                var index = Array.LastIndexOf(buffer, (byte)0x0A, count - 1);
                return index < 0 ? 0 : index + 1;
        }
    }

    private static List<string> Split(string text)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        if (lines.Count > 0 && lines[^1].Length == 0 && text.EndsWith('\n')) lines.RemoveAt(lines.Count - 1);
        return lines;
    }
}
