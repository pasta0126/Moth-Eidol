using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.Core.Tests.Alerts;

public class EventGroupingTests
{
    private static string Key(string message, EntryLevel level = EntryLevel.Error) => EventGrouping.Key(level, message);

    [Fact]
    public void The_same_message_differing_only_in_ids_is_one_group()
    {
        var a = Key("Envío KO: registro 164568, DNI/NIE 90000001N, tipo 3");
        var b = Key("Envío KO: registro 164570, DNI/NIE 90000002J, tipo 4");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Guids_timestamps_and_quoted_values_are_masked()
    {
        var a = Key("Job 3f2504e0-4f89-11d3-9a0c-0305e82c3301 failed at \"2026-01-01T10:00:00.1234567+01:00\" for \"alice\"");
        var b = Key("Job 9a1b2c3d-0000-4a4a-8b8b-112233445566 failed at \"2026-02-02T11:30:00.0000000+01:00\" for \"bob\"");

        Assert.Equal(a, b);
        Assert.Contains("<guid>", a);
        Assert.Contains("<str>", a);
    }

    [Fact]
    public void Different_recipients_of_the_same_failure_are_one_group()
    {
        var a = Key("Error enviando correo a valentina.montiel@corp.es: The specified string is not in the form required");
        var b = Key("Error enviando correo a almudena.lopez@corp.es: The specified string is not in the form required");

        Assert.Equal(a, b);
        Assert.Contains("<email>", a);
    }

    [Fact]
    public void Different_messages_are_different_groups()
    {
        Assert.NotEqual(Key("Connection to the database failed"), Key("Timeout calling the provider"));
    }

    [Fact]
    public void Different_levels_are_different_groups()
    {
        Assert.NotEqual(Key("Same text", EntryLevel.Warning), Key("Same text", EntryLevel.Error));
    }

    [Fact]
    public void The_same_message_with_different_exception_types_is_a_different_group()
    {
        var timeout = Key("An error occurred\nSystem.TimeoutException: timed out\n   at A.B()");
        var io = Key("An error occurred\nSystem.IO.IOException: disk\n   at A.B()");
        var timeoutAgain = Key("An error occurred\nSystem.TimeoutException: timed out again\n   at C.D()");

        Assert.NotEqual(timeout, io);
        Assert.Equal(timeout, timeoutAgain);
    }

    [Fact]
    public void An_entry_without_a_stack_trace_has_no_exception_type()
    {
        Assert.Null(EventGrouping.ExceptionType("Just a message"));
        Assert.Null(EventGrouping.ExceptionType("Message mentioning NullReferenceException in its first line"));
    }

    [Fact]
    public void The_exception_type_is_read_from_the_first_exception_line()
    {
        Assert.Equal("System.InvalidOperationException",
            EventGrouping.ExceptionType("Failed\nSystem.InvalidOperationException: boom\n ---> System.IO.IOException: inner"));
        Assert.Equal("Microsoft.Data.SqlClient.SqlException",
            EventGrouping.ExceptionType("Failed\n   Microsoft.Data.SqlClient.SqlException (0x80131904): A network-related error"));
    }

    [Fact]
    public void The_title_is_the_first_line_of_the_message_and_is_bounded()
    {
        Assert.Equal("First line", EventGrouping.Title("First line\n   at Frame()"));
        Assert.EndsWith("…", EventGrouping.Title(new string('x', 1000)));
    }

    [Fact]
    public void Whitespace_differences_do_not_split_a_group()
    {
        Assert.Equal(Key("Failed   to   send"), Key("Failed to send "));
    }

    [Fact]
    public void Every_anonymized_fixture_error_gets_a_stable_key()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "fixtures", "logs");
        var entries = Directory.GetFiles(root, "*.log", SearchOption.AllDirectories)
            .SelectMany(f => FeedFile(f))
            .Where(e => e.Level == EntryLevel.Error)
            .ToList();

        Assert.NotEmpty(entries);
        var groups = entries.GroupBy(e => EventGrouping.Key(e.Level, e.Message)).ToList();
        Assert.True(groups.Count < entries.Count, "repeated errors should collapse into fewer groups");
    }

    private static IEnumerable<LogEntry> FeedFile(string file)
    {
        var assembler = new EntryAssembler(1, SerilogTextParser.Instance, new EntryOptions());
        var now = DateTimeOffset.Now;
        foreach (var line in File.ReadAllLines(file))
        {
            if (assembler.Add(line, now) is { } entry) yield return entry;
        }
        if (assembler.FlushIfIdle(now + TimeSpan.FromHours(1)) is { } last) yield return last;
    }
}
