using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// Chrome layout parity: the measure and arrange results of the templates built on the code-drawn chrome
/// (ButtonChrome, ScrollChrome, BulletChrome, ClassicBorderDecorator, ListBoxChrome), asserted against the WPF
/// reference shooter's layout dumps. Each scene is the one in tools/wpf-reference/scenes; the expected rectangles are
/// copied from tools/wpf-reference/out/&lt;scene&gt;/&lt;family&gt;.&lt;scheme&gt;.normal.json (WPF under Wine,
/// RenderTargetBitmap at 96 DPI), in DIPs relative to the scene host.
/// </summary>
public class ChromeLayoutTests
{
    private static ComboBox ComboBoxScene() => new() { ItemsSource = new[] { "Item 1", "Item 2", "Item 3" }, SelectedIndex = 0, Width = 120 };

    /// <summary>
    /// The text line sets these heights: Avalonia rounds a Tahoma 8 pt line (13.28 px) up to 14 where WPF rounds it to
    /// 13 (triage.toml, layout rounding), so each edge may be 1 px off WPF's, and a centred box 1 px higher.
    /// </summary>
    private static void AssertTextRounded(Rect expected, Rect actual)
    {
        Assert.True(
            Math.Abs(expected.X - actual.X) <= 1 && Math.Abs(expected.Y - actual.Y) <= 1 &&
            Math.Abs(expected.Right - actual.Right) <= 1 && Math.Abs(expected.Bottom - actual.Bottom) <= 1,
            $"expected {expected} within 1 px, was {actual}");
    }

    private static ScrollBar VerticalScrollBarScene() => new()
    {
        Orientation = Orientation.Vertical,
        Minimum = 0,
        Maximum = 100,
        ViewportSize = 30,
        Value = 50,
        Height = 150,
        AllowAutoHide = false,
    };

    // ---------------------------------------------------------------------------------------------------------------
    // Aero ButtonChrome: its measure adds 2 px on every side to the content.

