using System.Security.Cryptography;

namespace MothEidol.Core.Logs;

/// <summary>
/// Recognizes "the same file" without platform-specific file ids (unreliable on network shares):
/// a hash of the file's first bytes. Log files start with timestamps, so a rotated or replaced file
/// at the same path has a different prefix.
/// </summary>
internal static class FileFingerprint
{
    public const int PrefixLength = 128;

    public static string? Compute(FileStream stream)
    {
        var length = (int)Math.Min(stream.Length, PrefixLength);
        if (length == 0) return null;

        var buffer = new byte[length];
        if (ReadAt(stream, 0, buffer, length) < length) return null;
        return $"{length}:{Convert.ToHexString(SHA256.HashData(buffer))}";
    }

    /// <summary>True when the file still begins with the bytes the fingerprint was taken from.</summary>
    public static bool Matches(FileStream stream, string? fingerprint)
    {
        if (fingerprint is null) return true;

        var separator = fingerprint.IndexOf(':');
        if (separator < 0 || !int.TryParse(fingerprint.AsSpan(0, separator), out var length)) return false;
        if (stream.Length < length) return false;

        var buffer = new byte[length];
        if (ReadAt(stream, 0, buffer, length) < length) return false;
        return Convert.ToHexString(SHA256.HashData(buffer)) == fingerprint[(separator + 1)..];
    }

    /// <summary>A fingerprint taken while the file was still short can be extended as it grows.</summary>
    public static bool CanExtend(string? fingerprint)
    {
        if (fingerprint is null) return true;
        var separator = fingerprint.IndexOf(':');
        return separator < 0 || !int.TryParse(fingerprint.AsSpan(0, separator), out var length) || length < PrefixLength;
    }

    internal static int ReadAt(FileStream stream, long offset, byte[] buffer, int count)
    {
        stream.Seek(offset, SeekOrigin.Begin);
        var total = 0;
        while (total < count)
        {
            var read = stream.Read(buffer, total, count - total);
            if (read == 0) break;
            total += read;
        }
        return total;
    }
}
