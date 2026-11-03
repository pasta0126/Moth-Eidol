namespace MothEidol.Core.Platform;

/// <summary>Shows a file or a folder in the operating system's file manager.</summary>
public interface IFileManager
{
    /// <summary>Reveals <paramref name="path"/> (selecting it when it is a file and the platform can). Returns false with a reason when it cannot.</summary>
    bool TryReveal(string path, bool isFile, out string? error);
}
