using System.Text;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Logs;

public sealed class SourceTailerTests : IDisposable
{
    private const int Big = 1024 * 1024;
    private readonly TempFolder _temp = new();

    public void Dispose() => _temp.Dispose();

    private static LogSource FileSource(string path) => new(1, SourceKind.File, path, null, true);

    private static LogSource FolderSource(string folder, string pattern) => new(1, SourceKind.Folder, folder, pattern, true);

    private static void Append(string path, string text) => File.AppendAllText(path, text);

    /// <summary>A tailer already anchored at the end of the file's current content, as a freshly added source is.</summary>
    private static SourceTailer Anchored(LogSource source)
    {
        var tailer = new SourceTailer(source, cursor: null);
        Assert.Empty(tailer.Poll(Big).Lines);
        return tailer;
    }

    [Fact]
    public void New_source_starts_at_the_end_and_emits_only_later_lines()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "old 1\r\nold 2\r\n");
        var tailer = Anchored(FileSource(path));

        Append(path, "new 1\r\nnew 2\r\n");

        Assert.Equal(["new 1", "new 2"], tailer.Poll(Big).Lines);
        Assert.Empty(tailer.Poll(Big).Lines);
    }

    [Fact]
    public void Lines_come_out_in_order_for_both_crlf_and_lf()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        var tailer = Anchored(FileSource(path));

        Append(path, "a\r\nb\nc\r\n\r\nd\n");

        Assert.Equal(["a", "b", "c", "", "d"], tailer.Poll(Big).Lines);
    }

    [Fact]
    public void A_partial_line_is_held_back_until_it_is_completed()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        var tailer = Anchored(FileSource(path));

        Append(path, "whole\npart");
        Assert.Equal(["whole"], tailer.Poll(Big).Lines);

        Append(path, "ial\n");
        Assert.Equal(["partial"], tailer.Poll(Big).Lines);
    }

    [Fact]
    public void A_very_large_file_is_not_read_when_the_source_is_added()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, string.Concat(Enumerable.Repeat("0123456789 existing line\n", 200_000)));

        var tailer = new SourceTailer(FileSource(path), null);
        var result = tailer.Poll(Big);

        Assert.Empty(result.Lines);
        Assert.False(result.HasMore);
        Assert.Equal(new FileInfo(path).Length, tailer.Cursor!.Offset);
    }

    [Fact]
    public void Resuming_from_a_saved_cursor_emits_only_what_was_written_while_stopped()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        var first = Anchored(FileSource(path));
        Append(path, "one\ntwo\n");
        Assert.Equal(["one", "two"], first.Poll(Big).Lines);
        var saved = first.Cursor;

        Append(path, "three\nfour\n");
        var second = new SourceTailer(FileSource(path), saved);

        Assert.Equal(["three", "four"], second.Poll(Big).Lines);
    }

    [Fact]
    public void A_file_replaced_while_stopped_is_read_from_its_beginning()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "the original file content\n");
        var first = Anchored(FileSource(path));
        Append(path, "more original\n");
        first.Poll(Big);
        var saved = first.Cursor;

        File.Delete(path);
        File.WriteAllText(path, "completely different\nsecond\n");
        var second = new SourceTailer(FileSource(path), saved);

        Assert.Equal(["completely different", "second"], second.Poll(Big).Lines);
    }

    [Fact]
    public void Truncation_restarts_reading_from_the_beginning()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "2026-01-01 first run line\n");
        var tailer = Anchored(FileSource(path));
        Append(path, "2026-01-01 a\n2026-01-01 b\n");
        Assert.Equal(2, tailer.Poll(Big).Lines.Count);

        File.WriteAllText(path, "2026-02-02 fresh\n");

        Assert.Equal(["2026-02-02 fresh"], tailer.Poll(Big).Lines);
    }

    [Fact]
    public void Rename_rotation_finishes_the_old_file_then_reads_the_new_one()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "2026-01-01 start of the old file\n");
        var tailer = Anchored(FileSource(path));
        Append(path, "2026-01-01 l1\n2026-01-01 l2\n");
        Assert.Equal(2, tailer.Poll(Big).Lines.Count);

        Append(path, "2026-01-01 l3 written just before rotation\n");
        File.Move(path, path + ".1");
        File.WriteAllText(path, "2026-01-02 n1\n2026-01-02 n2\n");

        Assert.Equal(
            ["2026-01-01 l3 written just before rotation", "2026-01-02 n1", "2026-01-02 n2"],
            tailer.Poll(Big).Lines);
        Assert.Empty(tailer.Poll(Big).Lines);
    }

    [Fact]
    public void Folder_source_follows_the_newest_file_and_finishes_the_previous_one_first()
    {
        var day1 = _temp.File("Svc20260101.log");
        var day2 = _temp.File("Svc20260102.log");
        File.WriteAllText(day1, "2026-01-01 00:00 start\n");
        File.SetLastWriteTimeUtc(day1, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        var tailer = Anchored(FolderSource(_temp.Path, "Svc*.log"));

        Append(day1, "2026-01-01 23:59 last of day one\n");
        File.SetLastWriteTimeUtc(day1, new DateTime(2026, 1, 1, 23, 59, 0, DateTimeKind.Utc));
        File.WriteAllText(day2, "2026-01-02 00:00 first of day two\n2026-01-02 00:01 second\n");
        File.SetLastWriteTimeUtc(day2, new DateTime(2026, 1, 2, 0, 1, 0, DateTimeKind.Utc));

        Assert.Equal(
            ["2026-01-01 23:59 last of day one", "2026-01-02 00:00 first of day two", "2026-01-02 00:01 second"],
            tailer.Poll(Big).Lines);
        Assert.Equal(day2, tailer.Cursor!.Path);

        Append(day2, "2026-01-02 00:02 third\n");
        Assert.Equal(["2026-01-02 00:02 third"], tailer.Poll(Big).Lines);
    }

    [Fact]
    public void Folder_source_ignores_files_that_do_not_match_the_pattern()
    {
        File.WriteAllText(_temp.File("Svc20260101.log"), "x\n");
        File.WriteAllText(_temp.File("Other20260102.log"), "y\n");
        File.SetLastWriteTimeUtc(_temp.File("Other20260102.log"), DateTime.UtcNow.AddDays(1));

        var tailer = new SourceTailer(FolderSource(_temp.Path, "Svc*.log"), null);
        tailer.Poll(Big);

        Assert.Equal(_temp.File("Svc20260101.log"), tailer.Cursor!.Path);
    }

    [Fact]
    public void A_missing_file_is_unavailable_and_recovers_when_it_returns()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "2026-01-01 start\n");
        var tailer = Anchored(FileSource(path));

        File.Delete(path);
        Assert.Equal(SourceStatus.Unavailable, tailer.Poll(Big).Status);
        Assert.Equal(SourceStatus.Unavailable, tailer.Poll(Big).Status);

        File.WriteAllText(path, "2026-01-02 back again\n");
        var result = tailer.Poll(Big);

        Assert.Equal(SourceStatus.Active, result.Status);
        Assert.Equal(["2026-01-02 back again"], result.Lines);
    }

    [Fact]
    public void A_file_that_does_not_exist_yet_is_read_from_its_start_once_it_appears()
    {
        var path = _temp.File("later.log");
        var tailer = new SourceTailer(FileSource(path), null);
        Assert.Equal(SourceStatus.Unavailable, tailer.Poll(Big).Status);

        File.WriteAllText(path, "first\nsecond\n");

        Assert.Equal(["first", "second"], tailer.Poll(Big).Lines);
    }

    [Fact]
    public void A_missing_folder_is_unavailable()
    {
        var tailer = new SourceTailer(FolderSource(_temp.File("nope"), "*.log"), null);

        Assert.Equal(SourceStatus.Unavailable, tailer.Poll(Big).Status);
    }

    [Fact]
    public void Write_bursts_are_processed_in_batches_without_losing_or_repeating_lines()
    {
        var path = _temp.File("app.log");
        File.WriteAllText(path, "start\n");
        var tailer = Anchored(FileSource(path));
        var expected = Enumerable.Range(0, 20_000).Select(i => $"2026-01-01 line number {i}").ToList();
        Append(path, string.Join("\n", expected) + "\n");

        var all = new List<string>();
        var polls = 0;
        PollResult result;
        do
        {
            result = tailer.Poll(maxBytes: 100_000);
            all.AddRange(result.Lines);
            polls++;
        } while (result.HasMore);

        Assert.True(polls > 1);
        Assert.Equal(expected, all);
    }

    [Fact]
    public void Utf8_with_bom_is_decoded_without_the_bom()
    {
        var path = _temp.File("app.log");
        File.WriteAllBytes(path, [.. new byte[] { 0xEF, 0xBB, 0xBF }, .. Encoding.UTF8.GetBytes("ñandú ✅\n")]);
        var tailer = new SourceTailer(FileSource(path), new FileCursor(path, 0, null, LineEncoding.Utf8));

        Assert.Equal(["ñandú ✅"], tailer.Poll(Big).Lines);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Utf16_with_bom_is_detected_and_decoded(bool littleEndian)
    {
        var encoding = littleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
        var path = _temp.File("app.log");
        File.WriteAllBytes(path, [.. encoding.GetPreamble(), .. encoding.GetBytes("uno ✅\r\ndos\r\n")]);
        var tailer = new SourceTailer(FileSource(path), new FileCursor(path, 0, null, LineEncoding.Utf8));

        var result = tailer.Poll(Big);

        Assert.Equal(["uno ✅", "dos"], result.Lines);
        Assert.Equal(littleEndian ? LineEncoding.Utf16Le : LineEncoding.Utf16Be, tailer.Cursor!.Encoding);
    }

    [Fact]
    public void Every_anonymized_fixture_is_read_completely_and_line_for_line()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "fixtures", "logs");
        var files = Directory.GetFiles(root, "*.log", SearchOption.AllDirectories);
        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var copy = _temp.File(Path.GetFileName(file));
            File.Copy(file, copy);
            var tailer = new SourceTailer(FileSource(copy), new FileCursor(copy, 0, null, LineEncoding.Utf8));

            var lines = new List<string>();
            PollResult result;
            do
            {
                result = tailer.Poll(Big);
                lines.AddRange(result.Lines);
            } while (result.HasMore);

            Assert.Equal(File.ReadAllLines(copy), lines);
        }
    }
}
