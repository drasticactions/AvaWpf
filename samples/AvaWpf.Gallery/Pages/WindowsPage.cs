using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaWpf.Gallery.Pages;

/// <summary>The window frame of every family side by side, each in its own ThemeScope.</summary>
public class WindowsPage : UserControl
{
    public WindowsPage()
    {
        var panel = new WrapPanel { ItemSpacing = 16, LineSpacing = 16 };
        foreach (var family in Enum.GetValues<ThemeFamily>())
        {
            foreach (var kind in new[] { WindowFrameKind.Normal, WindowFrameKind.Dialog })
            {
                panel.Children.Add(new ThemeScope
                {
                    Theme = family,
                    Child = new StackPanel
                    {
                        Spacing = 4,
                        Children =
                        {
                            new TextBlock { Text = $"{family} ({kind})" },
                            new WindowFrame
                            {
                                Title = kind == WindowFrameKind.Dialog ? "Properties" : "Untitled - Notepad",
                                Kind = kind,
                                Width = 300,
                                Height = 170,
                                Content = new StackPanel
                                {
                                    Margin = new Thickness(8),
                                    Spacing = 6,
                                    Children =
                                    {
                                        new TextBlock { Text = "Client area" },
                                        new Button { Content = "OK", Width = 75, HorizontalAlignment = HorizontalAlignment.Left },
                                    },
                                },
                            },
                            new WindowFrame { Title = "Inactive", IsActive = false, Width = 300, Height = 60 },
                        },
                    },
                });
            }
        }

        Content = panel;
    }
}
