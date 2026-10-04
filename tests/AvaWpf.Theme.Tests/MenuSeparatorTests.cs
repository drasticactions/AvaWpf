using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// A Separator item of a menu, context menu or submenu draws WPF's MenuItem.SeparatorStyleKey (SeparatorState's
/// :menu), not the plain Separator style.
/// </summary>
public class MenuSeparatorTests
{
    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2, 30.0)]
    [InlineData(ThemeFamily.AeroLite, 30.0)]
    [InlineData(ThemeFamily.Aero, 30.0)]
    [InlineData(ThemeFamily.Luna, 0.0)]
    [InlineData(ThemeFamily.Royale, 0.0)]
    [InlineData(ThemeFamily.Classic, 2.0)]
    [InlineData(ThemeFamily.Fluent, 0.0)]
    public void Menu_Separators_Start_Where_WPF_Starts_Them(ThemeFamily family, double lineX)
    {
        var inContext = new Separator();
        var inSubmenu = new Separator();
        var outside = new Separator();
        var menu = new ContextMenu { ItemsSource = new Control[] { new MenuItem { Header = "Cut" }, inContext, new MenuItem { Header = "Paste" } } };
        var file = new MenuItem { Header = "File", ItemsSource = new Control[] { new MenuItem { Header = "New" }, inSubmenu, new MenuItem { Header = "Exit" } } };
        var target = new Border { Width = 100, Height = 40, Background = Brushes.White, ContextMenu = menu };
        using var scene = ChromeScene.Show(family, null, new StackPanel { Children = { new Menu { ItemsSource = new[] { file } }, target, outside } }, 400, 300);
        menu.Open(target);
        file.IsSubMenuOpen = true;
        ChromeScene.Pump(5);

        foreach (var separator in new[] { inContext, inSubmenu })
        {
            Assert.Contains(SeparatorState.MenuPseudoClass, separator.Classes);
            // The drawn line (the deepest visual) starts lineX in from the menu's item column.
            var line = separator.GetVisualDescendants().OfType<Visual>().Last();
            Assert.Equal(lineX, line.TranslatePoint(default, separator.GetVisualParent()!)!.Value.X, 3);
        }

        Assert.DoesNotContain(SeparatorState.MenuPseudoClass, outside.Classes);
    }
}
