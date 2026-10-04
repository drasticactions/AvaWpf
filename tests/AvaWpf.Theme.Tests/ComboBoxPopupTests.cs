using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// An open ComboBox keeps its pointer-over look while the pointer is on it, as WPF's (the combo box captures the
/// mouse), and a click on it closes the list. Avalonia's light-dismiss overlay covers the window otherwise.
/// </summary>
public class ComboBoxPopupTests
{
    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2)]
    [InlineData(ThemeFamily.AeroLite)]
    [InlineData(ThemeFamily.Aero)]
    [InlineData(ThemeFamily.Luna)]
    [InlineData(ThemeFamily.Royale)]
    [InlineData(ThemeFamily.Classic)]
    [InlineData(ThemeFamily.Fluent)]
    public void Open_ComboBox_Keeps_Pointer_Over_And_Closes_On_Click(ThemeFamily family)
    {
        var combo = new ComboBox { Width = 120, ItemsSource = new[] { "Red", "Green" }, SelectedIndex = 0 };
        var outside = new Border { Width = 100, Height = 40, Background = Avalonia.Media.Brushes.Transparent };
        using var scene = ChromeScene.Show(family, null, new StackPanel { Spacing = 20, Children = { combo, outside } }, 300, 200);
        var p = combo.TranslatePoint(new Point(40, 10), scene.Window)!.Value;

        Click(scene, p);
        Assert.True(combo.IsDropDownOpen);
        scene.Window.MouseMove(p + new Point(1, 0));
        ChromeScene.Pump();
        Assert.True(combo.IsPointerOver);

        Click(scene, p);
        Assert.False(combo.IsDropDownOpen);

        Click(scene, p);
        Assert.True(combo.IsDropDownOpen);
        Click(scene, outside.TranslatePoint(new Point(50, 20), scene.Window)!.Value);
        Assert.False(combo.IsDropDownOpen);
    }

    private static void Click(ChromeScene scene, Point p)
    {
        scene.Window.MouseMove(p);
        scene.Window.MouseDown(p, MouseButton.Left);
        scene.Window.MouseUp(p, MouseButton.Left);
        ChromeScene.Pump();
    }
}
