using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>A menu bar drop-down draws one frame around its items, not a frame nested in another.</summary>
public class SubmenuFrameTests
{
    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2)]
    [InlineData(ThemeFamily.AeroLite)]
    [InlineData(ThemeFamily.Aero)]
    [InlineData(ThemeFamily.Luna)]
    [InlineData(ThemeFamily.Royale)]
    [InlineData(ThemeFamily.Classic)]
    [InlineData(ThemeFamily.Fluent)]
    public void Submenu_Has_One_Frame(ThemeFamily family)
    {
        var newItem = new MenuItem { Header = "New" };
        var file = new MenuItem { Header = "File", ItemsSource = new Control[] { newItem, new MenuItem { Header = "Exit" } } };
        using var scene = ChromeScene.Show(family, null, new Menu { ItemsSource = new[] { file } }, 400, 300);
        file.IsSubMenuOpen = true;
        ChromeScene.Pump(5);

        var popup = file.GetVisualDescendants().OfType<Popup>().First();
        Assert.Single(popup.Child!.GetVisualDescendants().OfType<ScrollViewer>());
    }
}
