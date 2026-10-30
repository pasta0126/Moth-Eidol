using System.Text;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Logs;

public sealed class LocationInspectorTests : IDisposable
{
    private readonly TempFolder _temp = new();

    public void Dispose() => _temp.Dispose();

    private static string Entry(int second, string level, string message) =>
        $"2026-01-05 10:00:{second:00}.000 +00:00 [{level}] {message}";

    private string Write(string name, params string[] lines)
    {
        var path = _temp.File(name);
        File.WriteAllText(path, string.Join("\r\n", lines) + "\r\n");
        return path;
    }

    [Fact]
    public void Folder_lists_matches_newest_first_and_samples_entries()
    {
        var old = Write("App20260104.log", Entry(1, "INF", "old"));
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-1));
        Write("App20260105.log", Entry(1, "INF", "started"), Entry(2, "ERR", "boom"), "   at Foo.Bar() in /src/Foo.cs:line 3");
        Write("other.txt", "x");

        var result = LocationInspector.Inspect(SourceKind.Folder, _temp.Path, "App*.log");

        Assert.True(result.CanAdd);
        Assert.Equal(2, result.TotalFiles);
        Assert.Equal("App20260105.log", result.Newest!.Name);
        Assert.Equal([EntryLevel.Information, EntryLevel.Error], result.Sample.Select(e => e.Level));
        Assert.Equal(1, result.Sample[1].ExtraLines);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void File_source_is_inspected()
    {
        var path = Write("a.log", Entry(1, "WRN", "careful"));

        var result = LocationInspector.Inspect(SourceKind.File, path, null);

        Assert.True(result.CanAdd);
        Assert.Single(result.Files);
        Assert.Equal(EntryLevel.Warning, result.Sample.Single().Level);
    }

    [Fact]
    public void Missing_path_is_an_error()
    {
        var folder = LocationInspector.Inspect(SourceKind.Folder, _temp.File("nope"), "*.log");
        var file = LocationInspector.Inspect(SourceKind.File, _temp.File("nope.log"), null);

        Assert.Equal(InspectionOutcome.Missing, folder.Outcome);
        Assert.Equal(InspectionOutcome.Missing, file.Outcome);
        Assert.False(folder.CanAdd);
        Assert.NotNull(folder.Error);
    }

    [Fact]
    public void Wrong_kind_is_an_error()
    {
        var path = Write("a.log", Entry(1, "INF", "x"));

        Assert.Equal(InspectionOutcome.Invalid, LocationInspector.Inspect(SourceKind.File, _temp.Path, null).Outcome);
        Assert.Equal(InspectionOutcome.Invalid, LocationInspector.Inspect(SourceKind.Folder, path, "*.log").Outcome);
    }

    [Fact]
    public void Empty_input_and_missing_pattern_are_errors()
    {
        Assert.Equal(InspectionOutcome.Invalid, LocationInspector.Inspect(SourceKind.File, " ", null).Outcome);
        Assert.Equal(InspectionOutcome.Invalid, LocationInspector.Inspect(SourceKind.Folder, _temp.Path, " ").Outcome);
    }

    [Fact]
    public void No_matching_files_is_a_warning_not_an_error()
    {
        Write("a.txt", "x");

        var result = LocationInspector.Inspect(SourceKind.Folder, _temp.Path, "*.log");

        Assert.True(result.CanAdd);
        Assert.Equal(0, result.TotalFiles);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Unrecognized_format_is_a_warning()
    {
        Write("a.log", "just some text", "and more text");

        var result = LocationInspector.Inspect(SourceKind.Folder, _temp.Path, "*.log");

        Assert.True(result.CanAdd);
        Assert.Empty(result.Sample);
        Assert.Contains(result.Warnings, w => w.Contains("recognized"));
    }

    [Fact]
    public void Empty_file_is_a_warning()
    {
        File.WriteAllText(_temp.File("a.log"), "");

        var result = LocationInspector.Inspect(SourceKind.Folder, _temp.Path, "*.log");

        Assert.True(result.CanAdd);
        Assert.Contains(result.Warnings, w => w.Contains("empty"));
    }

    [Fact]
    public void Utf16_file_with_bom_is_read()
    {
        var text = string.Join("\r\n", Entry(1, "INF", "hello"), Entry(2, "ERR", "boom")) + "\r\n";
        File.WriteAllBytes(_temp.File("a.log"), [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(text)]);

        var result = LocationInspector.Inspect(SourceKind.Folder, _temp.Path, "*.log");

        Assert.Equal([EntryLevel.Information, EntryLevel.Error], result.Sample.Select(e => e.Level));
    }

    [Fact]
    public void Large_file_reads_only_the_tail_and_returns_the_latest_entries()
    {
        var lines = Enumerable.Range(0, 5000).Select(i => Entry(i % 60, "INF", $"entry number {i}"));
        File.WriteAllLines(_temp.File("big.log"), lines);
        Assert.True(new FileInfo(_temp.File("big.log")).Length > LocationInspector.TailBytes * 2);

        var result = LocationInspector.Inspect(SourceKind.File, _temp.File("big.log"), null, maxSample: 3);

        Assert.Equal(3, result.Sample.Count);
        Assert.EndsWith("entry number 4999", result.Sample[^1].Message);
        Assert.StartsWith("entry number", result.Sample[0].Message); // The cut first line was dropped.
    }

    [Fact]
    public void Files_listing_is_capped_but_total_is_kept()
    {
        for (var i = 0; i < LocationInspector.MaxListedFiles + 5; i++) Write($"f{i:000}.log", Entry(1, "INF", "x"));

        var result = LocationInspector.Inspect(SourceKind.Folder, _temp.Path, "*.log");

        Assert.Equal(LocationInspector.MaxListedFiles, result.Files.Count);
        Assert.Equal(LocationInspector.MaxListedFiles + 5, result.TotalFiles);
    }
}
