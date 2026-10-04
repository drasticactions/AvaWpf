using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaWpf.Ribbon.Primitives;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Small Ribbon controls: image and label placement, and the title-only tool tip.</summary>
public class RibbonLabelTests
{
    private static Window ShowInGroup(params Control[] controls)
    {
        var group = new RibbonGroup { Header = "G" };
        foreach (var c in controls)
        {
            group.Items.Add(c);
        }

        return RibbonSamples.Show(new Ribbon { Items = { new RibbonTab { Header = "Home", Items = { group } } } });
    }

    [AvaloniaFact]
    public void A_Button_Without_A_Label_Centres_Its_Image()
    {
        // WPF collapses the label of a control with no Label; an empty one would still take its margins.
        var bold = new RibbonToggleButton { SmallImageSource = RibbonSamples.Image(16) };
        var left = new RibbonRadioButton { GroupName = "a", SmallImageSource = RibbonSamples.Image(16) };
        var window = ShowInGroup(new RibbonControlGroup { Items = { bold } }, new RibbonControlGroup { Items = { left } });

        foreach (var button in new Control[] { bold, left })
        {
            Assert.False(button.GetVisualDescendants().OfType<RibbonTwoLineText>().Single().IsVisible);
            var image = button.GetVisualDescendants().OfType<Image>().Single();
            var center = image.TranslatePoint(new Point(8, 8), button)!.Value.X;
            Assert.InRange(center - (button.Bounds.Width / 2), -1, 1);
        }

        window.Close();
    }

    [AvaloniaFact]
    public void A_Small_Label_Sits_On_The_Image_Centre_As_In_WPF()
    {
        // With the 13 px line WPF puts the baseline at 13 x ascent / line spacing; Avalonia keeps the full ascent.
        // The label's cap height is centred on the 16 px image.
        var bullets = new RibbonToggleButton { Label = "Bullets", SmallImageSource = RibbonSamples.Image(16) };
        RibbonControlService.SetControlSizeDefinition(bullets, new RibbonControlSizeDefinition { ImageSize = RibbonImageSize.Small, IsLabelVisible = true });
        var window = ShowInGroup(bullets);

        var image = bullets.GetVisualDescendants().OfType<Image>().Single();
        var text = bullets.GetVisualDescendants().OfType<TextBlock>().Single(t => t.IsVisible && !string.IsNullOrEmpty(t.Text));
        var line = text.TextLayout.TextLines[0];
        var baseline = text.TranslatePoint(new Point(0, line.Baseline), bullets)!.Value.Y;
        var capHeight = text.FontSize * 0.7;
        var imageCenter = image.TranslatePoint(new Point(0, 8), bullets)!.Value.Y;
        Assert.InRange(baseline - (capHeight / 2) - imageCenter, -1, 1);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Title_Only_Tool_Tip_Has_No_Empty_Description_Line()
    {
        var tip = new RibbonToolTip { Title = "Replace" };
        var window = RibbonSamples.Show(tip, 400, 200);

        var description = tip.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "Description");
        Assert.False(description.IsVisible);
        var title = tip.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "Title");
        // The tip is the title line plus padding, borders and the shadow margin, not a description line more.
        Assert.True(tip.DesiredSize.Height < title.Bounds.Height + 20, $"tip {tip.DesiredSize.Height}, title {title.Bounds.Height}");
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_Highlighted_Menu_Item_Fills_Its_Side_Bar(bool split)
    {
        // WPF clears the side bar (image column) of a highlighted item, so the highlight covers the whole row.
        MenuItem item = split ? new RibbonSplitMenuItem { Header = "Paste" } : new RibbonMenuItem { Header = "Paste" };
        var window = RibbonSamples.Show(new StackPanel { Children = { item } }, 300, 200);
        var sideBar = item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_SideBarBorder");
        Assert.NotEqual(Avalonia.Media.Brushes.Transparent, sideBar.Background);

        item.IsSelected = true;

        Assert.Equal(Avalonia.Media.Colors.Transparent, Assert.IsAssignableFrom<Avalonia.Media.ISolidColorBrush>(sideBar.Background).Color);
        window.Close();
    }

    [AvaloniaFact]
    public void The_Tab_Header_Border_Is_Not_Clipped_At_The_Top()
    {
        // The border sits 1 px above the header (WPF's -1 margin), so the header must not clip it.
        var ribbon = new Ribbon { Items = { new RibbonTab { Header = "Home", Items = { RibbonSamples.Group("G", 1) } } } };
        var window = RibbonSamples.Show(ribbon);
        var header = ribbon.GetVisualDescendants().OfType<RibbonTabHeader>().First();
        var border = header.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_OuterBorder");

        Assert.True(border.TranslatePoint(default, header)!.Value.Y < 0);
        Assert.False(header.ClipToBounds);
        window.Close();
    }
}
