using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaWpf.Chrome;
using Xunit;
using AvaloniaDataGrid = Avalonia.Controls.DataGrid;

namespace AvaWpf.DataGrid.Tests;

/// <summary>The DataGrid matrix: every DataGrid control in every family, scheme and variant.</summary>
public class DataGridThemeTests
{
    /// <summary>The themed types, by test-case name.</summary>
    private static readonly Dictionary<string, Type> s_types = new()
    {
        ["DataGrid"] = typeof(AvaloniaDataGrid),
        ["DataGridCell"] = typeof(DataGridCell),
        ["DataGridRow"] = typeof(DataGridRow),
        ["DataGridRowHeader"] = typeof(DataGridRowHeader),
        ["DataGridColumnHeader"] = typeof(DataGridColumnHeader),
        ["DataGridColumnHeadersPresenter"] = typeof(DataGridColumnHeadersPresenter),
        ["DataGridRowGroupHeader"] = typeof(DataGridRowGroupHeader),
        ["DataGridDetailsPresenter"] = typeof(DataGridDetailsPresenter),
        ["TopLeftCorner"] = typeof(DataGridColumnHeader),
    };

    /// <summary>The template parts each control needs (Avalonia's DataGrid finds them by name).</summary>
    private static readonly Dictionary<string, string[]> s_parts = new()
    {
        ["DataGrid"] =
        [
            "PART_ColumnHeadersPresenter", "PART_RowsPresenter", "PART_VerticalScrollbar", "PART_HorizontalScrollbar",
            "PART_TopLeftCornerHeader", "PART_FrozenColumnScrollBarSpacer", "PART_BottomRightCorner",
        ],
        ["DataGridCell"] = ["PART_RightGridLine", "PART_ContentPresenter"],
        ["DataGridRow"] = ["PART_Root", "PART_CellsPresenter", "PART_DetailsPresenter", "PART_RowHeader", "PART_BottomGridLine"],
        ["DataGridRowHeader"] = ["PART_Root"],
        ["DataGridColumnHeader"] = ["PART_ContentPresenter"],
        ["DataGridRowGroupHeader"] =
        [
            "PART_Root", "PART_ExpanderButton", "PART_IndentSpacer", "PART_ItemCountElement", "PART_PropertyNameElement",
            "PART_RowHeader",
        ],
        ["TopLeftCorner"] = ["Arrow"],
    };

    private static readonly Regex s_literal = new(@"=""[^""]*#[0-9A-Fa-f]{3,8}\b[^""]*""", RegexOptions.Compiled);

    public static IEnumerable<object[]> FamiliesOnly() => Families.Selected().Select(f => new object[] { f });

    public static IEnumerable<object[]> MatrixSchemes() =>
        from m in Families.SelectedMatrix()
        select new object[] { m.Family, m.Scheme };

    public static IEnumerable<object[]> MatrixControls() =>
        from m in Families.SelectedMatrix()
        from v in Families.Variants(m.Family)
        from c in s_types.Keys
        select new object[] { m.Family, m.Scheme, v.Key.ToString()!, c };

    private static (Window Window, AvaloniaDataGrid Grid) Show(ThemeFamily family, string scheme, ThemeVariant variant, bool grouped = true)
    {
        var grid = SampleGrid.Create(grouped);
        var scope = new ThemeScope { Theme = family, ColorScheme = scheme, RequestedThemeVariant = variant, Child = grid };
        var window = new Window { Content = scope, Width = 640, Height = 480 };
        window.Show();
        window.UpdateLayout();
        return (window, grid);
    }

    private static IEnumerable<Control> Instances(AvaloniaDataGrid grid, string control) => control switch
    {
        "DataGrid" => [grid],
        "TopLeftCorner" => grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Where(h => h.Name == "PART_TopLeftCornerHeader"),
        "DataGridColumnHeader" => grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Where(h => h.Name != "PART_TopLeftCornerHeader"),
        _ => grid.GetVisualDescendants().OfType<Control>().Where(c => c.GetType() == s_types[control]),
    };

