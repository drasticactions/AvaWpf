using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace AvaWpf.Gallery.Pages;

/// <summary>The SystemColors roles of the family, scheme and variant in effect.</summary>
public class SystemColorsPage : UserControl
{
    private static readonly string[] s_roles =
    [
        "ActiveBorder", "ActiveCaption", "ActiveCaptionText", "AppWorkspace", "Control", "ControlDark", "ControlDarkDark",
        "ControlLight", "ControlLightLight", "ControlText", "Desktop", "GradientActiveCaption", "GradientInactiveCaption",
        "GrayText", "Highlight", "HighlightText", "HotTrack", "InactiveBorder", "InactiveCaption", "InactiveCaptionText",
        "Info", "InfoText", "Menu", "MenuBar", "MenuHighlight", "MenuText", "ScrollBar", "Window", "WindowFrame", "WindowText",
    ];

    public SystemColorsPage()
    {
        // The page host scrolls horizontally, so the panel gets no width to wrap at: six swatches a row.
        var panel = new WrapPanel { ItemSpacing = 8, LineSpacing = 8, MaxWidth = (6 * 150) + (5 * 8), HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var role in s_roles)
        {
            panel.Children.Add(new StackPanel
            {
                Width = 150,
                Spacing = 2,
                Children =
                {
                    new Border
                    {
                        Height = 36,
                        BorderThickness = new Thickness(1),
                        [!Border.BorderBrushProperty] = new DynamicResourceExtension("SystemColors.WindowFrameBrush"),
                        [!Border.BackgroundProperty] = new DynamicResourceExtension($"SystemColors.{role}Brush"),
                    },
                    new TextBlock { Text = role, HorizontalAlignment = HorizontalAlignment.Left },
                },
            });
        }

        Content = new StackPanel { Spacing = 8, Children = { new TextBlock { Classes = { "Subtitle" }, Text = "SystemColors" }, panel } };
    }
}
