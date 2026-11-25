namespace MothEidol.Core.Updates;

/// <summary>Where the application learns which version of itself is published (the Microsoft Store in the packaged build).</summary>
public interface IUpdateSource
{
    Task<UpdateCheckResult> GetLatestAsync(CancellationToken cancellationToken);
}

/// <summary>The outcome of asking an <see cref="IUpdateSource"/> for the published version.</summary>
public abstract record UpdateCheckResult
{
    private UpdateCheckResult() { }

    /// <summary>Nothing newer is published.</summary>
    public sealed record NoUpdate : UpdateCheckResult;

    /// <summary>A version newer than the installed package is published.</summary>
    public sealed record Available(Version Version) : UpdateCheckResult;

    /// <summary>The check could not complete (no network, Store error, no readable version).</summary>
    public sealed record Failed(string Reason) : UpdateCheckResult;
}
