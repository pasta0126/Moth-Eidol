namespace MothEidol.Core.Platform;

/// <summary>The fixed web addresses the application is allowed to open. They are constants, never built from user data.</summary>
public static class SupportLinks
{
    public const string BuyMeACoffee = "https://buymeacoffee.com/pasta0126";
    public const string SuggestionsAndBugs = "https://github.com/pasta0126/Moth-Eidol/issues/new/choose";
    public const string GitHub = "https://github.com/pasta0126/";

    private static readonly string[] Allowed = [BuyMeACoffee, SuggestionsAndBugs, GitHub];

    /// <summary>True only for one of the fixed <c>https</c> addresses above.</summary>
    public static bool IsAllowed(string? url) =>
        url is not null
        && Allowed.Contains(url, StringComparer.Ordinal)
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;
}
