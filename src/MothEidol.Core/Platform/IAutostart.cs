namespace MothEidol.Core.Platform;

/// <summary>Controls whether the application starts with the user's session.</summary>
public interface IAutostart
{
    /// <summary>True when this platform implements start-with-session.</summary>
    bool IsSupported { get; }

    /// <summary>Why the option is unavailable, when <see cref="IsSupported"/> is false.</summary>
    string? UnsupportedReason { get; }

    bool IsEnabled { get; }

    void SetEnabled(bool enabled);
}
