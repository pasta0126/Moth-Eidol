using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace MothEidol.App.Views;

/// <summary>
/// Compact, right-aligned row of actions that act on the text shown below it. It takes its own row
/// (it never overlays text or scrollbars) and wraps when the window is narrow. Declare the actions
/// as children, in the order they must appear.
/// </summary>
/// <remarks>
/// Built in code on purpose: the control exposes its children as the XAML content property, so an
/// AXAML root element would be added through <see cref="Actions"/> before the host panel exists.
/// </remarks>
public class ViewActionsBar : UserControl
{
    private readonly WrapPanel _host = new()
    {
        Orientation = Orientation.Horizontal,
        ItemSpacing = 12,
        LineSpacing = 4,
    };

    public ViewActionsBar()
    {
        HorizontalAlignment = HorizontalAlignment.Right;
        Content = _host;
    }

    [Content]
    public Avalonia.Controls.Controls Actions => _host.Children;
}
