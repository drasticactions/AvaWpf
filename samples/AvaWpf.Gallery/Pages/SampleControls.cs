using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaWpf.Gallery.Pages;

/// <summary>A compact set of controls for the compare columns.</summary>
public class SampleControls : UserControl
{
    public SampleControls()
    {
        var combo = new ComboBox { SelectedIndex = 0, ItemsSource = new[] { "Combo box", "Second" }, Width = 160 };
        Content = new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(8),
            Children =
            {
                new Menu { ItemsSource = new[] { new MenuItem { Header = "_File" }, new MenuItem { Header = "_Edit" } } },
                new WrapPanel { ItemSpacing = 6, Children = { new Button { Content = "Button" }, new Button { Content = "Default", IsDefault = true }, new ToggleButton { Content = "Toggle", IsChecked = true } } },
                new CheckBox { Content = "Check box", IsChecked = true },
                new RadioButton { Content = "Radio button", IsChecked = true },
                new TextBox { Text = "Text box", Width = 160 },
                combo,
                new ListBox { Height = 70, Width = 160, SelectedIndex = 0, ItemsSource = new[] { "List item", "Second", "Third" } },
                new ProgressBar { Value = 60, Height = 16, Width = 160 },
                new Slider { Value = 40, Width = 160 },
                new ScrollBar { Orientation = Avalonia.Layout.Orientation.Horizontal, Width = 160, Maximum = 100, ViewportSize = 30, AllowAutoHide = false },
                new TabControl { Height = 70, Width = 200, ItemsSource = new[] { new TabItem { Header = "Tab" }, new TabItem { Header = "Second" } } },
                new GroupBox { Header = "Group box", Content = new TextBlock { Text = "Content", Margin = new Thickness(4) } },
            },
        };
    }
}
