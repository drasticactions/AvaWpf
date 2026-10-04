using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// The themes must work under NativeAOT, where reflection bindings fail: every template binding is compiled. These
/// check the bindings that WPF writes as ancestor or parent bindings still reach their values.
/// </summary>
public class CompiledBindingTests
{
    [Fact]
    public void Theme_Xaml_Has_No_Reflection_Bindings()
    {
        foreach (var family in Enum.GetValues<ThemeFamily>())
        {
            foreach (var (name, text) in ThemeXaml.ControlFiles(family))
            {
                Assert.False(text.Contains("ReflectionBinding", StringComparison.Ordinal), $"{family} {name} uses a ReflectionBinding");
                Assert.False(text.Contains("CompileBindings=\"False\"", StringComparison.Ordinal), $"{family} {name} turns compiled bindings off");
            }
        }
    }

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2)]
    [InlineData(ThemeFamily.Aero)]
    public void ComboBoxItem_Takes_The_ComboBox_Content_Alignment(ThemeFamily family)
    {
        var item = new ComboBoxItem { Content = "One" };
        var combo = new ComboBox { Width = 120, HorizontalContentAlignment = HorizontalAlignment.Right, Items = { item } };
        using var scene = ChromeScene.Show(family, null, combo, 300, 200);
        combo.IsDropDownOpen = true;
        ChromeScene.Pump();

        Assert.Equal(HorizontalAlignment.Right, item.HorizontalContentAlignment);
    }

    [AvaloniaFact]
    public void TabItem_Header_Takes_The_TabControl_Content_Alignment()
    {
        var item = new TabItem { Header = "One", Content = "Page" };
        var tabs = new TabControl { Width = 200, Height = 100, VerticalContentAlignment = VerticalAlignment.Bottom, Items = { item } };
        using var scene = ChromeScene.Show(ThemeFamily.Aero2, null, tabs, 300, 200);

        var header = item.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_ContentPresenter");
        Assert.Equal(VerticalAlignment.Bottom, header.VerticalAlignment);
    }

    [AvaloniaFact]
    public void Fluent_Vertical_Indeterminate_Block_Is_A_Quarter_Of_The_Track()
    {
        var bar = new ProgressBar { IsIndeterminate = true, Orientation = Orientation.Vertical, Width = 8, Height = 200 };
        using var scene = ChromeScene.Show(ThemeFamily.Fluent, null, bar, 100, 260);
        ChromeScene.Pump();

        var block = (Rectangle)scene.Part("Animation");
        var track = (Control)block.GetVisualParent()!;
        Assert.True(track.Bounds.Height > 0);
        Assert.Equal(track.Bounds.Height * 0.25, block.Height, 3);
    }
}
