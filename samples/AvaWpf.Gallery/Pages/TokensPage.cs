using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaWpf.Gallery.Pages;

/// <summary>Every generated token key of the family in effect with its value.</summary>
public class TokensPage : UserControl
{
    private readonly StackPanel _list = new() { Spacing = 2 };
    private readonly TextBox _filter = new() { PlaceholderText = "Filter", Width = 300, HorizontalAlignment = HorizontalAlignment.Left };

    public TokensPage()
    {
        _filter.TextChanged += (_, _) => Fill();
        Content = new StackPanel { Spacing = 8, Children = { new TextBlock { Classes = { "Subtitle" }, Text = "Tokens" }, _filter, _list } };
        AttachedToVisualTree += (_, _) => Fill();
    }

    private void Fill()
    {
        _list.Children.Clear();
        var family = App.Theme.ActualTheme.ToString();
        if (!TokenKeys.ByFamily.TryGetValue(family, out var keys))
        {
            return;
        }

        var filter = _filter.Text ?? string.Empty;
        foreach (var key in keys.Where(k => k.Contains(filter, StringComparison.OrdinalIgnoreCase)).Take(400))
        {
            this.TryFindResource(key, ActualThemeVariant, out var value);
            Control swatch = value switch
            {
                IBrush b => new Border { Width = 40, Height = 16, Background = b, BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray },
                Color c => new Border { Width = 40, Height = 16, Background = new SolidColorBrush(c), BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray },
                Geometry g => new Avalonia.Controls.Shapes.Path { Data = g, Width = 40, Height = 16, Stretch = Stretch.Uniform, Stroke = Brushes.Gray, StrokeThickness = 1 },
                _ => new TextBlock { Width = 40, Text = value?.ToString() ?? "-" },
            };
            _list.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { swatch, new TextBlock { Text = key } } });
        }
    }
}
