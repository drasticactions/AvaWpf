using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// A button's drop-down opens like a WPF button's ContextMenu with Placement="Bottom": left-aligned under the button,
/// with no item highlighted until a key is pressed (ButtonFlyoutState, focusable MenuFlyoutPresenter).
/// </summary>
public class ButtonFlyoutTests
{
    private static MenuFlyout Menu() => new() { Items = { new MenuItem { Header = "First" }, new MenuItem { Header = "Second" } } };

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2, false)]
    [InlineData(ThemeFamily.Aero2, true)]
    [InlineData(ThemeFamily.Luna, false)]
    [InlineData(ThemeFamily.Classic, true)]
    [InlineData(ThemeFamily.Fluent, false)]
    [InlineData(ThemeFamily.AeroLite, true)]
    [InlineData(ThemeFamily.Aero, false)]
    [InlineData(ThemeFamily.Royale, true)]
    public void Drop_Down_Opens_Left_Aligned_With_Nothing_Highlighted(ThemeFamily family, bool split)
    {
        var flyout = Menu();
        Control button = split
            ? new SplitButton { Content = "Split", Flyout = flyout }
            : new DropDownButton { Content = "Drop-down", Flyout = flyout };
        button.HorizontalAlignment = HorizontalAlignment.Center;
        using var scene = ChromeScene.Show(family, null, button, 400, 200);

        // A SplitButton's menu opens from its arrow part, a DropDownButton's from anywhere on it.
        var p = button.TranslatePoint(new Point(button.Bounds.Width - 4, button.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(p, MouseButton.Left);
        scene.Window.MouseUp(p, MouseButton.Left);
        ChromeScene.Pump();

        Assert.True(flyout.IsOpen);
        var items = flyout.Items.OfType<MenuItem>().ToList();
        var presenter = Assert.IsType<MenuFlyoutPresenter>(items[0].Parent);
        Assert.InRange(presenter.PointToScreen(default).X - button.PointToScreen(default).X, -2, 2);
        Assert.True(presenter.PointToScreen(default).Y >= button.PointToScreen(new Point(0, button.Bounds.Height)).Y - 2);

        Assert.DoesNotContain(items, i => i.IsSelected);

        TopLevel.GetTopLevel(presenter)!.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        ChromeScene.Pump();
        Assert.True(items[0].IsSelected);
        flyout.Hide();
    }

    [AvaloniaFact]
    public void Aero2_Drop_Down_Glyph_Is_Centred()
    {
        // The ComboBox chevron reaches above its origin (0.6 px for the drop-down, 1.6 px for the split arrow part);
        // these templates move it to its origin, at its exact size.
        var dropDown = new DropDownButton { Content = "Drop-down", Flyout = Menu() };
        var split = new SplitButton { Content = "Split", Flyout = Menu() };
        using var scene = ChromeScene.Show(ThemeFamily.Aero2, null, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { dropDown, split } }, 300, 60);

        foreach (var (host, glyphOwner, centredAcross) in new (Visual, Visual, bool)[] { (dropDown, dropDown, false), (split.GetVisualDescendants().OfType<Button>().Last(), split, true) })
        {
            var path = glyphOwner.GetVisualDescendants().OfType<Path>().Last();
            var ink = new Rect(path.TranslatePoint(path.Data!.Bounds.TopLeft, host)!.Value, path.Data.Bounds.Size);
            Assert.Equal(host.Bounds.Height / 2, ink.Center.Y, 1);
            if (centredAcross)
            {
                Assert.Equal(host.Bounds.Width / 2, ink.Center.X, 1);
            }
        }
    }
}
