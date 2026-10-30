using System.Text.RegularExpressions;

namespace MothEidol.LogSim;

/// <summary>
/// Replaces sensitive values with consistent fictional ones. Generic rules (emails, tax ids, private IPs,
/// user paths, URL hosts) are built in; specific names come from <see cref="AnonymizerConfig"/>.
/// </summary>
public sealed class Sanitizer
{
    private const string NifLetters = "TRWAGMYFPDXBNJZSQVHLCKE";

    private static readonly Regex Email = new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
    private static readonly Regex Nif = new(@"(?<![A-Za-z0-9])[0-9]{8}[A-Z](?![A-Za-z0-9])", RegexOptions.Compiled);
    private static readonly Regex Nie = new(@"(?<![A-Za-z0-9])[XYZ][0-9]{7}[A-Z](?![A-Za-z0-9])", RegexOptions.Compiled);
    private static readonly Regex PrivateIp = new(
        @"(?<![\d.])(?:10(?:\.\d{1,3}){3}|192\.168(?:\.\d{1,3}){2}|172\.(?:1[6-9]|2\d|3[01])(?:\.\d{1,3}){2})(?![\d.])",
        RegexOptions.Compiled);
    private static readonly Regex UserPath = new(@"(?<prefix>[Uu]sers[\\/])(?<name>[^\\/\s""']+)", RegexOptions.Compiled);
    private static readonly Regex UncHost = new(@"(?<=\\\\)[^\\\s""']+(?=\\)", RegexOptions.Compiled);
    private static readonly Regex UrlHost = new(@"(?<=https?://)[^/\s:""'?#]+", RegexOptions.Compiled);

    private readonly (Regex Pattern, string Replacement)[] _terms;
    private readonly (Regex Pattern, string Replacement)[] _rules;
    private readonly Dictionary<string, string> _emails = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _ids = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _ips = new();
    private readonly Dictionary<string, string> _hosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _userNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _configTerms;

    public Sanitizer(AnonymizerConfig config)
    {
        _configTerms = [.. config.Terms.Keys];
        _terms = config.Terms
            .OrderByDescending(t => t.Key.Length)
            .Select(t => (new Regex(Regex.Escape(t.Key), RegexOptions.IgnoreCase | RegexOptions.Compiled), t.Value))
            .ToArray();
        _rules = config.RegexRules
            .Select(r => (new Regex(r.Pattern, RegexOptions.Compiled), r.Replacement))
            .ToArray();
    }

    public string Apply(string text)
    {
        foreach (var (pattern, replacement) in _terms)
            text = pattern.Replace(text, replacement);
        foreach (var (pattern, replacement) in _rules)
            text = pattern.Replace(text, replacement);

        text = Email.Replace(text, m => Pseudonym(_emails, m.Value, n => $"user{n:000}@example.test"));
        text = Nie.Replace(text, m => Pseudonym(_ids, m.Value, n => FakeNie(m.Value[0], n)));
        text = Nif.Replace(text, m => Pseudonym(_ids, m.Value, FakeNif));
        text = PrivateIp.Replace(text, m => Pseudonym(_ips, m.Value, n => $"192.0.2.{(n - 1) % 254 + 1}"));
        text = UserPath.Replace(text, m =>
        {
            _userNames.Add(m.Groups["name"].Value);
            return m.Groups["prefix"].Value + "dev";
        });
        text = UncHost.Replace(text, m => Pseudonym(_hosts, m.Value, n => $"server{n:000}"));
        text = UrlHost.Replace(text, m =>
            m.Value.EndsWith(".example.test", StringComparison.OrdinalIgnoreCase) || m.Value.StartsWith("192.0.2.", StringComparison.Ordinal)
                ? m.Value
                : Pseudonym(_hosts, m.Value, n => $"host{n:000}.example.test"));
        return text;
    }

    /// <summary>Original values seen so far that must not survive in the output.</summary>
    public IEnumerable<string> SensitiveValues()
    {
        foreach (var term in _configTerms) yield return term;
        foreach (var id in _ids.Keys) yield return id;
        foreach (var ip in _ips.Keys) yield return ip;
        foreach (var name in _userNames.Where(n => n.Length >= 4 && n != "dev")) yield return name;
        foreach (var email in _emails.Keys)
        {
            yield return email;
            var local = email[..email.IndexOf('@')];
            if (local.Contains('.')) yield return local;
        }
    }

    private static string Pseudonym(Dictionary<string, string> map, string original, Func<int, string> make)
    {
        if (!map.TryGetValue(original, out var fake))
        {
            fake = make(map.Count + 1);
            map[original] = fake;
        }
        return fake;
    }

    private static string FakeNif(int n)
    {
        var number = 90_000_000 + n;
        return $"{number:00000000}{NifLetters[number % 23]}";
    }

    private static string FakeNie(char prefix, int n)
    {
        var digits = 9_000_000 + n;
        var full = (prefix - 'X') * 10_000_000 + digits;
        return $"{prefix}{digits:0000000}{NifLetters[full % 23]}";
    }
}
