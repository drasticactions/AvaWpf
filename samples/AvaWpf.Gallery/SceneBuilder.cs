using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;

namespace AvaWpf.Gallery;

/// <summary>A WPF reference scene: one control, its properties, the window size and the states to shoot.</summary>
public sealed record Scene(string Control, double Width, double Height, double Margin, IReadOnlyDictionary<string, JsonElement> Properties, IReadOnlyList<string> States);

/// <summary>
/// Loads <c>tools/wpf-reference/scenes/*.json</c> and builds the AvaWpf side of a scene, the same way the WPF shooter
/// (<c>SceneBuilder.Wpf.cs</c>) builds the WPF side.
/// </summary>
public static class SceneBuilder
{
    public static Scene Load(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var props = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (root.TryGetProperty("properties", out var p))
        {
            foreach (var prop in p.EnumerateObject())
            {
                props[prop.Name] = prop.Value.Clone();
            }
        }

        var states = root.TryGetProperty("states", out var s) ? s.EnumerateArray().Select(x => x.GetString()!).ToList() : new List<string> { "normal" };
        return new Scene(
            root.GetProperty("control").GetString()!,
            root.TryGetProperty("width", out var w) ? w.GetDouble() : 200,
            root.TryGetProperty("height", out var h) ? h.GetDouble() : 100,
            root.TryGetProperty("margin", out var m) ? m.GetDouble() : 8,
            props,
            states);
    }

