using System.Collections.Generic;
using System.Linq;
using Avalonia.Headless.XUnit;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Group resizing: GroupSizeDefinitions, the reduction order, collapse to a drop-down.</summary>
public class GroupSizeReductionTests
{
    private static RibbonTab Home(Ribbon ribbon) => (RibbonTab)ribbon.Items[0]!;

    private static RibbonGroup GroupNamed(Ribbon ribbon, string name) => Home(ribbon).Groups.Single(g => g.Name == name);

    private static string State(Ribbon ribbon) => string.Join(" ", Home(ribbon).Groups.Select(g => $"{g.Name}={g.SizeDefinitionIndex}"));

    [AvaloniaFact]
    public void Default_Steps_Match_WPF()
    {
        var group = RibbonSamples.Group("Six", 6);
        var steps = group.ActualGroupSizeDefinitions.Select(d => d.ToString()).ToList();

        // WPF: L L L L L L -> L L L M M M -> M M M M M M -> M M M S S S -> S S S S S S -> Collapsed.
        Assert.Equal(
            [
                "Large+Label Large+Label Large+Label Large+Label Large+Label Large+Label",
                "Large+Label Large+Label Large+Label Small+Label Small+Label Small+Label",
                "Small+Label Small+Label Small+Label Small+Label Small+Label Small+Label",
                "Small+Label Small+Label Small+Label Small Small Small",
                "Small Small Small Small Small Small",
                "Collapsed",
            ],
            steps);

        Assert.Equal(["Large+Label Large+Label Large+Label", "Small+Label Small+Label Small+Label", "Collapsed"],
            RibbonSamples.Group("Three", 3).ActualGroupSizeDefinitions.Select(d => d.ToString()));
    }

    [AvaloniaFact]
    public void Groups_Shrink_In_Reduction_Order_Then_Right_To_Left_At_Stepped_Widths()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon("Paragraph,Font,Font");
        ribbon.CollapseWidth = 0;
        var window = RibbonSamples.Show(ribbon, 2000);
        Assert.Equal("Clipboard=0 Font=0 Paragraph=0", State(ribbon));
        var fullWidth = RibbonSamples.GroupsPanel(Home(ribbon)).DesiredSize.Width;

        // Step the width down and record the order in which the groups shrink.
        var order = new List<string>();
        var log = new List<string>();
        var previous = Home(ribbon).Groups.ToDictionary(g => g.Name!, g => g.SizeDefinitionIndex);
        for (var width = fullWidth + 2; width >= 150; width -= 8)
        {
            window.Width = width;
            RibbonSamples.Settle(window);
            log.Add($"{width}: {State(ribbon)} widths={string.Join(",", Home(ribbon).Groups.Select(g => g.DesiredSize.Width.ToString("F0")))} panel={RibbonSamples.GroupsPanel(Home(ribbon)).Bounds.Width:F0} ribbon={ribbon.Bounds.Width:F0}");
            foreach (var group in Home(ribbon).Groups)
            {
                for (var i = previous[group.Name!]; i < group.SizeDefinitionIndex; i++)
                {
                    order.Add(group.Name!);
                }

                Assert.True(group.SizeDefinitionIndex >= previous[group.Name!], $"{group.Name} grew while the width shrank: {State(ribbon)}\n{string.Join("\n", log)}");
                previous[group.Name!] = group.SizeDefinitionIndex;
            }

            if (Home(ribbon).Groups.All(g => g.IsCollapsed))
            {
                break;
            }

            Assert.True(Home(ribbon).Groups.Sum(g => g.DesiredSize.Width) <= width, $"groups overflow at {width}: {State(ribbon)}");
        }

        // The reduction order first (Paragraph, Font, Font), then right to left, cyclically.
        Assert.Equal(["Paragraph", "Font", "Font", "Paragraph", "Font", "Clipboard"], order.Take(6));
        Assert.All(Home(ribbon).Groups, g => Assert.True(g.IsCollapsed, $"{g.Name} is not collapsed at the narrowest width: {State(ribbon)}"));

        // Widening restores every group, in reverse.
        window.Width = fullWidth + 40;
        RibbonSamples.Settle(window);
        Assert.Equal("Clipboard=0 Font=0 Paragraph=0", State(ribbon));
        window.Close();
    }

    [AvaloniaFact]
    public void One_Pixel_Short_Shrinks_Exactly_The_First_Group_Of_The_Order()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon("Font");
        var window = RibbonSamples.Show(ribbon, 2000);
        var fullWidth = RibbonSamples.GroupsPanel(Home(ribbon)).DesiredSize.Width;

        // The groups area has a 1 px border on each side.
        window.Width = fullWidth + 1;
        RibbonSamples.Settle(window);
        Assert.Equal("Clipboard=0 Font=1 Paragraph=0", State(ribbon));
        window.Close();
    }

    [AvaloniaFact]
    public void Explicit_GroupSizeDefinitions_Are_Applied_To_The_Controls()
    {
        var group = RibbonSamples.Group("Custom", 2);
        group.GroupSizeDefinitions =
        [
            new RibbonGroupSizeDefinition { ControlSizeDefinitions = { new() { ImageSize = RibbonImageSize.Large }, new() { ImageSize = RibbonImageSize.Small, IsLabelVisible = false } } },
            new RibbonGroupSizeDefinition { IsCollapsed = true },
        ];
        var tab = new RibbonTab { Header = "Tab" };
        tab.Items.Add(group);
        var ribbon = new Ribbon();
        ribbon.Items.Add(tab);
        var window = RibbonSamples.Show(ribbon);

        var buttons = group.Items.OfType<RibbonButton>().ToList();
        Assert.Contains(":large", buttons[0].Classes);
        Assert.Equal(66, buttons[0].Bounds.Height);
        Assert.Contains(":small", buttons[1].Classes);
        Assert.Contains(":nolabel", buttons[1].Classes);
        Assert.Equal(RibbonImageSize.Small, buttons[1].ControlSizeDefinition!.ImageSize);
        Assert.False(buttons[1].ControlSizeDefinition!.IsLabelVisible);
        Assert.False(group.IsCollapsed);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Collapsed_Group_Opens_Its_Controls_In_A_Popup()
    {
        var clipboard = RibbonSamples.Group("Clipboard", 3);
        clipboard.GroupSizeDefinitions = [new RibbonGroupSizeDefinition { IsCollapsed = true }];
        var tab = new RibbonTab { Header = "Home" };
        tab.Items.Add(clipboard);
        var ribbon = new Ribbon();
        ribbon.Items.Add(tab);
        var window = RibbonSamples.Show(ribbon);
        Assert.True(clipboard.IsCollapsed);
        Assert.True(clipboard.Bounds.Width < 80, $"collapsed group is {clipboard.Bounds.Width} px wide");

        clipboard.IsDropDownOpen = true;
        RibbonSamples.Settle(window);
        var button = clipboard.Items.OfType<RibbonButton>().First();
        Assert.True(button.IsEffectivelyVisible);
        Assert.Contains(":large", button.Classes);

        // Clicking a control in the popup closes it, as WPF's DismissPopup does.
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        button.RaiseEvent(new RibbonDismissPopupEventArgs());
        Assert.False(clipboard.IsDropDownOpen);
        window.Close();
    }
}
