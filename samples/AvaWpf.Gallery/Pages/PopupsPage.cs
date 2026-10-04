using System;
using Avalonia;
using Avalonia.Controls;
using AvaWpf.Animations;

namespace AvaWpf.Gallery.Pages;

/// <summary>WPF's popup animation kinds (None, Fade, Slide, Scroll) on a ContextMenu.</summary>
public class PopupsPage : UserControl
{
    public PopupsPage()
    {
        var panel = new StackPanel { Spacing = 10 };
        panel.Children.Add(new TextBlock { Classes = { "Subtitle" }, Text = "Popup animations (right-click a box)" });
        foreach (var kind in Enum.GetValues<PopupAnimationKind>())
        {
            var menu = new ContextMenu { ItemsSource = new[] { new MenuItem { Header = "Cut" }, new MenuItem { Header = "Copy" }, new MenuItem { Header = "Paste" } } };
            PopupAnimation.SetKind(menu, kind);
            panel.Children.Add(new Border
            {
                Width = 260,
                Height = 40,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                BorderThickness = new Thickness(1),
                Background = Avalonia.Media.Brushes.Transparent,
                [!Border.BorderBrushProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("WpfBorderBrush"),
                ContextMenu = menu,
                Child = new TextBlock { Text = kind.ToString(), Margin = new Thickness(8) },
            });
        }

        panel.Children.Add(new TextBlock { Text = $"Duration: {PopupAnimation.Duration.TotalMilliseconds} ms (WPF Popup.AnimationDelay)." });
        var tooltip = new Button { Content = "Hover for a tooltip" };
        ToolTip.SetTip(tooltip, "A tooltip in the family's ToolTipAnimation");
        panel.Children.Add(tooltip);
        Content = panel;
    }
}