    /// <summary>Builds the scene's control in <paramref name="state"/>, wrapped in a panel with the window background.</summary>
    public static Control Build(Scene scene, string state)
    {
        var control = Create(scene.Control);
        control.Margin = new Thickness(scene.Margin);
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        foreach (var (name, value) in scene.Properties)
        {
            Apply(control, name, value);
        }

        switch (state)
        {
            case "disabled":
                control.IsEnabled = false;
                break;
            case "default" when control is Button b:
                b.IsDefault = true;
                break;
            case "checked" when control is ToggleButton t:
                t.IsChecked = true;
                break;
            case "hover":
            case "pressed":
                // Real pointer input is sent by the renderer once the scene is on screen (see PointerState).
                break;
            case "focused":
                control.AttachedToVisualTree += (_, _) => control.Focus(Avalonia.Input.NavigationMethod.Tab);
                break;
        }

        // The shooter's "surface": SystemColors.Control behind the control, Fluent's application background for Fluent.
        var surface = App.Theme.ActualTheme == ThemeFamily.Fluent ? "Fluent.ApplicationBackgroundBrush" : "SystemColors.ControlBrush";
        return new Panel
        {
            [!Panel.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension(surface),
            Children = { control },
        };
    }

    /// <summary>The scene's control inside a built scene.</summary>
    public static Control Target(Control built) => ((Panel)built).Children[0];

    private static Control Create(string type) => type switch
    {
        "Button" => new Button(),
        "CheckBox" => new CheckBox(),
        "RadioButton" => new RadioButton(),
        "ToggleButton" => new ToggleButton(),
        "RepeatButton" => new RepeatButton(),
        "TextBox" => new TextBox(),
        "ComboBox" => new ComboBox(),
        "ListBox" => new ListBox(),
        "ProgressBar" => new ProgressBar(),
        "Slider" => new Slider(),
        "ScrollBar" => new ScrollBar { AllowAutoHide = false },
        "Expander" => new Expander(),
        "GroupBox" => new GroupBox(),
        "TabControl" => new TabControl(),
        "Menu" => new Menu(),
        "TreeView" => new TreeView(),
        "Label" => new Label(),
        "DataGrid" => new Avalonia.Controls.DataGrid(),
        _ => throw new NotSupportedException($"Scene control '{type}' is not supported."),
    };

    private static void Apply(Control c, string name, JsonElement v)
    {
        switch (name)
        {
            case "Content" when c is ContentControl cc: cc.Content = v.GetString(); break;
            case "Header" when c is HeaderedContentControl hc: hc.Header = v.GetString(); break;
            case "Text" when c is TextBox tb: tb.Text = v.GetString(); break;
            case "IsChecked" when c is ToggleButton t: t.IsChecked = v.ValueKind == JsonValueKind.Null ? null : v.GetBoolean(); break;
            case "IsThreeState" when c is ToggleButton t3: t3.IsThreeState = v.GetBoolean(); break;
            case "Value" when c is RangeBase r: r.Value = v.GetDouble(); break;
            case "Minimum" when c is RangeBase r: r.Minimum = v.GetDouble(); break;
            case "Maximum" when c is RangeBase r: r.Maximum = v.GetDouble(); break;
            case "ViewportSize" when c is ScrollBar sb: sb.ViewportSize = v.GetDouble(); break;
            case "IsIndeterminate" when c is ProgressBar pb: pb.IsIndeterminate = v.GetBoolean(); break;
            case "Orientation" when c is ScrollBar sb2: sb2.Orientation = Enum.Parse<Orientation>(v.GetString()!); break;
            case "Orientation" when c is Slider sl: sl.Orientation = Enum.Parse<Orientation>(v.GetString()!); break;
            case "Orientation" when c is ProgressBar p2: p2.Orientation = Enum.Parse<Orientation>(v.GetString()!); break;
            case "IsExpanded" when c is Expander e: e.IsExpanded = v.GetBoolean(); break;
            case "SelectedIndex" when c is Avalonia.Controls.DataGrid g:
                // The grid selects its current item when it loads, so the index is applied after that.
                var index = v.GetInt32();
                g.Loaded += (_, _) => g.SelectedIndex = index;
                break;
            case "SelectedIndex" when c is SelectingItemsControl s: s.SelectedIndex = v.GetInt32(); break;
            case "Rows" when c is Avalonia.Controls.DataGrid grid:
                grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = CompiledBinding.Create<SceneRow, string>(r => r.Name), CustomSortComparer = SceneRow.ByName });
                grid.Columns.Add(new DataGridTextColumn { Header = "Title", Binding = CompiledBinding.Create<SceneRow, string>(r => r.Title) });
                grid.Columns.Add(new DataGridTextColumn { Header = "Age", Binding = CompiledBinding.Create<SceneRow, int>(r => r.Age) });
                grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Remote", Binding = CompiledBinding.Create<SceneRow, bool>(r => r.IsRemote) });
                grid.ItemsSource = new DataGridCollectionView(v.EnumerateArray().Select(SceneRow.Parse).ToList());
                break;
            case "SortAscending" when c is Avalonia.Controls.DataGrid sorted:
                // The rows are listed in this order; sorting by the comparer shows the column's arrow.
                ((DataGridCollectionView)sorted.ItemsSource!).SortDescriptions.Add(DataGridSortDescription.FromComparer(SceneRow.ByName));
                break;
            case "Width": c.Width = v.GetDouble(); break;
            case "Height": c.Height = v.GetDouble(); break;
            case "Items" when c is TabControl tc:
                tc.ItemsSource = v.EnumerateArray().Select(x => new TabItem { Header = x.GetString() }).ToList();
                break;
            case "Items" when c is Menu menu:
                menu.ItemsSource = v.EnumerateArray().Select(x => new MenuItem { Header = x.GetString() }).ToList();
                break;
            case "Items" when c is TreeView tree:
                tree.ItemsSource = v.EnumerateArray().Select(x => new TreeViewItem { Header = x.GetString() }).ToList();
                break;
            case "Items" when c is ItemsControl ic:
                ic.ItemsSource = v.EnumerateArray().Select(x => x.GetString()).ToList();
                break;
        }
    }
}

/// <summary>A DataGrid scene row: <c>[name, title, age, remote]</c>.</summary>
public sealed class SceneRow
{
    public static readonly System.Collections.IComparer ByName =
        Comparer<object>.Create((x, y) => string.CompareOrdinal(((SceneRow)x).Name, ((SceneRow)y).Name));

    public string Name { get; init; } = "";

    public string Title { get; init; } = "";

    public int Age { get; init; }

    public bool IsRemote { get; set; }

    public static SceneRow Parse(JsonElement row) => new()
    {
        Name = row[0].GetString()!,
        Title = row[1].GetString()!,
        Age = row[2].GetInt32(),
        IsRemote = row[3].GetBoolean(),
    };
}
