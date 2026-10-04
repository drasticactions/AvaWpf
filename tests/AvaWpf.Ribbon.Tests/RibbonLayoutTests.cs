using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaWpf.Ribbon.Primitives;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Ribbon layout details.</summary>
public class RibbonLayoutTests
{
    [AvaloniaFact]
    public void Image_Set_After_The_Control_Joins_The_Group_Still_Gives_The_Large_Default_Size()
    {
        // XAML may add a control to the group before it sets the control's attributes.
        var group = new RibbonGroup { Header = "Arrange" };
        var rotate = new RibbonButton { Label = "Rotate" };
        var crop = new RibbonButton { Label = "Crop" };
        group.Items.Add(rotate);
        group.Items.Add(crop);
        _ = group.ActualGroupSizeDefinitions;
        rotate.LargeImageSource = RibbonSamples.Image();
        crop.LargeImageSource = RibbonSamples.Image();
        var tab = new RibbonTab { Header = "Format", Items = { group } };
        var ribbon = new Ribbon { Items = { tab } };
        var window = RibbonSamples.Show(ribbon);
        Assert.Contains(":large", rotate.Classes);
        Assert.Contains(":large", crop.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void InRibbonGallery_Row_Uses_Its_Column_Counts_And_Hides_Category_Titles()
    {
        var category = new RibbonGalleryCategory { Header = "Styles" };
        for (var i = 0; i < 8; i++)
        {
            category.Items.Add(new RibbonGalleryItem { Content = new Border { Width = 40, Height = 20 } });
        }

        var inRibbon = new InRibbonGallery { MinColumnCount = 2, MaxColumnCount = 3, Items = { new RibbonGallery { Items = { category } } } };
        var group = new RibbonGroup { Header = "Styles", Items = { inRibbon } };
        var ribbon = new Ribbon { Items = { new RibbonTab { Header = "Home", Items = { group } } } };
        var window = RibbonSamples.Show(ribbon);

        Assert.Contains(":inribbon", category.Classes);
        var header = category.GetVisualDescendants().OfType<Border>().First(b => b.Name == "HeaderBorder");
        Assert.False(header.IsVisible);
        var panel = category.GetVisualDescendants().OfType<RibbonGalleryItemsPanel>().Single();
        var firstRowY = category.GetRealizedContainers().First().Bounds.Y;
        Assert.Equal(3, category.GetRealizedContainers().Count(c => c.Bounds.Y == firstRowY));
        Assert.True(panel.Bounds.Width > 0);
        window.Close();
    }

    [AvaloniaFact]
    public void Title_Stays_Clear_Of_The_Contextual_Group_Headers()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        ribbon.Title = "A rather long document title - WordPad";
        ribbon.ContextualTabGroups.Add(new RibbonContextualTabGroup { Header = "Picture Tools", Background = Brushes.Orange });
        ribbon.Items.Add(new RibbonTab { Header = "Format", ContextualTabGroupHeader = "Picture Tools", Items = { RibbonSamples.Group("Adjust", 1) } });
        var window = RibbonSamples.Show(ribbon, 420);
        var group = ribbon.ContextualTabGroups[0];
        var title = ribbon.GetVisualDescendants().OfType<ContentPresenter>().First(p => p.Name == "PART_TitleHost");
        Assert.True(group.Bounds.Width > 0);
        var groupRight = group.TranslatePoint(new Avalonia.Point(group.Bounds.Width, 0), window)!.Value.X;
        Assert.True(title.TranslatePoint(default, window)!.Value.X >= groupRight - 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void Control_Without_An_Image_Has_No_Image_Slot()
    {
        // WPF's DefaultControlSizeDefinition collapses the image of a control that has none: a font box sits at the
        // left edge of its group, with no empty 16 px slot before it.
        var box = new RibbonComboBox { Text = "Segoe UI" };
        var group = new RibbonGroup { Header = "Font", Items = { box } };
        var window = RibbonSamples.Show(new Ribbon { Items = { new RibbonTab { Header = "Home", Items = { group } } } });
        Assert.Contains(":noimage", box.Classes);
        Assert.Contains(":nolabel", box.Classes);
        var image = box.GetVisualDescendants().OfType<Image>().First(i => i.Name == "Image");
        Assert.False(image.IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Small_Image_And_Label_Set_After_The_Control_Joins_The_Group_Still_Show()
    {
        // XAML adds a control to the group before it sets the control's attributes.
        var group = new RibbonGroup { Header = "Editing" };
        var find = new RibbonButton();
        var selectAll = new RibbonButton();
        group.Items.Add(find);
        group.Items.Add(selectAll);
        _ = group.ActualGroupSizeDefinitions;
        find.SmallImageSource = RibbonSamples.Image();
        find.Label = "Find";
        selectAll.SmallImageSource = RibbonSamples.Image();
        selectAll.Label = "Select all";
        var window = RibbonSamples.Show(new Ribbon { Items = { new RibbonTab { Header = "Home", Items = { group } } } });
        foreach (var button in new[] { find, selectAll })
        {
            Assert.Contains(":small", button.Classes);
            Assert.DoesNotContain(":nolabel", button.Classes);
            Assert.True(button.Bounds.Width > 16);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void Control_Group_Default_Size_Is_The_Union_Of_Its_Items()
    {
        var bold = new RibbonToggleButton { SmallImageSource = RibbonSamples.Image() };
        var italic = new RibbonToggleButton { SmallImageSource = RibbonSamples.Image() };
        var controlGroup = new RibbonControlGroup { Items = { bold, italic } };
        var group = new RibbonGroup { Header = "Font", Items = { controlGroup } };
        var window = RibbonSamples.Show(new Ribbon { Items = { new RibbonTab { Header = "Home", Items = { group } } } });
        var size = RibbonHelper.EffectiveControlSizeDefinition(controlGroup);
        Assert.Equal(RibbonImageSize.Small, size.ImageSize);
        Assert.False(size.IsLabelVisible);
        Assert.Contains(":small", bold.Classes);
        window.Close();
    }
}
