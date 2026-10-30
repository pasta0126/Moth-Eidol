using MothEidol.Core.Logs;

namespace MothEidol.Core.Projects;

/// <summary>A log source as entered in the project wizard, before anything is saved.</summary>
public sealed record SourceDraft(SourceKind Kind, string Path, string? Pattern)
{
    public string DisplayName => Kind == SourceKind.File ? Path : System.IO.Path.Combine(Path, Pattern ?? "*");

    /// <summary>The draft with its text trimmed and the pattern dropped for files.</summary>
    public SourceDraft Normalized() => new(
        Kind,
        Path?.Trim() ?? "",
        Kind == SourceKind.File || string.IsNullOrWhiteSpace(Pattern) ? null : Pattern.Trim());

    public bool Same(SourceDraft other) =>
        Kind == other.Kind
        && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase)
        && string.Equals(Pattern, other.Pattern, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Everything the project wizard collects. Validation is per step and returns a readable message (or null
/// when the step is valid), so the view only shows it and the rules stay testable without a UI.
/// </summary>
public sealed class ProjectDraft
{
    public string Name { get; set; } = "";

    public List<SourceDraft> Sources { get; } = [];

    public AlertSettings Alerts { get; set; } = AlertSettings.Default;

    public string? ValidateName(IEnumerable<string> existingNames)
    {
        var name = Name?.Trim() ?? "";
        if (name.Length == 0) return "A project needs a name.";
        if (existingNames.Any(n => string.Equals(n, name, StringComparison.CurrentCultureIgnoreCase)))
            return $"A project named '{name}' already exists.";
        return null;
    }

    /// <summary>Checks one source on its own, so it can be validated before it is added to the list.</summary>
    public static string? ValidateSource(SourceDraft source)
    {
        source = source.Normalized();
        if (source.Path.Length == 0) return source.Kind == SourceKind.File ? "Enter the log file path." : "Enter the log folder path.";
        if (source.Kind == SourceKind.Folder && source.Pattern is null) return "A folder needs a file-name pattern such as *.log.";
        return null;
    }

    /// <summary>Adds a source after validating it. Returns the error, or null when it was added.</summary>
    public string? AddSource(SourceDraft source)
    {
        if (ValidateSource(source) is { } error) return error;

        source = source.Normalized();
        if (Sources.Any(s => s.Same(source))) return "That source is already in the list.";

        Sources.Add(source);
        return null;
    }

    public string? ValidateSources() =>
        Sources.Count == 0 ? "Add at least one log file or folder." : null;

    public string? ValidateAlerts()
    {
        try
        {
            Alerts.Validate();
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
    }
}
