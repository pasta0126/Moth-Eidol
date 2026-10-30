using System.Text;

namespace MothEidol.LogSim.Tests;

public class AnonymizerTests
{
    private const string Sample =
        "2026-10-28 10:00:00.100 +01:00 [INF] Started for ana.perez@corp.es from 10.1.2.3\r\n" +
        "2026-10-28 10:00:01.200 +01:00 [ERR] Failed in C:\\Users\\jdoe\\src\\Worker.cs\r\n" +
        "System.InvalidOperationException: boom\r\n" +
        "   at Corp.Worker.Run() in C:\\Users\\jdoe\\src\\Worker.cs:line 60\r\n" +
        "2026-10-28 10:00:02.300 +01:00 [INF] Done\r\n";

    private static AnonymizerConfig Config() => new()
    {
        Terms = new() { ["Corp"] = "Acme" },
        MaxAlertingEntriesPerFile = 10,
        MaxInfoEntriesPerFile = 10,
    };

    private static string Prepare(TempDir temp, string content = Sample)
    {
        var input = temp.Sub("in");
        Directory.CreateDirectory(Path.Combine(input, "CorpService"));
        File.WriteAllText(Path.Combine(input, "CorpService", "CorpService20261028.log"), content, new UTF8Encoding(false));
        return input;
    }

    [Fact]
    public void Renames_files_and_folders_shifts_dates_and_keeps_structure()
    {
        using var temp = new TempDir();
        var output = Path.Combine(temp.Path, "out");

        var result = Anonymizer.Run(Prepare(temp), output, Config());

        Assert.Empty(result.Leaks);
        var file = Path.Combine(output, "AcmeService", "AcmeService20260101.log");
        Assert.True(File.Exists(file));
        var text = File.ReadAllText(file);
        Assert.Contains("2026-01-01 10:00:00.100 +01:00 [INF] Started for user001@example.test from 192.0.2.1\r\n", text);
        Assert.Contains("[ERR] Failed in C:\\Users\\dev\\src\\Worker.cs\r\nSystem.InvalidOperationException: boom\r\n   at Acme.Worker.Run()", text);
    }

    [Fact]
    public void Leak_check_fails_and_writes_nothing_when_a_sensitive_value_survives()
    {
        using var temp = new TempDir();
        var input = Prepare(temp, Sample + "2026-10-28 10:00:03.000 +01:00 [INF] contact valentina.montiel at the desk\r\n");
        var output = Path.Combine(temp.Path, "out");

        // The email local part is only known because another line used the full address.
        var content = File.ReadAllText(Path.Combine(input, "CorpService", "CorpService20261028.log"))
            .Replace("ana.perez@corp.es", "valentina.montiel@corp.es");
        File.WriteAllText(Path.Combine(input, "CorpService", "CorpService20261028.log"), content);

        var result = Anonymizer.Run(input, output, Config());

        Assert.NotEmpty(result.Leaks);
        Assert.Contains(result.Leaks, l => l.Contains("valentina.montiel"));
        Assert.False(Directory.Exists(output));
    }

    [Fact]
    public void Trim_keeps_alerting_entries_and_a_bounded_spread_of_the_rest()
    {
        var entries = Enumerable.Range(0, 100)
            .Select(i => new LogEntryText(i % 10 == 0 ? "ERR" : "INF", [$"line {i}"]))
            .ToList();

        var trimmed = Anonymizer.Trim(entries, maxAlerting: 5, maxInfo: 10);

        Assert.Equal(5, trimmed.Count(e => e.Level == "ERR"));
        Assert.Equal(10, trimmed.Count(e => e.Level == "INF"));
        Assert.Equal(trimmed.OrderBy(e => int.Parse(e.Lines[0][5..])).ToList(), trimmed);
    }

    [Fact]
    public void Committed_fixtures_use_crlf_and_contain_no_original_dates_or_emails()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "fixtures", "logs");
        var files = Directory.GetFiles(root, "*.log", SearchOption.AllDirectories);

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.Contains("\r\n", text);
            Assert.DoesNotMatch(@"[A-Za-z0-9._-]+@(?!example\.test)[A-Za-z0-9.-]+", text);
            Assert.DoesNotContain("2026-10-", text);
        }
    }
}
