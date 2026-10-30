using System.Collections.ObjectModel;
using Avalonia.Threading;
using MothEidol.Core.Logs;

namespace MothEidol.App.Views;

/// <summary>Keeps the observable list shown by the views in sync with the tailing service.</summary>
public sealed class SourceListModel
{
    private readonly TailingService _service;

    public SourceListModel(TailingService service)
    {
        _service = service;
        Refresh();
        service.StatusChanged += (_, _) => Dispatcher.UIThread.Post(Refresh);
    }

    public ObservableCollection<SourceRow> Rows { get; } = [];

    /// <summary>Re-reads sources and statuses, reusing existing rows so selections survive.</summary>
    public void Refresh()
    {
        var sources = _service.Sources;

        for (var i = Rows.Count - 1; i >= 0; i--)
        {
            if (sources.All(s => s.Id != Rows[i].Id)) Rows.RemoveAt(i);
        }

        foreach (var source in sources)
        {
            var (status, detail) = _service.GetStatus(source.Id);
            var row = Rows.FirstOrDefault(r => r.Id == source.Id);
            if (row is null) Rows.Add(new SourceRow(source, status, detail));
            else row.Update(source, status, detail);
        }
    }
}
