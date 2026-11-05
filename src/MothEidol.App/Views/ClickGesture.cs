using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace MothEidol.App.Views;

/// <summary>
/// Makes a container react to a click while letting the text inside it be selected: the action runs only when
/// the pointer is released close to where it was pressed (a drag is a text selection, not a click). The
/// handlers also see events the selectable text has already handled, but ignore presses on buttons.
/// </summary>
internal static class ClickGesture
{
    private const double DragThreshold = 4;

    public static void Attach(Control container, Action<Control> onClick)
    {
        Point? pressedAt = null;

        container.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            // A press on a button inside the container belongs to that button, not to the container.
            var onButton = (e.Source as Visual)?.FindAncestorOfType<Button>(includeSelf: true) is not null;
            pressedAt = !onButton && e.GetCurrentPoint(container).Properties.IsLeftButtonPressed
                ? e.GetPosition(container)
                : null;
        }, RoutingStrategies.Bubble, handledEventsToo: true);

        container.AddHandler(InputElement.PointerReleasedEvent, (_, e) =>
        {
            if (pressedAt is not { } start) return;
            pressedAt = null;

            var end = e.GetPosition(container);
            if (Math.Abs(end.X - start.X) <= DragThreshold && Math.Abs(end.Y - start.Y) <= DragThreshold)
                onClick(container);
        }, RoutingStrategies.Bubble, handledEventsToo: true);
    }
}
