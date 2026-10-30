using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MothEidol.LogSim;

public sealed record AnonymizeResult(int FilesWritten, IReadOnlyList<string> Leaks);

public static partial class Anonymizer
{
    [GeneratedRegex(@"^(?<project>.*?)(?<date>\d{8})\.log$")]
    private static partial Regex DatedFileName();

    /// <summary>
    /// Anonymizes every <c>*.log</c> under <paramref name="inputDir"/> into <paramref name="outputDir"/>.
    /// Nothing is written when the leak check finds a sensitive value in the result.
    /// </summary>
    public static AnonymizeResult Run(string inputDir, string outputDir, AnonymizerConfig config)
    {
        var files = Directory.EnumerateFiles(inputDir, "*.log", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        var knownDates = files
            .Select(f => DatedFileName().Match(Path.GetFileName(f)))
            .Where(m => m.Success)
            .Select(m => DateOnly.TryParseExact(m.Groups["date"].Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateOnly?)null)
            .Where(d => d is not null)
            .Select(d => d!.Value)
            .Distinct()
            .ToList();

        var sanitizer = new Sanitizer(config);
        var shifter = new DateShifter(config.DateShiftDays, knownDates);
        var output = new List<(string RelativePath, string Content)>();

        foreach (var file in files)
        {
            var relativeDir = Path.GetRelativePath(inputDir, Path.GetDirectoryName(file)!);
            var folder = relativeDir == "." ? "" : shifter.Shift(sanitizer.Apply(relativeDir));
            var name = shifter.Shift(sanitizer.Apply(Path.GetFileName(file)));

            var entries = Trim(
                LogFormat.Group(LogFormat.SplitLines(File.ReadAllText(file, Encoding.UTF8))),
                config.MaxAlertingEntriesPerFile,
                config.MaxInfoEntriesPerFile);

            var sb = new StringBuilder();
            foreach (var line in entries.SelectMany(e => e.Lines))
                sb.Append(shifter.Shift(sanitizer.Apply(line))).Append("\r\n");

            output.Add((Path.Combine(folder, name), sb.ToString()));
        }

        var leaks = FindLeaks(output, sanitizer, shifter);
        if (leaks.Count > 0) return new AnonymizeResult(0, leaks);

        Directory.CreateDirectory(outputDir);
        foreach (var stale in Directory.EnumerateFiles(outputDir, "*.log", SearchOption.AllDirectories))
            File.Delete(stale);
        foreach (var (relativePath, content) in output)
        {
            var target = Path.Combine(outputDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, content, new UTF8Encoding(false));
        }
        return new AnonymizeResult(output.Count, []);
    }

    /// <summary>Keeps warnings and above up to a cap, and an evenly spread sample of lower levels.</summary>
    public static List<LogEntryText> Trim(List<LogEntryText> entries, int maxAlerting, int maxInfo)
    {
        var keep = new bool[entries.Count];
        var alerting = Enumerable.Range(0, entries.Count).Where(i => LogFormat.IsAlerting(entries[i].Level)).ToList();
        var low = Enumerable.Range(0, entries.Count).Where(i => !LogFormat.IsAlerting(entries[i].Level)).ToList();

        foreach (var i in alerting.Take(maxAlerting)) keep[i] = true;
        if (low.Count <= maxInfo)
        {
            foreach (var i in low) keep[i] = true;
        }
        else if (maxInfo > 0)
        {
            var step = (double)low.Count / maxInfo;
            for (var n = 0; n < maxInfo; n++) keep[low[(int)(n * step)]] = true;
        }
        return entries.Where((_, i) => keep[i]).ToList();
    }

    private static List<string> FindLeaks(
        List<(string RelativePath, string Content)> output, Sanitizer sanitizer, DateShifter shifter)
    {
        var sensitive = sanitizer.SensitiveValues().Concat(shifter.OriginalForms()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var leaks = new List<string>();
        foreach (var (path, content) in output)
        {
            foreach (var value in sensitive)
            {
                if (path.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                    content.Contains(value, StringComparison.OrdinalIgnoreCase))
                    leaks.Add($"{path}: still contains '{value}'");
            }
        }
        return leaks;
    }
}
