using MothEidol.Core.Settings;

namespace MothEidol.App.Views;

/// <summary>The word wrap choice shared by the tail and the event detail, persisted in the settings.</summary>
public sealed class WordWrapState(AppSettings settings)
{
    public event EventHandler? Changed;

    public bool Enabled
    {
        get => settings.TailWordWrap;
        set
        {
            if (settings.TailWordWrap == value) return;
            settings.TailWordWrap = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
