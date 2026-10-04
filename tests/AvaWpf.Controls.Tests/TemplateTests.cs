using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Controls.Tests;

/// <summary>Template builds, resource resolution and the no-literal-colors rule for the AvaWpf.Controls themes.</summary>
public class TemplateTests
{
    /// <summary>
    /// The family x scheme combinations the matrix covers: one scheme of every family, every Luna scheme, and the
    /// Classic default and a high-contrast scheme. Templates are identical across a family's schemes; the brushes are not.
    /// </summary>
    public static readonly (ThemeFamily Family, string Scheme)[] Matrix =
    [
        (ThemeFamily.Aero2, ColorSchemes.Default),
        (ThemeFamily.AeroLite, ColorSchemes.Default),
        (ThemeFamily.Aero, ColorSchemes.Default),
        (ThemeFamily.Luna, ColorSchemes.NormalColor),
        (ThemeFamily.Luna, ColorSchemes.Metallic),
        (ThemeFamily.Luna, ColorSchemes.Homestead),
        (ThemeFamily.Royale, ColorSchemes.NormalColor),
        (ThemeFamily.Classic, ColorSchemes.WindowsStandard),
        (ThemeFamily.Classic, ColorSchemes.HighContrastBlack),
        (ThemeFamily.Fluent, ColorSchemes.Default),
    ];

    /// <summary>Light and Dark everywhere, plus high contrast for Fluent.</summary>
    private static string[] Variants(ThemeFamily family) => family is ThemeFamily.Fluent
        ? ["Light", "Dark", "HighContrast"]
        : ["Light", "Dark"];

    private static ThemeVariant Variant(string key) => key switch
    {
        "Dark" => ThemeVariant.Dark,
        "HighContrast" => WpfThemeVariants.HighContrast,
        _ => ThemeVariant.Light,
    };

    public static IEnumerable<object[]> Cases() =>
        from m in Matrix
        from v in Variants(m.Family)
        from c in Samples.Keys
        select new object[] { m.Family, m.Scheme, v, c };

    public static IEnumerable<object[]> ScannedFamilies() =>
        Enum.GetValues<ThemeFamily>().Select(f => new object[] { f });

    private static readonly Dictionary<string, (Func<Control> Create, string[] Parts)> Samples = new()
    {
        ["ToolBar"] = (() => new ToolBar { Header = "Tools", ItemsSource = new Control[] { new Button { Content = "A" }, new Separator(), new CheckBox { Content = "B" } } },
            ["PART_ToolBarPanel", "PART_Gripper", "PART_OverflowButton", "PART_HeaderPresenter"]),
        ["ToolBarTray"] = (() => new ToolBarTray { ToolBars = { new ToolBar { ItemsSource = new[] { new Button { Content = "A" } } } } }, []),
        ["StatusBar"] = (() => new StatusBar { ItemsSource = new object[] { "Ready", new StatusBarItem { Content = "Ln 1" } } }, ["PART_ItemsPresenter"]),
        ["StatusBarItem"] = (() => new StatusBarItem { Content = "Ready" }, ["PART_ContentPresenter"]),
        ["ResizeGrip"] = (() => new ResizeGrip(), []),
        ["ListView"] = (() => new ListView { ItemsSource = new[] { "One", "Two" } }, ["PART_ScrollViewer"]),
        ["ListViewGridView"] = (() => new ListView
        {
            ItemsSource = new[] { "One", "Two" },
            View = new GridView { Columns = { new GridViewColumn { Header = "Name" } } },
        }, ["PART_ScrollViewer", "PART_ItemsPresenter", "PART_HeaderRowPresenter"]),
        ["ListViewItem"] = (() => new ListViewItem { Content = "Item" }, ["PART_ContentPresenter"]),
        ["GridViewColumnHeader"] = (() => new GridViewColumnHeader { Content = "Name" }, ["PART_HeaderGripper", "PART_ContentPresenter"]),
    };

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Template_Builds(ThemeFamily family, string scheme, string variant, string control)
    {
        var (create, parts) = Samples[control];
        var instance = create();
        var scope = new ThemeScope { Theme = family, ColorScheme = scheme, RequestedThemeVariant = Variant(variant), Child = instance };
        var window = TestHelpers.Show(scope, 500, 300);

        var key = instance.GetType();
        Assert.True(instance.TryFindResource(key, instance.ActualThemeVariant, out var theme) && theme is ControlTheme,
            $"{family}.{scheme}.{variant} has no ControlTheme for {key.Name}");

        if (instance is TemplatedControl templated)
        {
            Assert.True(templated.GetVisualChildren().Any(), $"{control} built an empty visual tree in {family}.{scheme}.{variant}");
        }

        foreach (var part in parts)
        {
            Assert.True(instance.GetVisualDescendants().OfType<Control>().Any(c => c.Name == part),
                $"{control} in {family}.{scheme}.{variant} misses the part {part}");
        }

        if (instance is ListView { View: GridView })
        {
            // One header row: a nested ScrollViewer must not pick up the GridView ScrollViewer template again.
            Assert.Single(instance.GetVisualDescendants().OfType<GridViewHeaderRowPresenter>());
        }

        window.Close();
    }

