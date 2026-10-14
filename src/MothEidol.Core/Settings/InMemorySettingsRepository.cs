using System.Collections.Concurrent;

namespace MothEidol.Core.Settings;

/// <summary>Non-persistent store, used as a fallback when the database cannot be used.</summary>
public sealed class InMemorySettingsRepository : ISettingsRepository
{
    private readonly ConcurrentDictionary<string, string> _values = new();

    public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, string value) => _values[key] = value;
}
