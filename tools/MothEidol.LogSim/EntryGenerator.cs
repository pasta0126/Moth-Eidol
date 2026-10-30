using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MothEidol.LogSim;

public enum SlotKind { Guid, Timestamp, Quoted, Number }

/// <summary>A message shape learned from samples: literal text with typed slots and the values seen for each slot.</summary>
public sealed class MessageTemplate
{
    public MessageTemplate(string level, string[] literals, SlotKind[] slots)
    {
        Level = level;
        Literals = literals;
        Slots = slots;
        Pools = slots.Select(_ => new List<string>()).ToArray();
    }

    public string Level { get; }
    public string[] Literals { get; }
    public SlotKind[] Slots { get; }
    public List<string>[] Pools { get; }
    public int Weight { get; set; }
    public IReadOnlyList<string>? Trace { get; set; }
}

public static class TemplateLearner
{
    private static readonly Regex Slot = new(
        @"(?<guid>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})" +
        @"|(?<ts>""?\d{4}-\d{2}-\d{2}T[\d:.]+(?:[+-]\d{2}:\d{2}|Z)?""?)" +
        @"|(?<quoted>""[^""\r\n]*"")" +
        @"|(?<num>\d+)",
        RegexOptions.Compiled);

    public static List<MessageTemplate> Learn(string samplesDir)
    {
        var byKey = new Dictionary<string, MessageTemplate>();
        var files = Directory.EnumerateFiles(samplesDir, "*.log", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal);
        foreach (var file in files)
        {
            foreach (var entry in LogFormat.Group(LogFormat.SplitLines(File.ReadAllText(file, Encoding.UTF8))))
            {
                var header = entry.Lines[0];
                var match = LogFormat.Header.Match(header);
                if (!match.Success) continue;

                var level = match.Groups["level"].Value;
                var (literals, slots, values) = Split(match.Groups["message"].Value);
                var key = level + "\u0001" + string.Join("\u0002", literals) + "\u0001" + string.Join(",", slots);
                if (!byKey.TryGetValue(key, out var template))
                {
                    template = new MessageTemplate(level, literals, slots);
                    byKey[key] = template;
                }
                template.Weight++;
                for (var i = 0; i < values.Length; i++) template.Pools[i].Add(values[i]);
                if (template.Trace is null && entry.Lines.Count > 1) template.Trace = entry.Lines.Skip(1).ToList();
            }
        }
        return [.. byKey.Values];
    }

    private static (string[] Literals, SlotKind[] Slots, string[] Values) Split(string message)
    {
        var literals = new List<string>();
        var slots = new List<SlotKind>();
        var values = new List<string>();
        var last = 0;
        foreach (Match m in Slot.Matches(message))
        {
            literals.Add(message[last..m.Index]);
            slots.Add(m.Groups["guid"].Success ? SlotKind.Guid
                : m.Groups["ts"].Success ? SlotKind.Timestamp
                : m.Groups["quoted"].Success ? SlotKind.Quoted
                : SlotKind.Number);
            values.Add(m.Value);
            last = m.Index + m.Length;
        }
        literals.Add(message[last..]);
        return ([.. literals], [.. slots], [.. values]);
    }
}

/// <summary>Produces plausible new Serilog-format entries from learned templates; deterministic for a given seed.</summary>
public sealed class EntryGenerator
{
    private readonly List<MessageTemplate> _templates;
    private readonly Random _random;
    private readonly Dictionary<string, int>? _mix;

    /// <param name="mix">Optional relative weight per level (for example INF=70, ERR=5); defaults to the sampled distribution.</param>
    public EntryGenerator(IEnumerable<MessageTemplate> templates, int seed, IReadOnlyDictionary<string, int>? mix = null)
    {
        _templates = [.. templates];
        if (_templates.Count == 0) throw new InvalidOperationException("No message templates could be learned from the samples.");
        _random = new Random(seed);
        _mix = mix is null ? null : new Dictionary<string, int>(mix);
    }

    public IReadOnlyList<string> Next(DateTimeOffset at)
    {
        var template = PickTemplate();
        var message = new StringBuilder(template.Literals[0]);
        for (var i = 0; i < template.Slots.Length; i++)
        {
            message.Append(Fill(template, i, at));
            message.Append(template.Literals[i + 1]);
        }

        var header = string.Create(CultureInfo.InvariantCulture,
            $"{at:yyyy-MM-dd HH:mm:ss.fff} {at:zzz} [{template.Level}] {message}");
        var lines = new List<string> { header };
        if (template.Trace is not null) lines.AddRange(template.Trace);
        return lines;
    }

    private MessageTemplate PickTemplate()
    {
        var pool = _templates;
        if (_mix is not null)
        {
            var levels = _templates.Select(t => t.Level).Distinct().Where(l => _mix.GetValueOrDefault(l) > 0).ToList();
            if (levels.Count == 0) throw new InvalidOperationException("The level mix selects no level present in the samples.");
            var level = Weighted(levels, l => _mix[l]);
            pool = _templates.Where(t => t.Level == level).ToList();
        }
        return Weighted(pool, t => t.Weight);
    }

    private T Weighted<T>(IReadOnlyList<T> items, Func<T, int> weight)
    {
        var total = items.Sum(weight);
        var roll = _random.Next(total);
        foreach (var item in items)
        {
            roll -= weight(item);
            if (roll < 0) return item;
        }
        return items[^1];
    }

    private string Fill(MessageTemplate template, int slot, DateTimeOffset at) => template.Slots[slot] switch
    {
        SlotKind.Guid => NewGuid().ToString(),
        SlotKind.Timestamp => Quote(template.Pools[slot][0], at.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture)),
        _ => template.Pools[slot][_random.Next(template.Pools[slot].Count)],
    };

    /// <summary>Keeps the quotes the sampled timestamp had around it.</summary>
    private static string Quote(string sample, string value) => sample.StartsWith('"') ? $"\"{value}\"" : value;

    private Guid NewGuid()
    {
        var bytes = new byte[16];
        _random.NextBytes(bytes);
        return new Guid(bytes);
    }

    public static Dictionary<string, int> ParseMix(string text)
    {
        var mix = new Dictionary<string, int>();
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = part.Split('=');
            if (pair.Length != 2 || !int.TryParse(pair[1], out var weight) || weight < 0)
                throw new ArgumentException($"Invalid mix item '{part}'; expected LEVEL=weight.");
            mix[pair[0].ToUpperInvariant()] = weight;
        }
        return mix;
    }
}
