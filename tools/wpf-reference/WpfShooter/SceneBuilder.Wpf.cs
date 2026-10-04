using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WpfShooter;

/// <summary>
/// One scene from <c>tools/wpf-reference/scenes/*.json</c>. The AvaWpf gallery reads the same files
/// (<c>samples/AvaWpf.Gallery/SceneBuilder.cs</c>), so both sides build the same control.
/// </summary>
/// <remarks>
/// <code>
/// {
///   "control": "Button",          // WPF/Avalonia type name, the same on both sides
///   "width": 120, "height": 50,   // window client size in px
///   "margin": 8,                  // optional; the control is centered with this margin (default 8)
///   "properties": { "Content": "OK", "Width": 75, "Height": 23 },
///   "states": ["normal", "hover", "pressed", "disabled", "focused", "default", "checked"]
/// }
/// </code>
/// Properties are plain JSON values, converted with the property's TypeConverter. <c>Items</c> fills an ItemsControl
/// with strings (TabItems with that header for a TabControl). <c>Rows</c> fills a DataGrid with Name, Title, Age and
/// Remote columns from <c>[name, title, age, remote]</c> arrays; <c>SortAscending</c> shows a column's sort arrow (the
/// rows are listed in that order). The scene name is the file name.
/// </remarks>
public sealed class SceneDefinition
{
    public required string Name { get; init; }

    public required string Control { get; init; }

    public double Width { get; init; } = 120;

    public double Height { get; init; } = 50;

    public double Margin { get; init; } = 8;

    public required JsonElement Properties { get; init; }

    public required IReadOnlyList<SceneState> States { get; init; }

    public static SceneDefinition Load(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var r = doc.RootElement;
        var states = r.TryGetProperty("states", out var st)
            ? st.EnumerateArray().Select(s => SceneState.Parse(s.GetString()!)).ToList()
            : [SceneState.Parse("normal")];
        return new SceneDefinition
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Control = r.GetProperty("control").GetString()!,
            Width = r.TryGetProperty("width", out var w) ? w.GetDouble() : 120,
            Height = r.TryGetProperty("height", out var h) ? h.GetDouble() : 50,
            Margin = r.TryGetProperty("margin", out var m) ? m.GetDouble() : 8,
            Properties = r.TryGetProperty("properties", out var p) ? p.Clone() : JsonDocument.Parse("{}").RootElement.Clone(),
            States = states,
        };
    }
}

/// <summary>Kind of real input a state needs; run.py provides it with xdotool.</summary>
public enum InputKind
{
    None,
    Hover,
    Press,
}

/// <summary>
/// One state name and what it means. normal = as built; disabled = IsEnabled false; focused = keyboard focus;
/// default = IsDefault on a Button; checked = IsChecked true; hover and pressed = real pointer input.
/// </summary>
public sealed class SceneState
{
    public required string Name { get; init; }

    public IReadOnlyList<KeyValuePair<string, object?>> Set { get; init; } = [];

    public bool Focus { get; init; }

    public InputKind Input { get; init; }

    public static SceneState Parse(string name)
    {
        var set = new List<KeyValuePair<string, object?>>();
        var focus = false;
        var input = InputKind.None;
        switch (name)
        {
            case "normal": break;
            case "hover": input = InputKind.Hover; break;
            case "pressed": input = InputKind.Press; break;
            case "disabled": set.Add(new("IsEnabled", false)); break;
            case "focused": focus = true; break;
            case "default": set.Add(new("IsDefault", true)); break;
            case "checked": set.Add(new("IsChecked", true)); break;
            default: throw new FormatException($"unknown state '{name}'");
        }

        return new SceneState { Name = name, Set = set, Focus = focus, Input = input };
    }
}

/// <summary>The built scene: the host panel (window client size) and the control.</summary>
public sealed class BuiltScene
{
    public const string TargetName = "target";

    public required Grid Host { get; init; }

    public required FrameworkElement Target { get; init; }
}

