using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// A gallery-like window body: the controls of the gallery's Buttons, Text, Lists, Selection, Range and Containers pages,
/// repeated until it holds the requested number of controls.
/// </summary>
internal static class MixedContent
{
    private static readonly Func<int, Control>[] s_factories =
    [
        i => new Button { Content = "Button " + i },
        i => new ToggleButton { Content = "Toggle " + i },
        i => new CheckBox { Content = "Check " + i, IsChecked = i % 3 == 0 },
        i => new RadioButton { Content = "Radio " + i, GroupName = "g" + (i / 8) },
        i => new RepeatButton { Content = "Repeat " + i },
        i => new TextBox { Text = "Text " + i, Width = 120 },
        i => new ComboBox { ItemsSource = new[] { "One", "Two", "Three" }, SelectedIndex = 0, Width = 120 },
        i => new ListBox { ItemsSource = new[] { "Alpha", "Beta", "Gamma" }, Height = 80, Width = 120 },
        i => new Slider { Value = i % 100, Width = 120 },
        i => new ProgressBar { Value = i % 100, Width = 120 },
        i => new ScrollBar { Orientation = Orientation.Horizontal, Maximum = 100, ViewportSize = 10, Width = 120 },
        i => new TreeView { ItemsSource = new[] { new TreeViewItem { Header = "Node " + i, ItemsSource = new[] { "Leaf" } } }, Width = 120 },
        i => new TabControl { ItemsSource = new[] { new TabItem { Header = "One", Content = "1" }, new TabItem { Header = "Two", Content = "2" } }, Width = 160 },
        i => new Expander { Header = "Expander " + i, Content = new TextBlock { Text = "Content" }, IsExpanded = i % 2 == 0 },
        i => new GroupBox { Header = "Group " + i, Content = new CheckBox { Content = "Inner" } },
        i => new Menu { ItemsSource = new[] { new MenuItem { Header = "_File" }, new MenuItem { Header = "_Edit" } } },
        i => new NumericUpDown { Value = i, Width = 120 },
        i => new Label { Content = "_Label " + i },
        i => new HyperlinkButton { Content = "Link " + i },
        i => new SplitButton { Content = "Split " + i },
        i => new DropDownButton { Content = "Drop " + i },
        i => new ToggleSwitch { IsChecked = i % 2 == 0 },
        i => new DatePicker(),
        i => new Separator { Width = 120 },
    ];

    /// <summary>One control of <paramref name="type"/>, as the mix creates it.</summary>
    public static Control CreateKind(Type type, int index)
    {
        foreach (var factory in s_factories)
        {
            if (factory(index) is { } control && control.GetType() == type)
            {
                return control;
            }
        }

        throw new ArgumentException($"{type.Name} is not in the mix.", nameof(type));
    }

    /// <summary>A scrollable wrap panel with <paramref name="count"/> top-level controls.</summary>
    public static Control Create(int count)
    {
        var panel = new WrapPanel();
        for (var i = 0; i < count; i++)
        {
            panel.Children.Add(s_factories[i % s_factories.Length](i));
        }

        return new ScrollViewer { Content = panel };
    }
}
