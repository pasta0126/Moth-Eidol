using System.Globalization;
using System.Text.RegularExpressions;

namespace MothEidol.LogSim;

/// <summary>Shifts every date found in text by one constant offset, so relative times are preserved.</summary>
public sealed class DateShifter
{
    private static readonly Regex Iso = new(@"(?<!\d)(\d{4})-(\d{2})-(\d{2})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Slash = new(@"(?<!\d)(\d{2})/(\d{2})/(\d{4})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Compact = new(@"(?<!\d)(\d{8})(\d{6})?(?!\d)", RegexOptions.Compiled);

    private readonly int _days;
    private readonly HashSet<DateOnly> _known;

    /// <param name="days">Offset in days.</param>
    /// <param name="knownDates">Dates of the input files; compact forms (yyyyMMdd, ddMMyyyy) are only shifted for these, so ids are left alone.</param>
    public DateShifter(int days, IEnumerable<DateOnly> knownDates)
    {
        _days = days;
        _known = [.. knownDates];
        if (_known.Any(d => _known.Contains(d.AddDays(days))))
            throw new InvalidDataException("The date offset maps an input date onto another input date; use a larger offset.");
    }

    public IReadOnlyCollection<DateOnly> KnownDates => _known;

    public string Shift(string text)
    {
        text = Iso.Replace(text, m =>
            TryDate(m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value, "yyyyMMdd", out var d)
                ? d.AddDays(_days).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : m.Value);
        text = Slash.Replace(text, m =>
            TryDate(m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value, "ddMMyyyy", out var d)
                ? d.AddDays(_days).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : m.Value);
        return Compact.Replace(text, m =>
        {
            var digits = m.Groups[1].Value;
            var tail = m.Groups[2].Value;
            if (TryDate(digits, "yyyyMMdd", out var d) && _known.Contains(d))
                return d.AddDays(_days).ToString("yyyyMMdd", CultureInfo.InvariantCulture) + tail;
            if (TryDate(digits, "ddMMyyyy", out d) && _known.Contains(d))
                return d.AddDays(_days).ToString("ddMMyyyy", CultureInfo.InvariantCulture) + tail;
            return m.Value;
        });
    }

    /// <summary>The original dates in every textual form we rewrite, used by the leak check.</summary>
    public IEnumerable<string> OriginalForms()
    {
        foreach (var d in _known)
        {
            yield return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            yield return d.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            yield return d.ToString("ddMMyyyy", CultureInfo.InvariantCulture);
            yield return d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
    }

    private static bool TryDate(string digits, string format, out DateOnly date) =>
        DateOnly.TryParseExact(digits, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
}
