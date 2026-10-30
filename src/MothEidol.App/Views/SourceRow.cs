using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using MothEidol.Core.Logs;

namespace MothEidol.App.Views;

/// <summary>A source as shown in the lists: its configuration plus its live status.</summary>
public sealed class SourceRow : INotifyPropertyChanged
{
    private bool _enabled;
    private SourceStatus _status;
    private string? _detail;

    public SourceRow(LogSource source, SourceStatus status, string? detail)
    {
        Id = source.Id;
        Name = source.DisplayName;
        Kind = source.Kind == SourceKind.File ? "File" : "Folder";
        Update(source, status, detail);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public long Id { get; }

    public string Name { get; }

    public string Kind { get; }

    public bool Enabled => _enabled;

    public string StatusText => _status switch
    {
        SourceStatus.Active => "Active",
        SourceStatus.Paused => "Paused",
        SourceStatus.Unavailable => "Unavailable",
        _ => "Read error",
    };

    public string? Detail => _status is SourceStatus.Unavailable or SourceStatus.ReadError ? _detail : null;

    public IBrush? StatusBrush => Resource(_status switch
    {
        SourceStatus.Active => "MothInfoBrush",
        SourceStatus.Paused => "MothTextMutedBrush",
        SourceStatus.Unavailable => "MothWarningBrush",
        _ => "MothErrorBrush",
    });

    public void Update(LogSource source, SourceStatus status, string? detail)
    {
        _enabled = source.Enabled;
        _status = status;
        _detail = detail;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    public override string ToString() => Name;

    private static IBrush? Resource(string key) =>
        Application.Current is { } app && app.TryGetResource(key, app.ActualThemeVariant, out var value)
            ? value as IBrush
            : null;
}
