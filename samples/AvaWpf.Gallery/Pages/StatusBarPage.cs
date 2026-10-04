using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaWpf.Controls;

namespace AvaWpf.Gallery.Pages;

/// <summary>StatusBar: docked items, separators, a progress item and a ResizeGrip.</summary>
public class StatusBarPage : UserControl
{
    public StatusBarPage()
    {
        Content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Classes = { "Subtitle" }, Text = "Status bar (items docked right, the first fills)" },
                Framed(Editor()),
                new TextBlock { Classes = { "Subtitle" }, Text = "Plain items (strings become StatusBarItems)" },
                Framed(new StatusBar { ItemsSource = new object[] { "Ready" } }),
                new TextBlock { Classes = { "Subtitle" }, Text = "Disabled item" },
                Framed(new StatusBar { ItemsSource = new object[] { new StatusBarItem { Content = "Offline", IsEnabled = false } } }),
            },
        };
    }

    /// <summary>An editor-style bar: message, progress, caret position, mode and a resize grip.</summary>
    internal static StatusBar Editor()
    {
        static T Right<T>(T c)
            where T : Control
        {
            DockPanel.SetDock(c, Avalonia.Controls.Dock.Right);
            return c;
        }

        return new StatusBar
        {
            ItemsSource = new Control[]
            {
                Right(new StatusBarItem { Content = new ResizeGrip(), Padding = new Thickness(0), VerticalContentAlignment = VerticalAlignment.Bottom }),
                Right(new StatusBarItem { Content = "INS" }),
                Right(new Separator()),
                Right(new StatusBarItem { Content = "Ln 12, Col 4" }),
                Right(new Separator()),
                Right(new StatusBarItem { Content = new ProgressBar { Width = 120, Height = 14, Value = 62 } }),
                Right(new Separator()),
                new StatusBarItem { Content = "Saving document..." },
            },
        };
    }

    private static Border Framed(Control bar) => new()
    {
        Width = 560,
        HorizontalAlignment = HorizontalAlignment.Left,
        BorderThickness = new Thickness(1),
        [!Border.BorderBrushProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("WpfBorderBrush"),
        Child = new DockPanel { Height = 90, Children = { AtBottom(bar), new Panel() } },
    };

    private static Control AtBottom(Control c)
    {
        DockPanel.SetDock(c, Avalonia.Controls.Dock.Bottom);
        return c;
    }
}
