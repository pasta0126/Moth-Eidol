using Avalonia.Controls;
using Avalonia.Metadata;

namespace MothEidol.App.Views;

/// <summary>
/// Compact, right-aligned row of actions that act on the text shown below it. It takes its own row
/// (it never overlays text or scrollbars) and wraps when the window is narrow. Declare the actions
/// as children, in the order they must appear.
/// </summary>
public partial class ViewActionsBar : UserControl
{
    public ViewActionsBar()
    {
        InitializeComponent();
    }

    [Content]
    public Avalonia.Controls.Controls Actions => Host.Children;
}
