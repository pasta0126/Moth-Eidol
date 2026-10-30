namespace MothEidol.LogSim;

public sealed class Args
{
    private readonly Dictionary<string, string> _options = new(StringComparer.OrdinalIgnoreCase);

    public Args(string[] args)
    {
        Command = args.Length > 0 ? args[0] : "";
        for (var i = 1; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Unexpected argument '{args[i]}'.");
            var key = args[i][2..];
            var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
            _options[key] = hasValue ? args[++i] : "true";
        }
    }

    public string Command { get; }

    public string Required(string key) =>
        _options.TryGetValue(key, out var value) ? value : throw new ArgumentException($"Missing --{key}.");

    public string? Get(string key) => _options.GetValueOrDefault(key);

    public int GetInt(string key, int fallback) =>
        _options.TryGetValue(key, out var value)
            ? int.TryParse(value, out var parsed) ? parsed : throw new ArgumentException($"--{key} must be a number.")
            : fallback;
}
