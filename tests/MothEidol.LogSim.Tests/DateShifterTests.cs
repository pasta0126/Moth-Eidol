namespace MothEidol.LogSim.Tests;

public class DateShifterTests
{
    private static DateShifter Create(int days = -300) => new(days, [new DateOnly(2026, 10, 28)]);

    [Fact]
    public void Shifts_header_and_iso_timestamps_keeping_the_time()
    {
        var result = Create().Shift("2026-10-28 10:49:09.258 +01:00 [INF] at \"2026-10-28T10:49:09.2581234+01:00\"");

        Assert.Equal("2026-01-01 10:49:09.258 +01:00 [INF] at \"2026-01-01T10:49:09.2581234+01:00\"", result);
    }

    [Fact]
    public void Shifts_known_compact_dates_in_file_names_in_both_orders()
    {
        var result = Create().Shift("NotifierX20261028.log Extract28102026060020.csv");

        Assert.Equal("NotifierX20260101.log Extract01012026060020.csv", result);
    }

    [Fact]
    public void Leaves_unknown_compact_numbers_alone()
    {
        const string text = "id 12345678 and 164568123456";

        Assert.Equal(text, Create().Shift(text));
    }

    [Fact]
    public void Shifts_slash_dates()
    {
        Assert.Equal("hoy (01/01/2026)", Create().Shift("hoy (28/10/2026)"));
    }

    [Fact]
    public void Rejects_an_offset_that_maps_a_known_date_onto_another()
    {
        var known = new[] { new DateOnly(2026, 10, 28), new DateOnly(2026, 10, 29) };

        Assert.Throws<InvalidDataException>(() => new DateShifter(1, known));
    }

    [Fact]
    public void Original_forms_cover_every_rewritten_format()
    {
        var forms = Create().OriginalForms().ToList();

        Assert.Contains("2026-10-28", forms);
        Assert.Contains("20261028", forms);
        Assert.Contains("28102026", forms);
        Assert.Contains("28/10/2026", forms);
    }
}
