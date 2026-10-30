using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MothEidol.App.Views;
using MothEidol.Core.Logs;
using MothEidol.Core.Notifications;

namespace MothEidol.App.Platform;

/// <summary>
/// Shows the toasts of a <see cref="ToastStack"/> as small borderless windows above everything else,
/// stacked upward from the bottom-right corner of the primary screen, so alerts reach the user
/// even when the main window is hidden in the tray.
/// </summary>
public sealed class ToastPresenter
{
    private const int ToastWidth = 360;
    private const int Margin = 12;
    private const long OverflowKey = -1;

    private readonly ToastStack _stack;
    private readonly Window _owner;
    private readonly Dictionary<long, Window> _windows = [];
    private readonly DispatcherTimer _expiry = new() { Interval = TimeSpan.FromSeconds(1) };

    public ToastPresenter(ToastStack stack, Window owner)
    {
        _stack = stack;
        _owner = owner;
        stack.Changed += (_, _) => Dispatcher.UIThread.Post(Sync);
        _expiry.Tick += (_, _) => _stack.Expire(DateTimeOffset.UtcNow);
        _expiry.Start();
    }

    private void Sync()
    {
        var visible = _stack.Visible;
        var wanted = visible.Select(t => t.Id).ToHashSet();
        if (_stack.Overflow > 0) wanted.Add(OverflowKey);

        foreach (var id in _windows.Keys.Where(id => !wanted.Contains(id)).ToList())
        {
            _windows[id].Close();
            _windows.Remove(id);
        }

        foreach (var toast in visible.Where(t => !_windows.ContainsKey(t.Id)))
        {
            _windows[toast.Id] = Create(
                toast.ProjectName, toast.Level, toast.Headline, toast.Detail, () => _stack.Dismiss(toast.Id));
        }

        // The "+N more" toast is rebuilt so its count stays current.
        if (_windows.Remove(OverflowKey, out var old)) old.Close();
        if (_stack.Overflow > 0)
        {
            _windows[OverflowKey] = Create(
                "More alerts", EntryLevel.Warning, $"+{_stack.Overflow:N0} more", "Open Moth-Eidol to review them",
                _stack.DismissOverflow);
        }

        Dispatcher.UIThread.Post(Layout, DispatcherPriority.Background);
    }

    private static Window Create(string project, EntryLevel level, string headline, string? detail, Action dismiss)
    {
        var content = new StackPanel { Spacing = 3 };
        content.Children.Add(new TextBlock
        {
            Text = project,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeBrushes.Get("MothAccentBrush"),
        });
        content.Children.Add(new TextBlock
        {
            Text = headline,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 3,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = ThemeBrushes.Get("MothTextBrush"),
        });
        if (!string.IsNullOrEmpty(detail))
        {
            content.Children.Add(new TextBlock
            {
                Text = detail,
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = ThemeBrushes.Get("MothTextMutedBrush"),
            });
        }

        var accent = ThemeBrushes.Get(level switch
        {
            EntryLevel.Error or EntryLevel.Fatal => "MothErrorBrush",
            EntryLevel.Warning => "MothWarningBrush",
            _ => "MothInfoBrush",
        });

        var window = new Window
        {
            Width = ToastWidth,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            ShowInTaskbar = false,
            ShowActivated = false,
            Topmost = true,
            WindowDecorations = WindowDecorations.None,
            Background = ThemeBrushes.Get("MothSurfaceBrush"),
            Content = new Border
            {
                Padding = new Thickness(14, 10),
                BorderBrush = accent,
                BorderThickness = new Thickness(4, 0, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = content,
            },
        };
        window.PointerPressed += (_, _) => dismiss(); // A click dismisses it.
        window.Show();
        return window;
    }

    /// <summary>Places the toasts from the bottom-right corner upward, newest at the bottom, "+N more" on top.</summary>
    private void Layout()
    {
        var screen = _owner.Screens.Primary ?? _owner.Screens.All.FirstOrDefault();
        if (screen is null) return;

        var area = screen.WorkingArea;
        var scale = screen.Scaling;
        var order = _stack.Visible.Select(t => t.Id).Reverse().ToList();
        if (_windows.ContainsKey(OverflowKey)) order.Add(OverflowKey);

        var bottom = area.Bottom - (int)(Margin * scale);
        foreach (var id in order)
        {
            if (!_windows.TryGetValue(id, out var window)) continue;

            var height = (int)(Math.Max(window.Bounds.Height, 60) * scale);
            var x = area.Right - (int)((ToastWidth + Margin) * scale);
            window.Position = new PixelPoint(x, bottom - height);
            bottom -= height + (int)(8 * scale);
        }
    }
}
