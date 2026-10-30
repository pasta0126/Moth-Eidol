using System.Text;

namespace MothEidol.LogSim;

/// <summary>Writes lines like a running Serilog file sink: held open, shared with readers, CRLF, UTF-8 without BOM.</summary>
public sealed class LogFileWriter : IDisposable
{
    private static readonly UTF8Encoding Utf8 = new(false);
    private readonly FileStream _stream;

    public LogFileWriter(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
    }

    public void WriteLines(IEnumerable<string> lines)
    {
        foreach (var line in lines) WriteRaw(line + "\r\n");
    }

    /// <summary>Writes text as is, so a line can be left unterminated.</summary>
    public void WriteRaw(string text)
    {
        var bytes = Utf8.GetBytes(text);
        _stream.Write(bytes, 0, bytes.Length);
        _stream.Flush();
    }

    public void Truncate()
    {
        _stream.SetLength(0);
        _stream.Flush();
    }

    public void Dispose() => _stream.Dispose();
}
