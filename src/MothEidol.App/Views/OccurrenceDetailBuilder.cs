using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Layout;
using Avalonia.Media;
using MothEidol.Core.Alerts;
using MothEidol.Core.Logs;

namespace MothEidol.App.Views;

/// <summary>
/// Builds the readable view of one occurrence: the message, the stack trace as an ordered list of frames
/// (the application's own code highlighted, framework frames collapsed) and what happened just before.
/// </summary>
internal static class OccurrenceDetailBuilder
{
    private const string Mono = "Menlo,Consolas,Monospace";

    public static Control Build(EventOccurrence occurrence, bool wrapAll = false)
    {
        var panel = new StackPanel { Spacing = 18 };
        panel.Children.Add(StackSection(occurrence.Message));
        panel.Children.Add(ContextSection(occurrence));
        if (wrapAll) WrapEverything(panel);
        return panel;
    }

    /// <summary>Makes every line wrap to the available width instead of running off to the right.</summary>
    private static void WrapEverything(Control root)
    {
        foreach (var text in root.GetLogicalDescendants().OfType<SelectableTextBlock>())
        {
            text.TextWrapping = TextWrapping.Wrap;
            text.TextTrimming = TextTrimming.None;
        }
    }

    private static Control StackSection(string message)
    {
        var trace = StackTraceParser.Parse(message);
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(Heading(trace.HasException ? "Message and stack trace" : "Message"));
        panel.Children.Add(Selectable(message.Split('\n', 2)[0], "MothTextBrush", wrap: true));

        if (!trace.HasException)
        {
            // Continuation lines we cannot interpret are still shown, as they were logged.
            var rest = message.Contains('\n') ? message[(message.IndexOf('\n') + 1)..] : "";
            if (rest.Length > 0) panel.Children.Add(Selectable(rest, "MothTextMutedBrush", wrap: false));
            return panel;
        }

        for (var i = 0; i < trace.Exceptions.Count; i++)
        {
            var exception = trace.Exceptions[i];
            var title = exception.Message.Length == 0 ? exception.Type : $"{exception.Type}: {exception.Message}";
            var block = new StackPanel { Spacing = 3, Margin = new Thickness(i == 0 ? 0 : 16, 4, 0, 0) };
            block.Children.Add(Selectable(i == 0 ? title : $"Caused by {title}", "MothErrorBrush", wrap: true, bold: true));
            AddFrames(block, exception.Frames);
            panel.Children.Add(block);
        }

        if (trace.OtherLines.Count > 0)
            panel.Children.Add(Selectable(string.Join('\n', trace.OtherLines), "MothTextMutedBrush", wrap: false));
        return panel;
    }

    /// <summary>Application frames stand out; runs of framework frames fold behind an expander.</summary>
    private static void AddFrames(StackPanel block, IReadOnlyList<StackFrame> frames)
    {
        var folded = new List<StackFrame>();

        void FlushFolded()
        {
            if (folded.Count == 0) return;
            var lines = new StackPanel { Spacing = 1 };
            foreach (var frame in folded) lines.Children.Add(Selectable(Describe(frame), "MothTextMutedBrush", wrap: false));
            block.Children.Add(new Expander
            {
                Header = folded.Count == 1 ? "1 framework frame" : $"{folded.Count} framework frames",
                Content = lines,
                Margin = new Thickness(0, 2),
            });
            folded.Clear();
        }

        foreach (var frame in frames)
        {
            if (!frame.IsApplication)
            {
                folded.Add(frame);
                continue;
            }

            FlushFolded();
            var line = new StackPanel { Margin = new Thickness(8, 2, 0, 2) };
            line.Children.Add(Selectable(frame.Method, "MothTextBrush", wrap: false, bold: true));
            if (frame.File is not null)
                line.Children.Add(Selectable($"{frame.File}:{frame.Line}", "MothAccentSoftBrush", wrap: false));

            block.Children.Add(new Border
            {
                Child = line,
                BorderBrush = ThemeBrushes.Get("MothPrimaryBrush"),
                BorderThickness = new Thickness(3, 0, 0, 0),
                Margin = new Thickness(0, 2),
            });
        }
        FlushFolded();
    }

    private static string Describe(StackFrame frame) =>
        frame.File is null ? frame.Method : $"{frame.Method}  {frame.File}:{frame.Line}";

    private static Control ContextSection(EventOccurrence occurrence)
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(Heading("Before the error"));

        if (occurrence.Context is not { Count: > 0 } context)
        {
            panel.Children.Add(new SelectableTextBlock
            {
                Text = "No earlier entries from this source were read before this one.",
                Foreground = ThemeBrushes.Get("MothTextMutedBrush"),
            });
            return panel;
        }

        foreach (var entry in context)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("90,36,*"), ColumnSpacing = 8 };
            row.Children.Add(new SelectableTextBlock
            {
                Text = Before(occurrence.At - entry.At),
                Foreground = ThemeBrushes.Get("MothTextMutedBrush"),
                FontSize = 12,
            });

            var level = new SelectableTextBlock
            {
                Text = LevelText(entry.Level),
                FontFamily = new FontFamily(Mono),
                FontWeight = FontWeight.Bold,
                FontSize = 12,
                Foreground = ThemeBrushes.Get(LevelBrush(entry.Level)),
            };
            Grid.SetColumn(level, 1);
            row.Children.Add(level);

            var text = new SelectableTextBlock
            {
                Text = entry.Text.Split('\n', 2)[0],
                FontFamily = new FontFamily(Mono),
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = ThemeBrushes.Get("MothTextBrush"),
            };
            ToolTip.SetTip(text, $"{entry.At.ToLocalTime():yyyy-MM-dd HH:mm:ss.fff}\n{entry.Text}");
            Grid.SetColumn(text, 2);
            row.Children.Add(text);
            panel.Children.Add(row);
        }
        return panel;
    }

    private static string Before(TimeSpan gap)
    {
        if (gap < TimeSpan.FromSeconds(1)) return "just before";
        if (gap < TimeSpan.FromMinutes(1)) return $"{(int)gap.TotalSeconds} s before";
        if (gap < TimeSpan.FromHours(1)) return $"{(int)gap.TotalMinutes} min before";
        if (gap < TimeSpan.FromDays(1)) return $"{(int)gap.TotalHours} h before";
        return $"{(int)gap.TotalDays} d before";
    }

    private static string LevelText(EntryLevel level) => level switch
    {
        EntryLevel.Verbose => "VRB",
        EntryLevel.Debug => "DBG",
        EntryLevel.Information => "INF",
        EntryLevel.Warning => "WRN",
        EntryLevel.Error => "ERR",
        EntryLevel.Fatal => "FTL",
        _ => "···",
    };

    private static string LevelBrush(EntryLevel level) => level switch
    {
        EntryLevel.Information => "MothInfoBrush",
        EntryLevel.Warning => "MothWarningBrush",
        EntryLevel.Error or EntryLevel.Fatal => "MothErrorBrush",
        _ => "MothTextMutedBrush",
    };

    private static SelectableTextBlock Heading(string text) => new()
    {
        Text = text,
        FontWeight = FontWeight.SemiBold,
        FontSize = 14,
        Foreground = ThemeBrushes.Get("MothAccentBrush"),
    };

    private static SelectableTextBlock Selectable(string text, string brush, bool wrap, bool bold = false) => new()
    {
        Text = text,
        FontFamily = new FontFamily(Mono),
        FontSize = 12,
        FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
        TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
        HorizontalAlignment = HorizontalAlignment.Left,
        Foreground = ThemeBrushes.Get(brush),
    };
}