    [AvaloniaTheory]
    [MemberData(nameof(FamiliesOnly))]
    public void Family_Has_ControlThemes(ThemeFamily family)
    {
        var dictionary = AvaWpfDataGridTheme.CreateFamilyControls(family, ColorSchemes.For(family)[0]);
        foreach (var type in s_types.Values.Distinct())
        {
            Assert.True(dictionary.TryGetResource(type, null, out var theme), $"{family} has no ControlTheme for {type.Name}");
            Assert.IsType<ControlTheme>(theme);
        }

        foreach (var key in new[] { family + ".DataGridSelectAllButtonStyle", family + ".DataGridRowGroupExpanderButton", "DataGridCellTextBlockTheme" })
        {
            Assert.True(dictionary.TryGetResource(key, null, out var theme) && theme is ControlTheme, $"{family} misses {key}");
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(MatrixControls))]
    public void Template_Builds_With_Required_Parts(ThemeFamily family, string scheme, string variant, string control)
    {
        var (window, grid) = Show(family, scheme, Families.Variant(variant));
        var type = s_types[control];
        var instances = Instances(grid, control).ToList();
        Assert.True(instances.Count > 0, $"{family}: the sample grid realized no {control}");

        foreach (var instance in instances)
        {
            Assert.True(instance.TryFindResource(type, instance.ActualThemeVariant, out var theme) && theme is ControlTheme,
                $"{family} has no ControlTheme for {control}");
            if (instance is not TemplatedControl templated)
            {
                continue;
            }

            Assert.True(templated.GetVisualChildren().Any(), $"{control} built an empty visual tree in {family}");
            foreach (var part in s_parts.GetValueOrDefault(control, []))
            {
                Assert.True(templated.GetVisualDescendants().OfType<Control>().Any(c => c.Name == part && c.TemplatedParent == templated),
                    $"{control} in {family} misses the part {part}");
            }
        }

        if (control == "TopLeftCorner")
        {
            var corner = (DataGridColumnHeader)instances[0];
            var arrow = corner.GetVisualDescendants().OfType<Polygon>().Single(p => p.Name == "Arrow");
            Assert.Equal(3, arrow.Points.Count);
        }

        if (control == "DataGridColumnHeader" && family != ThemeFamily.Fluent)
        {
            Assert.All(instances, h => Assert.Single(h.GetVisualDescendants().OfType<DataGridHeaderBorderBase>()));
        }

        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(MatrixSchemes))]
    public void Headers_Use_The_Family_Chrome(ThemeFamily family, string scheme)
    {
        var (window, grid) = Show(family, scheme, ThemeVariant.Light);
        var expected = family switch
        {
            ThemeFamily.Aero2 or ThemeFamily.Aero => typeof(Chrome.Aero.DataGridHeaderBorder),
            ThemeFamily.AeroLite => typeof(Chrome.AeroLite.DataGridHeaderBorder),
            ThemeFamily.Luna => typeof(Chrome.Luna.DataGridHeaderBorder),
            ThemeFamily.Royale => typeof(Chrome.Royale.DataGridHeaderBorder),
            ThemeFamily.Classic => typeof(Chrome.Classic.DataGridHeaderBorder),
            _ => null,
        };

        var chromes = grid.GetVisualDescendants().OfType<DataGridHeaderBorderBase>().ToList();
        if (expected is null)
        {
            Assert.Empty(chromes);
        }
        else
        {
            Assert.NotEmpty(chromes);
            Assert.All(chromes, c => Assert.IsType(expected, c));
            Assert.Contains(chromes, c => c.Orientation == Avalonia.Layout.Orientation.Horizontal);
        }

        if (family == ThemeFamily.Luna)
        {
            var theme = scheme switch
            {
                ColorSchemes.Metallic => Chrome.Luna.ThemeColor.Metallic,
                ColorSchemes.Homestead => Chrome.Luna.ThemeColor.Homestead,
                _ => Chrome.Luna.ThemeColor.NormalColor,
            };
            Assert.All(chromes.Cast<Chrome.Luna.DataGridHeaderBorder>(), c => Assert.Equal(theme, c.ThemeColor));
        }

        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(MatrixSchemes))]
    public void Every_Template_Resource_Resolves(ThemeFamily family, string scheme)
    {
        var resources = new ThemeResources(family, scheme, null);
        var controls = AvaWpfDataGridTheme.CreateFamilyControls(family, scheme);
        var missing = new List<string>();
        foreach (var variant in Families.Variants(family))
        {
            foreach (var key in DataGridXaml.ReferencedKeys(family))
            {
                if (!resources.TryGetResource(key, variant, out _) && !controls.TryGetResource(key, variant, out _))
                {
                    missing.Add($"{key} ({variant})");
                }
            }
        }

        Assert.True(missing.Count == 0, $"{family}.{scheme}: {missing.Count} template keys do not resolve: {string.Join(", ", missing.Distinct().Take(30))}");
    }

    [AvaloniaTheory]
    [MemberData(nameof(FamiliesOnly))]
    public void Templates_Hold_No_Color_Literals(ThemeFamily family)
    {
        var files = DataGridXaml.ControlFiles(family);
        Assert.NotEmpty(files);
        var offenders = new List<string>();
        foreach (var (name, text) in files)
        {
            var withoutComments = Regex.Replace(text, "<!--.*?-->", string.Empty, RegexOptions.Singleline);
            foreach (Match m in s_literal.Matches(withoutComments))
            {
                offenders.Add($"{name}: {m.Value}");
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders.Take(20)));
    }

    [AvaloniaTheory]
    [MemberData(nameof(FamiliesOnly))]
    public void Vertical_Grid_Lines_Span_The_Cell(ThemeFamily family)
    {
        var (window, grid) = Show(family, Families.Matrix.First(m => m.Family == family).Scheme, ThemeVariant.Light, grouped: false);
        var cells = grid.GetVisualDescendants().OfType<DataGridCell>().ToList();
        Assert.NotEmpty(cells);
        foreach (var cell in cells)
        {
            var line = cell.GetVisualDescendants().OfType<Rectangle>().Single(r => r.Name == "PART_RightGridLine");
            if (line.IsVisible && line.Bounds.Width > 0)
            {
                // As WPF draws it: the cell's full height, at its right edge.
                var box = line.Bounds.TransformToAABB(line.GetVisualParent()!.TransformToVisual(cell)!.Value);
                Assert.Equal(0, box.Top);
                Assert.Equal(cell.Bounds.Height, box.Bottom);
                Assert.Equal(cell.Bounds.Width, box.Right);
            }
        }

        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(MatrixSchemes))]
    public void Sorting_Sets_The_Sort_Direction(ThemeFamily family, string scheme)
    {
        var (window, grid) = Show(family, scheme, ThemeVariant.Light, grouped: false);
        var column = grid.Columns[0];
        DataGridColumnHeader Header() => grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => Equals(h.Content, "Name"));

        AssertSort(Header(), family, null);
        column.Sort(ListSortDirection.Ascending);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AssertSort(Header(), family, ListSortDirection.Ascending);
        column.Sort(ListSortDirection.Descending);
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        AssertSort(Header(), family, ListSortDirection.Descending);

        // Another column's header stays unsorted.
        var age = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => Equals(h.Content, "Age"));
        AssertSort(age, family, null);
        window.Close();
    }

    private static void AssertSort(DataGridColumnHeader header, ThemeFamily family, ListSortDirection? expected)
    {
        if (family == ThemeFamily.Fluent)
        {
            var glyph = header.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "SortIndicator");
            Assert.Equal(expected is null ? 0 : 1, glyph.Opacity);
            if (expected is { } direction)
            {
                Assert.Equal(direction == ListSortDirection.Ascending ? FluentGlyph.SortDown : FluentGlyph.SortUp, glyph.Text);

                // The glyph is drawn from the bundled glyph font, not a fallback.
                Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(glyph.FontFamily), out var face));
                Assert.Equal("AvaWpf Fluent Glyphs", face.FamilyName);
                Assert.True(face.CharacterToGlyphMap.TryGetGlyph(char.ConvertToUtf32(glyph.Text!, 0), out _));
            }

            return;
        }

        var chrome = header.GetVisualDescendants().OfType<DataGridHeaderBorderBase>().Single();
        Assert.Equal(expected, chrome.SortDirection);
    }

    [AvaloniaFact]
    public void Header_Pointer_States_Map_To_The_Chrome()
    {
        var (window, grid) = Show(ThemeFamily.Aero2, ColorSchemes.Default, ThemeVariant.Light, grouped: false);
        var header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().First(h => Equals(h.Content, "Name"));
        var chrome = header.GetVisualDescendants().OfType<DataGridHeaderBorderBase>().Single();
        Assert.False(chrome.IsHovered);
        ((IPseudoClasses)header.Classes).Add(":pointerover");
        Assert.True(chrome.IsHovered);
        ((IPseudoClasses)header.Classes).Add(":pressed");
        Assert.True(chrome.IsPressed);
        ((IPseudoClasses)header.Classes).Remove(":pressed");
        ((IPseudoClasses)header.Classes).Remove(":pointerover");
        Assert.False(chrome.IsHovered);
        Assert.False(chrome.IsPressed);
        window.Close();
    }

    [AvaloniaFact]
    public void Family_Switch_Keeps_The_Column_Headers_Over_Their_Columns()
    {
        var app = (TestApplication)Application.Current!;
        var grid = SampleGrid.Create(grouped: false);
        var window = new Window { Content = grid, Width = 640, Height = 480 };
        window.Show();
        window.UpdateLayout();

        foreach (var family in new[] { ThemeFamily.Aero2, ThemeFamily.AeroLite, ThemeFamily.Luna, ThemeFamily.Classic, ThemeFamily.Fluent, ThemeFamily.Aero2 })
        {
            app.Theme.Theme = family;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            // The corner is as wide as the row headers, so each column header starts where its cells start.
            var header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => Equals(h.Content, "Name"));
            var cell = grid.GetVisualDescendants().OfType<DataGridCell>().First();
            Assert.Equal(cell.TranslatePoint(default, grid)!.Value.X, header.TranslatePoint(default, grid)!.Value.X, 1);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void Family_Switch_Leaves_No_Stale_Horizontal_Scroll()
    {
        var app = (TestApplication)Application.Current!;
        // Two rows fit a classic grid but not a Fluent one, whose vertical scroll bar then takes width.
        var grid = new AvaloniaDataGrid { Width = 560, Height = 90, HeadersVisibility = DataGridHeadersVisibility.Column, ItemsSource = SampleGrid.People().Take(2).ToList() };
        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Width = new DataGridLength(140), Binding = new Binding(nameof(Person.Name)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Team", Width = new DataGridLength(1, DataGridLengthUnitType.Star), Binding = new Binding(nameof(Person.Team)) });
        var window = new Window { Content = grid, Width = 640, Height = 480 };
        window.Show();
        window.UpdateLayout();

        // The columns fill the grid, so nothing scrolls sideways in any family, however it was reached.
        foreach (var family in new[] { ThemeFamily.Aero2, ThemeFamily.Classic, ThemeFamily.Fluent, ThemeFamily.Aero2, ThemeFamily.Fluent })
        {
            app.Theme.Theme = family;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var bar = grid.GetVisualDescendants().OfType<ScrollBar>().Single(s => s.Name == "PART_HorizontalScrollbar");
            Assert.False(bar.IsVisible && bar.Maximum > 0, $"{family}: horizontal extent {bar.Maximum}");
        }

        window.Close();
    }

    [AvaloniaFact]
    public void Family_Switch_Keeps_A_Grouped_Grid()
    {
        var app = (TestApplication)Application.Current!;
        var grid = SampleGrid.Create(grouped: true);
        var window = new Window { Content = grid, Width = 640, Height = 480 };
        window.Show();
        window.UpdateLayout();
        Assert.True(grid.HeadersVisibility.HasFlag(DataGridHeadersVisibility.Row));

        // A clicked group header becomes the grid's current element.
        var header = grid.GetVisualDescendants().OfType<DataGridRowGroupHeader>().First();
        var point = header.TranslatePoint(new Point(header.Bounds.Width / 2, header.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
        Assert.Contains(":current", header.Classes);

        foreach (var family in new[] { ThemeFamily.Classic, ThemeFamily.Fluent, ThemeFamily.Luna, ThemeFamily.Aero2 })
        {
            app.Theme.Theme = family;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            Assert.NotEmpty(grid.GetVisualDescendants().OfType<DataGridRowGroupHeader>());
            Assert.True(grid.HeadersVisibility.HasFlag(DataGridHeadersVisibility.Row));
        }

        window.Close();
    }

    [AvaloniaFact]
    public void Family_Switch_Swaps_The_DataGrid_Themes()
    {
        var app = (TestApplication)Application.Current!;
        var grid = SampleGrid.Create(grouped: false);
        var window = new Window { Content = grid, Width = 640, Height = 480 };
        window.Show();
        window.UpdateLayout();
        Assert.IsType<Chrome.Aero.DataGridHeaderBorder>(grid.GetVisualDescendants().OfType<DataGridHeaderBorderBase>().First());

        app.Theme.Theme = ThemeFamily.Classic;
        window.UpdateLayout();
        Assert.IsType<Chrome.Classic.DataGridHeaderBorder>(grid.GetVisualDescendants().OfType<DataGridHeaderBorderBase>().First());

        app.Theme.Theme = ThemeFamily.Fluent;
        window.UpdateLayout();
        Assert.Empty(grid.GetVisualDescendants().OfType<DataGridHeaderBorderBase>());
        window.Close();
    }

    [AvaloniaFact]
    public void Selection_Activity_Follows_Keyboard_Focus_And_Is_Inherited()
    {
        var (window, grid) = Show(ThemeFamily.Aero2, ColorSchemes.Default, ThemeVariant.Light, grouped: false);
        grid.SelectedIndex = 0;
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var row = grid.GetVisualDescendants().OfType<DataGridRow>().Single(r => r.IsSelected);
        var cells = row.GetVisualDescendants().OfType<DataGridCell>().Where(c => c.IsVisible).ToList();
        Assert.NotEmpty(cells);
        Assert.All(cells, c => Assert.True(DataGridState.GetIsRowSelected(c)));
        Assert.All(grid.GetVisualDescendants().OfType<DataGridRow>().Where(r => !r.IsSelected).SelectMany(r => r.GetVisualDescendants().OfType<DataGridCell>()),
            c => Assert.False(DataGridState.GetIsRowSelected(c)));

        // Every cell of the selected row shows the inactive selection (ControlBrush) while the grid has no focus.
        var cell = cells.Last();
        Assert.False(DataGridState.GetIsSelectionActive(cell));
        var inactive = cell.GetVisualDescendants().OfType<Border>().First(b => b.Name == "Bd").Background;
        Assert.True(cell.TryFindResource("SystemColors.InactiveSelectionHighlightBrush", cell.ActualThemeVariant, out var inactiveBrush));
        Assert.Equal(inactiveBrush, inactive);

        grid.Focus();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.True(grid.IsKeyboardFocusWithin);
        Assert.True(DataGridState.GetIsSelectionActive(cell));
        var active = cell.GetVisualDescendants().OfType<Border>().First(b => b.Name == "Bd").Background;
        Assert.NotEqual(inactive, active);
        Assert.True(cell.TryFindResource("SystemColors.HighlightBrush", cell.ActualThemeVariant, out var highlight));
        Assert.Equal(highlight, active);

        // Deselecting clears the selection look.
        grid.SelectedIndex = -1;
        Dispatcher.UIThread.RunJobs();
        Assert.False(DataGridState.GetIsRowSelected(cell));
        Assert.Equal(Avalonia.Media.Brushes.Transparent, cell.GetVisualDescendants().OfType<Border>().First(b => b.Name == "Bd").Background);
        window.Close();
    }

    [AvaloniaFact]
    public void Polygon_Points_Parse_From_A_String()
    {
        var points = DataGridState.Parse("0,10 10,10 10,0");
        Assert.Equal([new Point(0, 10), new Point(10, 10), new Point(10, 0)], points);
        Assert.Empty(DataGridState.Parse(null));
    }

    [AvaloniaFact]
    public void Missing_AvaWpfTheme_Throws_A_Clear_Error()
    {
        var app = Application.Current!;
        app.Styles.Remove(((TestApplication)app).Theme);
        var ex = Assert.Throws<InvalidOperationException>(() => app.Styles.Add(new AvaWpfDataGridTheme()));
        Assert.Contains("AvaWpfTheme", ex.Message, StringComparison.Ordinal);
        Assert.Contains("before", ex.Message, StringComparison.Ordinal);
    }

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2, 8.71)]
    [InlineData(ThemeFamily.AeroLite, 8.71)]
    [InlineData(ThemeFamily.Aero, 8.71)]
    [InlineData(ThemeFamily.Luna, 8.71)]
    [InlineData(ThemeFamily.Royale, 8.71)]
    [InlineData(ThemeFamily.Classic, 8.53)]
    public void Group_Glyph_Chevron_Sits_Where_WPF_Centers_It(ThemeFamily family, double naturalWidth)
    {
        // WPF centers the chevron Path by its natural size, which includes the stroke (Avalonia's leaves it out), and
        // does not round it: the 8.71 px wide 2 px chevron sits 5.145 px into the 19 px glyph, not 6 px.
        var (window, grid) = Show(family, ColorSchemes.For(family)[0], ThemeVariant.Light);
        var toggle = grid.GetVisualDescendants().OfType<ToggleButton>().First(t => t.Name == "PART_ExpanderButton");
        var arrow = toggle.GetVisualDescendants().OfType<Path>().Single(p => p.Name == "arrow");
        var holder = (Visual)arrow.GetVisualParent()!;

        Assert.Equal(naturalWidth, arrow.Bounds.Width, 2);
        Assert.Equal((holder.Bounds.Width - naturalWidth) / 2, arrow.Bounds.X, 2);
        window.Close();
    }

    [AvaloniaFact]
    public void Read_Only_Check_Boxes_Look_Enabled_As_In_WPF()
    {
        // Avalonia disables every display check box but the current cell's; WPF's are enabled, only not hit-testable.
        var (window, grid) = Show(ThemeFamily.Aero2, ColorSchemes.For(ThemeFamily.Aero2)[0], ThemeVariant.Light, grouped: false);
        grid.CurrentColumn = grid.Columns[0];
        Dispatcher.UIThread.RunJobs();

        var boxes = grid.GetVisualDescendants().OfType<CheckBox>().Where(b => b.Parent is DataGridCell).ToList();
        Assert.NotEmpty(boxes);
        Assert.All(boxes, b =>
        {
            Assert.True(b.IsEffectivelyEnabled);
            Assert.False(b.IsHitTestVisible);
            Assert.False(b.Focusable);
        });

        grid.IsEnabled = false;
        Assert.All(boxes, b => Assert.False(b.IsEffectivelyEnabled));
        window.Close();
    }
}