    [AvaloniaFact]
    public void Aero_ButtonChrome_Measures_The_ComboBox_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Aero, null, ComboBoxScene(), 160, 50);

        // combobox/Aero.Default.normal.json: "ToggleButton/Chrome" ButtonChrome x 20 y 14 w 120 h 22.
        Assert.Equal(new Rect(20, 14, 120, 22), scene.BoundsOf(scene.Part("Chrome")));
        // "ToggleButton/Arrow" Path x 127 y 24 w 7 h 4.
        Assert.Equal(new Rect(127, 24, 7, 4), scene.BoundsOf(scene.Part("Arrow")));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Aero ListBoxChrome: the border plus a 1 px inner gap.

    [AvaloniaFact]
    public void Aero_ListBoxChrome_Measures_The_TextBox_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Aero, null, new TextBox { Text = "Text", Width = 120 }, 160, 50);

        // textbox/Aero.Default.normal.json: "target/Bd" ListBoxChrome x 20 y 14 w 120 h 22.
        Assert.Equal(new Rect(20, 14, 120, 22), scene.BoundsOf(scene.Part("Bd")));
        // "target/PART_ContentHost" ScrollViewer x 22 y 16 w 116 h 18: the chrome arranges its child 2 px in. AvaWpf's
        // host is inside the TextBox Padding (1 px), so it starts 1 px further in; its outer edge is WPF's.
        var host = scene.BoundsOf(scene.Part("PART_ScrollViewer"));
        Assert.Equal(new Rect(22, 16, 116, 18), host.Inflate(1));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // ScrollChrome: Aero (scroll bar buttons and thumb) and Luna (the ComboBox drop-down button).

    [AvaloniaFact]
    public void Aero_ScrollChrome_Arranges_The_ScrollBar_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Aero, null, VerticalScrollBarScene(), 40, 180);

        // scrollbar-vertical/Aero.Default.normal.json: the three "Chrome" ScrollChrome elements.
        Assert.Equal(new Rect(12, 15, 17, 17), scene.BoundsOf(scene.Part("Chrome", 0)));
        Assert.Equal(new Rect(12, 77, 17, 27), scene.BoundsOf(scene.Part("Chrome", 1)));
        Assert.Equal(new Rect(12, 148, 17, 17), scene.BoundsOf(scene.Part("Chrome", 2)));
        // "target/PART_Track" Track x 12 y 32 w 17 h 116.
        Assert.Equal(new Rect(12, 32, 17, 116), scene.BoundsOf(scene.Part("PART_Track")));
    }

    [AvaloniaFact]
    public void Luna_ScrollChrome_Arranges_The_ComboBox_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Luna, ColorSchemes.NormalColor, ComboBoxScene(), 160, 50);

        // combobox/Luna.NormalColor.normal.json: "target" ComboBox x 20 y 16 w 120 h 19.
        AssertTextRounded(new Rect(20, 16, 120, 19), scene.BoundsOf(scene.Target));
        // "ToggleButton/Chrome" ScrollChrome x 121 y 18 w 17 h 15.
        AssertTextRounded(new Rect(121, 18, 17, 15), scene.BoundsOf(scene.Part("Chrome")));
        // "target/SelectedItemBorder" Border x 23 y 19 w 97 h 13: it stops where the button column (shared size
        // group ComboBoxButton) starts.
        AssertTextRounded(new Rect(23, 19, 97, 13), scene.BoundsOf(scene.Part("SelectedItemBorder")));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // BulletChrome: Aero (13 x 13 box, 12 x 12 circle) and Luna.

    [AvaloniaFact]
    public void Aero_BulletChrome_Measures_The_RadioButton_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Aero, null, new RadioButton { Content = "RadioButton" }, 160, 50);

        // radiobutton/Aero.Default.normal.json: "target" RadioButton x 39 y 17 w 82 h 16.
        Assert.Equal(new Rect(39, 17, 82, 16), scene.BoundsOf(scene.Target));
        Assert.Equal(new Size(12, 12), scene.Part("Chrome").Bounds.Size);
    }

    [AvaloniaFact]
    public void Aero_BulletChrome_Measures_The_CheckBox_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Aero, null, new CheckBox { Content = "CheckBox" }, 160, 50);

        // checkbox/Aero.Default.normal.json: "target" CheckBox x 46 y 17 w 68 h 16. The width follows the label, which
        // Avalonia rounds up (triage.toml, layout rounding); the height comes from the chrome and the text line.
        var target = scene.BoundsOf(scene.Target);
        Assert.Equal(17, target.Y);
        Assert.Equal(16, target.Height);
        Assert.InRange(target.Width, 68, 69);
        Assert.Equal(new Size(13, 13), scene.Part("Chrome").Bounds.Size);
    }

    [AvaloniaFact]
    public void Luna_BulletChrome_Measures_The_CheckBox_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Luna, ColorSchemes.NormalColor, new CheckBox { Content = "CheckBox" }, 160, 50);

        // checkbox/Luna.NormalColor.normal.json: "target" CheckBox x 49 y 18 w 62 h 13.
        AssertTextRounded(new Rect(49, 18, 62, 13), scene.BoundsOf(scene.Target));
        Assert.Equal(new Size(13, 13), scene.Part("Bullet").Bounds.Size);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // ClassicBorderDecorator: Sunken (TextBox, CheckBox), ThinRaised menu items, AltRaised scroll bar buttons.

    [AvaloniaFact]
    public void ClassicBorderDecorator_Measures_The_TextBox_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Classic, ColorSchemes.WindowsStandard, new TextBox { Text = "Text", Width = 120 }, 160, 50);

        // textbox/Classic.WindowsStandard.normal.json: "target/Bd" ClassicBorderDecorator x 20 y 16 w 120 h 19 and
        // "target/PART_ContentHost" ScrollViewer x 22 y 18 w 116 h 15 (the 2 px Sunken border).
        var bd = scene.BoundsOf(scene.Part("Bd"));
        AssertTextRounded(new Rect(20, 16, 120, 19), bd);
        var host = scene.BoundsOf(scene.Part("PART_ScrollViewer"));
        Assert.Equal(bd.Deflate(2), host);
    }

    [AvaloniaFact]
    public void ClassicBorderDecorator_Measures_The_CheckBox_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Classic, ColorSchemes.WindowsStandard, new CheckBox { Content = "CheckBox" }, 160, 50);

        // checkbox/Classic.WindowsStandard.normal.json: "target" x 49 y 18 w 62 h 13, "target/CheckMark" x 49 y 18 w 13 h 13.
        AssertTextRounded(new Rect(49, 18, 62, 13), scene.BoundsOf(scene.Target));
        Assert.Equal(new Size(13, 13), scene.Part("CheckMark").Bounds.Size);
    }

    [AvaloniaFact]
    public void ClassicBorderDecorator_Measures_The_Menu_Item_As_WPF()
    {
        var menu = new Menu { Width = 200, ItemsSource = new[] { new MenuItem { Header = "File" }, new MenuItem { Header = "Edit" } } };
        using var scene = ChromeScene.Show(ThemeFamily.Classic, ColorSchemes.WindowsStandard, menu, 220, 40);

        // menu/Classic.WindowsStandard.normal.json: "target" Menu x 10 y 10 w 200 h 20, the first
        // "MenuItem/ClassicBorder" x 10 y 11 w 28 h 18 and its "MenuItem/ContentPanel" DockPanel x 11 y 12 w 26 h 16.
        AssertTextRounded(new Rect(10, 10, 200, 20), scene.BoundsOf(scene.Target));
        var border = scene.BoundsOf(scene.Part("ClassicBorder"));
        AssertTextRounded(new Rect(10, 11, 28, 18), border);
        Assert.Equal(border.Deflate(1), scene.BoundsOf(scene.Part("ContentPanel")));
    }

    [AvaloniaFact]
    public void ClassicBorderDecorator_Arranges_The_ScrollBar_Buttons_As_WPF()
    {
        using var scene = ChromeScene.Show(ThemeFamily.Classic, ColorSchemes.WindowsStandard, VerticalScrollBarScene(), 40, 180);

        // scrollbar-vertical/Classic.WindowsStandard.normal.json: "target" ScrollBar x 12 y 15 w 16 h 150 and the two
        // "RepeatButton/ClassicBorder" x 12 y 15 w 16 h 16 and x 12 y 149 w 16 h 16.
        Assert.Equal(new Rect(12, 15, 16, 150), scene.BoundsOf(scene.Target));
        Assert.Equal(new Rect(12, 15, 16, 16), scene.BoundsOf(scene.Part("ClassicBorder", 0)));
        Assert.Equal(new Rect(12, 149, 16, 16), scene.BoundsOf(scene.Part("ClassicBorder", 1)));
    }
}
