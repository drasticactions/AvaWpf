using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using AvaWpf.Chrome;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Gallery item alignment and the RibbonComboBox drop-down width.</summary>
public class RibbonGalleryTests
{
    private static (RibbonComboBox Box, RibbonGallery Gallery, Window Window) OpenComboBox()
    {
        var gallery = new RibbonGallery
        {
            MaxColumnCount = 1,
            Items = { new RibbonGalleryCategory { ItemsSource = new[] { "A", "Times New Roman" } } },
        };
        var box = new RibbonComboBox { SelectionBoxWidth = 200, Items = { gallery } };
        var window = RibbonSamples.Show(new StackPanel { Children = { box } });
        box.IsDropDownOpen = true;
        RibbonSamples.Settle(window);
        return (box, gallery, window);
    }

    [AvaloniaFact]
    public void Gallery_Items_Stretch_By_Default()
    {
        var (_, gallery, window) = OpenComboBox();

        var items = gallery.AllItemContainers().ToList();
        Assert.Equal(2, items.Count);
        Assert.All(items, i => Assert.Equal(HorizontalAlignment.Stretch, i.HorizontalContentAlignment));

        gallery.HorizontalContentAlignment = HorizontalAlignment.Center;
        Assert.All(items, i => Assert.Equal(HorizontalAlignment.Center, i.HorizontalContentAlignment));

        var category = gallery.Categories.Single();
        category.HorizontalContentAlignment = HorizontalAlignment.Right;
        gallery.HorizontalContentAlignment = HorizontalAlignment.Left;
        Assert.All(items, i => Assert.Equal(HorizontalAlignment.Right, i.HorizontalContentAlignment));
        window.Close();
    }

    [AvaloniaFact]
    public void ComboBox_DropDown_Is_At_Least_As_Wide_As_The_Box()
    {
        var (box, gallery, window) = OpenComboBox();

        var border = box.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "Border");
        var shadow = gallery.GetVisualAncestors().OfType<SystemDropShadowChrome>().First();
        Assert.True(shadow.Bounds.Width >= border.Bounds.Width, $"drop-down {shadow.Bounds.Width} px, box {border.Bounds.Width} px");
        window.Close();
    }

    [AvaloniaFact]
    public void Gallery_In_A_Limited_DropDown_Scrolls_Instead_Of_Growing()
    {
        var gallery = new RibbonGallery
        {
            MaxColumnCount = 1,
            Items = { new RibbonGalleryCategory { ItemsSource = Enumerable.Range(0, 200).Select(i => $"Item {i}").ToArray() } },
        };
        var box = new RibbonComboBox { DropDownHeight = 200, Items = { new RibbonMenuItem { Header = "Above" }, gallery } };
        var window = RibbonSamples.Show(new StackPanel { Children = { box } });
        box.IsDropDownOpen = true;
        RibbonSamples.Settle(window);

        var scroller = gallery.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.True(gallery.Bounds.Height <= 200, $"gallery is {gallery.Bounds.Height} px high");
        Assert.True(scroller.Extent.Height > scroller.Viewport.Height, $"extent {scroller.Extent.Height}, viewport {scroller.Viewport.Height}");
        window.Close();
    }
}
