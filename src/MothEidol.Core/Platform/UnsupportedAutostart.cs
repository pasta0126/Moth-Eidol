namespace MothEidol.Core.Platform;

public sealed class UnsupportedAutostart(string reason) : IAutostart
{
    public bool IsSupported => false;

    public string? UnsupportedReason => reason;

    public bool IsEnabled => false;

    public void SetEnabled(bool enabled) =>
        throw new NotSupportedException(reason);
}