    [AvaloniaFact]
    public void GridView_Rows_Use_The_Row_Presenter()
    {
        var list = new ListView
        {
            ItemsSource = new[] { "One", "Two" },
            View = new GridView { Columns = { new GridViewColumn { Header = "Name" } } },
        };
        var window = TestHelpers.Show(list);
        var items = TestHelpers.All<ListViewItem>(list).ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Contains(":gridview", i.Classes));
        Assert.All(items, i => Assert.NotNull(i.FindDescendantOfType<GridViewRowPresenter>()));
        Assert.IsType<VirtualizingStackPanel>(list.ItemsPanelRoot);
        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(ScannedFamilies))]
    public void Every_Template_Resource_Resolves(ThemeFamily family)
    {
        foreach (var scheme in ColorSchemes.For(family))
        {
            var r = new ThemeResources(family, scheme, null);
            var controls = AvaWpfControlsTheme.CreateFamilyResources(family);
            var missing = new List<string>();
            foreach (var variant in Variants(family).Select(Variant))
            {
                foreach (var key in ControlsXaml.ReferencedKeys(family))
                {
                    if (!r.TryGetResource(key, variant, out _) && !controls.TryGetResource(key, variant, out _))
                    {
                        missing.Add($"{key} ({variant})");
                    }
                }
            }

            Assert.True(missing.Count == 0, $"{family}.{scheme}: {string.Join(", ", missing.Distinct().Take(30))}");
        }
    }

    private static readonly Regex s_literal = new(@"=""[^""]*#[0-9A-Fa-f]{3,8}\b[^""]*""", RegexOptions.Compiled);

    [AvaloniaTheory]
    [MemberData(nameof(ScannedFamilies))]
    public void Templates_Hold_No_Color_Literals(ThemeFamily family)
    {
        var offenders = new List<string>();
        foreach (var (name, text) in ControlsXaml.ControlFiles(family))
        {
            var withoutComments = Regex.Replace(text, "<!--.*?-->", string.Empty, RegexOptions.Singleline);
            foreach (Match m in s_literal.Matches(withoutComments))
            {
                offenders.Add($"{name}: {m.Value}");
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders.Take(20)));
    }

    [AvaloniaFact]
    public void Family_Switch_Keeps_The_Addon_Themes()
    {
        var bar = new ToolBar { ItemsSource = new[] { new Button { Content = "A" } } };
        var window = TestHelpers.Show(bar);
        var app = TestApplication.Instance;
        app.Theme.Theme = ThemeFamily.Luna;
        TestHelpers.Layout(window);
        app.Theme.Theme = ThemeFamily.Aero2;
        TestHelpers.Layout(window);
        Assert.True(bar.TryFindResource(typeof(ToolBar), bar.ActualThemeVariant, out var theme) && theme is ControlTheme);
        Assert.NotNull(bar.ToolBarPanel);
        window.Close();
    }

    [AvaloniaFact]
    public void Controls_Theme_Without_AvaWpfTheme_Throws()
    {
        var app = TestApplication.Instance;
        app.Styles.Remove(app.Theme);
        var window = new Window();
        var ex = Assert.Throws<InvalidOperationException>(() => window.Styles.Add(new AvaWpfControlsTheme()));
        Assert.Contains("AvaWpfTheme", ex.Message);
    }
}
