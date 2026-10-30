namespace MothEidol.LogSim.Tests;

public class GeneratorTests
{
    private static readonly string Fixtures = Path.Combine(AppContext.BaseDirectory, "fixtures", "logs");
    private static readonly DateTimeOffset At = new(2026, 1, 1, 10, 0, 0, TimeSpan.FromHours(1));

    private static List<string> Generate(int seed, int count, IReadOnlyDictionary<string, int>? mix = null)
    {
        var generator = new EntryGenerator(TemplateLearner.Learn(Fixtures), seed, mix);
        return Enumerable.Range(0, count).SelectMany(_ => generator.Next(At)).ToList();
    }

    [Fact]
    public void Same_seed_gives_identical_output_and_different_seeds_differ()
    {
        Assert.Equal(Generate(7, 50), Generate(7, 50));
        Assert.NotEqual(Generate(7, 50), Generate(8, 50));
    }

    [Fact]
    public void Generated_headers_follow_the_serilog_format()
    {
        var headers = Generate(1, 100).Where(l => LogFormat.Header.IsMatch(l)).ToList();

        Assert.NotEmpty(headers);
        Assert.StartsWith("2026-01-01 10:00:00.000 +01:00 [", headers[0]);
    }

    [Fact]
    public void Level_mix_restricts_the_levels_produced()
    {
        var lines = Generate(3, 80, new Dictionary<string, int> { ["ERR"] = 1 });

        var levels = lines.Select(l => LogFormat.Header.Match(l)).Where(m => m.Success).Select(m => m.Groups["level"].Value);
        Assert.All(levels, level => Assert.Equal("ERR", level));
    }

    [Fact]
    public void Error_entries_carry_their_stack_trace_lines()
    {
        var entries = LogFormat.Group(Generate(5, 200, new Dictionary<string, int> { ["ERR"] = 1 }));

        Assert.Contains(entries, e => e.Lines.Count > 1);
    }

    [Fact]
    public void Timestamps_inside_messages_are_regenerated_not_copied_from_samples()
    {
        var lines = Generate(2, 300);

        Assert.DoesNotContain(lines, l => l.Contains("2025-12-28T"));
        Assert.Contains(lines, l => l.Contains("\"2026-01-01T10:00:00.0000000+01:00\""));
    }

    [Fact]
    public void Learning_from_an_empty_folder_is_an_error()
    {
        using var temp = new TempDir();

        Assert.Throws<InvalidOperationException>(() => new EntryGenerator(TemplateLearner.Learn(temp.Path), 1));
    }

    [Fact]
    public void Mix_text_is_parsed_and_validated()
    {
        Assert.Equal(70, EntryGenerator.ParseMix("inf=70,ERR=5")["INF"]);
        Assert.Throws<ArgumentException>(() => EntryGenerator.ParseMix("INF"));
    }
}
