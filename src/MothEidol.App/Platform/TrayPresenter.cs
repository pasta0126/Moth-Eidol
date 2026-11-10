using System.Globalization;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MothEidol.App.Views;
using MothEidol.Core.Notifications;

namespace MothEidol.App.Platform;

/// <summary>Shows pending alerts on the tray icon: a badge with the unread count and a tooltip that says so.</summary>
public sealed class TrayPresenter
{
    private readonly TrayIcon _tray;
    private readonly string _baseTooltip;
    private readonly UnreadCounter _counter;
    private readonly Bitmap _baseIcon;

    public TrayPresenter(TrayIcon tray, UnreadCounter counter)
    {
        _tray = tray;
        _counter = counter;
        _baseTooltip = "Moth-Eidol";
        _baseIcon = new Bitmap(AssetLoader.Open(new Uri("avares://MothEidol.App/Assets/tray.png")));

        counter.Changed += (_, _) => Dispatcher.UIThread.Post(Update);
        Update();
    }

    private void Update()
    {
        var unread = _counter.Count;
        _tray.ToolTipText = unread == 0
            ? _baseTooltip
            : $"{_baseTooltip} · {unread:N0} unread alert{(unread == 1 ? "" : "s")}";
        _tray.Icon = unread == 0 ? new WindowIcon(_baseIcon) : new WindowIcon(WithBadge(unread));
    }

    /// <summary>The tray icon with a round badge in the corner holding the unread count (9+ when larger).</summary>
    private Bitmap WithBadge(long unread)
    {
        var size = _baseIcon.PixelSize;
        var target = new RenderTargetBitmap(size, new Vector(96, 96));
        using (var context = target.CreateDrawingContext())
        {
            context.DrawImage(_baseIcon, new Rect(0, 0, size.Width, size.Height));

            var radius = size.Width * 0.30;
            var center = new Point(size.Width - radius, size.Height - radius);
            context.DrawEllipse(ThemeBrushes.Get("MothErrorBrush", ThemeVariant.Dark), null, center, radius, radius);

            var text = new FormattedText(
                unread > 9 ? "9+" : unread.ToString(CultureInfo.InvariantCulture),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold),
                radius * 1.25,
                ThemeBrushes.Get("MothTextBrush", ThemeVariant.Dark));
            context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
        }
        return target;
    }
}
