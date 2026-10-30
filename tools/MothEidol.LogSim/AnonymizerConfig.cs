using System.Text.Json;

namespace MothEidol.LogSim;

public sealed class RegexRule
{
    public string Pattern { get; set; } = "";
    public string Replacement { get; set; } = "";
}

/// <summary>
/// Private anonymization settings. The real company, project and user names live here (in a git-ignored
/// file), never in the source or in the committed fixtures.
/// </summary>
public sealed class AnonymizerConfig
{
    /// <summary>Case-insensitive substring replacements, longest term first.</summary>
    public Dictionary<string, string> Terms { get; set; } = new();

    public List<RegexRule> RegexRules { get; set; } = [];

    public int DateShiftDays { get; set; } = -300;

    public int MaxAlertingEntriesPerFile { get; set; } = 60;

    public int MaxInfoEntriesPerFile { get; set; } = 25;

    public static AnonymizerConfig Load(string path)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        var config = JsonSerializer.Deserialize<AnonymizerConfig>(File.ReadAllText(path), options)
                     ?? throw new InvalidDataException($"'{path}' is empty.");
        config.Validate();
        return config;
    }

    public void Validate()
    {
        foreach (var (term, replacement) in Terms)
        {
            if (term.Length == 0) throw new InvalidDataException("A term must not be empty.");
            foreach (var other in Terms.Keys)
            {
                if (replacement.Contains(other, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        $"The replacement of '{term}' contains the term '{other}', which would leak or chain.");
            }
        }
    }
}