/// <summary>Builds the scene's control by reflection over PresentationFramework.</summary>
public static class SceneBuilder
{
    /// <param name="surface">The host background for the family (families.json "surface").</param>
    public static BuiltScene Build(SceneDefinition scene, string surface)
    {
        var host = new Grid { Width = scene.Width, Height = scene.Height, Name = "SceneHost", UseLayoutRounding = true, SnapsToDevicePixels = true };
        ApplyBackground(host, surface);
        var type = ResolveType(scene.Control);
        var control = (FrameworkElement)(Activator.CreateInstance(type) ?? throw new InvalidOperationException($"cannot create {type}"));
        control.Name = BuiltScene.TargetName;
        control.Margin = new Thickness(scene.Margin);
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        foreach (var p in scene.Properties.EnumerateObject())
        {
            SetProperty(control, p.Name, p.Value);
        }

        host.Children.Add(control);
        return new BuiltScene { Host = host, Target = control };
    }

    /// <summary>Applies a state's static properties to the control.</summary>
    public static void ApplyState(BuiltScene built, SceneState state)
    {
        foreach (var (name, value) in state.Set)
        {
            var prop = built.Target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new FormatException($"state '{state.Name}': {built.Target.GetType().Name} has no {name}");
            prop.SetValue(built.Target, value);
        }
    }

    private static void ApplyBackground(Panel host, string spec)
    {
        if (spec.StartsWith('#'))
        {
            host.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(spec));
        }
        else if (spec.StartsWith("resource:", StringComparison.Ordinal))
        {
            host.SetResourceReference(Panel.BackgroundProperty, spec["resource:".Length..]);
        }
        else
        {
            var key = typeof(SystemColors).GetProperty(spec + "BrushKey", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                ?? throw new FormatException($"unknown SystemColors role '{spec}'");
            host.SetResourceReference(Panel.BackgroundProperty, key);
        }
    }

    private static Type ResolveType(string name)
    {
        var asm = typeof(Button).Assembly;
        return asm.GetType("System.Windows.Controls." + name)
            ?? asm.GetType("System.Windows.Controls.Primitives." + name)
            ?? throw new TypeLoadException($"control '{name}' not found in PresentationFramework");
    }

    private static void SetProperty(FrameworkElement target, string name, JsonElement value)
    {
        if (target is DataGrid grid && name == "Rows")
        {
            grid.AutoGenerateColumns = false;
            grid.CanUserAddRows = false;
            grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new System.Windows.Data.Binding(nameof(SceneRow.Name)) });
            grid.Columns.Add(new DataGridTextColumn { Header = "Title", Binding = new System.Windows.Data.Binding(nameof(SceneRow.Title)) });
            grid.Columns.Add(new DataGridTextColumn { Header = "Age", Binding = new System.Windows.Data.Binding(nameof(SceneRow.Age)) });
            grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Remote", Binding = new System.Windows.Data.Binding(nameof(SceneRow.IsRemote)) });
            grid.ItemsSource = value.EnumerateArray().Select(SceneRow.Parse).ToList();
            return;
        }

        if (target is DataGrid sorted && name == "SortAscending")
        {
            sorted.Columns.First(c => (string)c.Header == value.GetString()).SortDirection = ListSortDirection.Ascending;
            return;
        }

        if (name == "Items")
        {
            var items = target is ItemsControl ic ? ic.Items : throw new FormatException($"{target.GetType().Name} has no Items");
            foreach (var item in value.EnumerateArray())
            {
                var text = item.ValueKind == JsonValueKind.String ? item.GetString()! : item.GetRawText();
                items.Add(target is TabControl ? new TabItem { Header = text } : text);
            }

            return;
        }

        var prop = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new FormatException($"{target.GetType().Name} has no property '{name}'");
        prop.SetValue(target, Convert(value, prop.PropertyType));
    }

    private static object? Convert(JsonElement value, Type type)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False && (type == typeof(bool) || type == typeof(bool?) || type == typeof(object)))
        {
            return value.GetBoolean();
        }

        var text = value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();
        if (type == typeof(object) || type == typeof(string))
        {
            return text;
        }

        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(double))
        {
            return double.Parse(text, CultureInfo.InvariantCulture);
        }

        if (underlying == typeof(int))
        {
            return int.Parse(text, CultureInfo.InvariantCulture);
        }

        return TypeDescriptor.GetConverter(underlying).ConvertFromInvariantString(text);
    }
}

/// <summary>A DataGrid scene row: <c>[name, title, age, remote]</c>.</summary>
public sealed class SceneRow
{
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
