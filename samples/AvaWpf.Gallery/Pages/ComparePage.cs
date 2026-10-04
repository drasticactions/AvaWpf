using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace AvaWpf.Gallery.Pages;

/// <summary>One control set in up to four ThemeScope columns, each with its own family, scheme and variant.</summary>
public class ComparePage : UserControl
{
    public ComparePage()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,Auto") };
        var defaults = new[] { ThemeFamily.Classic, ThemeFamily.Luna, ThemeFamily.Aero, ThemeFamily.Aero2 };
        for (var i = 0; i < 4; i++)
        {
            grid.Children.Add(Column(defaults[i], i));
        }

        Content = grid;
    }

    private static Control Column(ThemeFamily initial, int index)
    {
        var scope = new ThemeScope { Theme = initial, Child = new SampleControls() };
        var family = new ComboBox { ItemsSource = Enum.GetValues<ThemeFamily>(), SelectedItem = initial, Width = 110 };
        var scheme = new ComboBox { ItemsSource = ColorSchemes.For(initial).ToList(), SelectedIndex = 0, Width = 140 };
        var variant = new ComboBox { ItemsSource = new[] { "Light", "Dark", "HighContrast" }, SelectedIndex = 0, Width = 110 };
        family.SelectionChanged += (_, _) =>
        {
            if (family.SelectedItem is ThemeFamily f)
            {
                scope.ColorScheme = null;
                scope.Theme = f;
                scheme.ItemsSource = ColorSchemes.For(f).ToList();
                scheme.SelectedIndex = 0;
            }
        };
        scheme.SelectionChanged += (_, _) =>
        {
            if (scheme.SelectedItem is string s)
            {
                scope.ColorScheme = s;
            }
        };
        variant.SelectionChanged += (_, _) => scope.RequestedThemeVariant = variant.SelectedItem switch
        {
            "Dark" => ThemeVariant.Dark,
            "HighContrast" => WpfThemeVariants.HighContrast,
            _ => ThemeVariant.Light,
        };
        var column = new StackPanel { Spacing = 4, Margin = new Thickness(0, 0, 12, 0), Children = { family, scheme, variant, scope } };
        Grid.SetColumn(column, index);
        return column;
    }
}
