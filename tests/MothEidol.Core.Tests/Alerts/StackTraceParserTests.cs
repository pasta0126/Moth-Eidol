using MothEidol.Core.Alerts;

namespace MothEidol.Core.Tests.Alerts;

public class StackTraceParserTests
{
    private const string Simple =
        "Failed to send\n" +
        "System.InvalidOperationException: boom\n" +
        "   at DocumentFetcher.Services.FileService.ProcessAsync(String path) in C:\\src\\FileService.cs:line 704\n" +
        "   at System.Threading.Tasks.Task.Run()\n" +
        "   at Microsoft.Extensions.Hosting.Host.Start()";

    [Fact]
    public void An_exception_and_its_frames_are_read_in_order()
    {
        var trace = StackTraceParser.Parse(Simple);

        var exception = Assert.Single(trace.Exceptions);
        Assert.Equal("System.InvalidOperationException", exception.Type);
        Assert.Equal("InvalidOperationException", exception.ShortType);
        Assert.Equal("boom", exception.Message);
        Assert.Equal(3, exception.Frames.Count);
        Assert.Empty(trace.OtherLines);
    }

    [Fact]
    public void Frames_with_a_source_location_are_application_code_and_framework_ones_are_not()
    {
        var frames = StackTraceParser.Parse(Simple).Exceptions[0].Frames;

        Assert.True(frames[0].IsApplication);
        Assert.Equal("C:\\src\\FileService.cs", frames[0].File);
        Assert.Equal(704, frames[0].Line);
        Assert.False(frames[1].IsApplication);
        Assert.Null(frames[1].File);
        Assert.False(frames[2].IsApplication);
    }

    [Fact]
    public void A_frame_outside_the_framework_namespaces_is_application_code_even_without_a_location()
    {
        var trace = StackTraceParser.Parse("Failed\nSystem.Exception: x\n   at Acme.Billing.Worker.Run()");

        Assert.True(trace.Exceptions[0].Frames[0].IsApplication);
    }

    [Fact]
    public void Inner_exceptions_become_separate_blocks_and_codes_and_separators_are_understood()
    {
        var trace = StackTraceParser.Parse(
            "A transient exception occurred\n" +
            "Microsoft.Data.SqlClient.SqlException (0x80131904): A network-related error occurred\n" +
            " ---> System.ComponentModel.Win32Exception (10060): A connection attempt failed\n" +
            "   at Microsoft.Data.SqlClient.TdsParser.Connect()\n" +
            "--- End of stack trace from previous location ---\n" +
            "   at Acme.Data.Repo.Load() in C:\\src\\Repo.cs:line 12");

        Assert.Equal(2, trace.Exceptions.Count);
        Assert.Equal("Microsoft.Data.SqlClient.SqlException", trace.Exceptions[0].Type);
        Assert.Equal("A network-related error occurred", trace.Exceptions[0].Message);
        Assert.Empty(trace.Exceptions[0].Frames);
        Assert.Equal("System.ComponentModel.Win32Exception", trace.Exceptions[1].Type);
        Assert.Equal("A connection attempt failed", trace.Exceptions[1].Message);
        Assert.Equal(2, trace.Exceptions[1].Frames.Count);
    }

    [Fact]
    public void Unrecognized_lines_after_the_frames_are_kept_as_plain_text()
    {
        var trace = StackTraceParser.Parse("Failed\nSystem.Exception: x\n   at A.B()\nError Number:10060,State:0,Class:20");

        Assert.Equal(["Error Number:10060,State:0,Class:20"], trace.OtherLines);
    }

    [Fact]
    public void A_multi_line_exception_message_is_kept_whole()
    {
        var trace = StackTraceParser.Parse("Failed\nSystem.Exception: first line\nsecond line of the message\n   at A.B()");

        Assert.Equal("first line\nsecond line of the message", trace.Exceptions[0].Message);
    }

    [Fact]
    public void An_entry_without_an_exception_has_none()
    {
        Assert.False(StackTraceParser.Parse("Just a message").HasException);
        Assert.False(StackTraceParser.Parse("Message\nsome continuation text").HasException);
    }

    [Fact]
    public void Every_stack_trace_in_the_fixtures_is_read_with_all_its_frames()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "fixtures", "logs");
        var frames = 0;
        var exceptions = 0;
        var parsedFrames = 0;
        foreach (var file in Directory.GetFiles(root, "*.log", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            frames += lines.Count(l => l.TrimStart().StartsWith("at ", StringComparison.Ordinal));
            foreach (var entry in Assemble(lines))
            {
                var trace = StackTraceParser.Parse(entry);
                exceptions += trace.Exceptions.Count;
                parsedFrames += trace.Exceptions.Sum(e => e.Frames.Count);
            }
        }

        Assert.True(exceptions > 0);
        Assert.True(frames > 0);
        Assert.Equal(frames, parsedFrames);
    }

    private static IEnumerable<string> Assemble(string[] lines)
    {
        var assembler = new MothEidol.Core.Logs.EntryAssembler(1, MothEidol.Core.Logs.SerilogTextParser.Instance, new MothEidol.Core.Logs.EntryOptions());
        var now = DateTimeOffset.Now;
        foreach (var line in lines)
        {
            if (assembler.Add(line, now) is { } entry) yield return entry.Message;
        }
        if (assembler.FlushIfIdle(now.AddHours(1)) is { } last) yield return last.Message;
    }
}
