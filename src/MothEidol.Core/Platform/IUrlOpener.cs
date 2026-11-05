namespace MothEidol.Core.Platform;

/// <summary>Opens a web address in the user's default browser. The application never sends data with it.</summary>
public interface IUrlOpener
{
    /// <summary>Opens <paramref name="url"/>. Returns false with a reason when it is not allowed or the browser cannot be started.</summary>
    bool TryOpen(string url, out string? error);
}
