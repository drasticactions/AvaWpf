using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Ribbon controls after a theme family switch.</summary>
public class ThemeSwitchTests
{
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Menu_Button_Reopens_Its_Gallery_After_A_Family_Switch(bool hiddenDuringSwitch)
    {
        var theme = TestApplication.Instance.Theme;
        theme.Theme = ThemeFamily.Aero2;
        var gallery = new RibbonGallery
        {
            Items = { new RibbonGalleryCategory { Header = "Light", ItemsSource = new[] { "One", "Two" } } },
        };
        var menu = new RibbonMenuButton { Label = "Theme", Items = { gallery } };
        var view = new RibbonTab { Header = "View", Items = { new RibbonGroup { Header = "Theme", Items = { menu } } } };
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        ribbon.Items.Add(view);
        var window = RibbonSamples.Show(ribbon);

        ribbon.SelectedItem = view;
        RibbonSamples.Settle(window);
        menu.IsDropDownOpen = true;
        RibbonSamples.Settle(window);
        Assert.True(gallery.IsAttachedToVisualTree());
        menu.IsDropDownOpen = false;
        if (hiddenDuringSwitch)
        {
            ribbon.SelectedIndex = 0;
        }

        RibbonSamples.Settle(window);
        theme.Theme = ThemeFamily.Luna;
        RibbonSamples.Settle(window);

        ribbon.SelectedItem = view;
        RibbonSamples.Settle(window);
        menu.IsDropDownOpen = true;
        RibbonSamples.Settle(window);
        Assert.True(gallery.IsAttachedToVisualTree());

        menu.IsDropDownOpen = false;
        theme.Theme = ThemeFamily.Aero2;
        window.Close();
    }
}
