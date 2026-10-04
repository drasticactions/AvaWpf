using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Ribbon minimize: double-click the selected tab, Ctrl+F1, minimized tabs open as a popup.</summary>
public class MinimizeTests
{
    private static Control GroupsBorder(Ribbon ribbon) => ribbon.GroupsBorder!;

    [AvaloniaFact]
    public void Double_Click_On_The_Selected_Tab_Toggles_Minimized()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);
        var home = ribbon.TabHeaders[0];
        var groupsHeight = ribbon.Bounds.Height;

        ribbon.NotifyMouseClickedOnTabHeader(home, 1);
        ribbon.NotifyMouseClickedOnTabHeader(home, 2);
        RibbonSamples.Settle(window);
        Assert.True(ribbon.IsMinimized);
        Assert.False(ribbon.IsDropDownOpen);
        Assert.True(ribbon.Bounds.Height < groupsHeight - 60, $"minimized Ribbon is {ribbon.Bounds.Height} px high (was {groupsHeight})");

        ribbon.NotifyMouseClickedOnTabHeader(home, 1);
        ribbon.NotifyMouseClickedOnTabHeader(home, 2);
        RibbonSamples.Settle(window);
        Assert.False(ribbon.IsMinimized);
        Assert.Equal(groupsHeight, ribbon.Bounds.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void Double_Click_On_Another_Tab_Only_Selects_It()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);
        var insert = ribbon.TabHeaders[1];

        ribbon.NotifyMouseClickedOnTabHeader(insert, 1);
        ribbon.NotifyMouseClickedOnTabHeader(insert, 2);
        Assert.Equal(1, ribbon.SelectedIndex);
        Assert.False(ribbon.IsMinimized);
        window.Close();
    }

    [AvaloniaFact]
    public void Ctrl_F1_Toggles_Minimized()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);

        window.KeyPress(Key.F1, RawInputModifiers.Control, PhysicalKey.F1, null);
        window.KeyRelease(Key.F1, RawInputModifiers.Control, PhysicalKey.F1, null);
        Assert.True(ribbon.IsMinimized);

        window.KeyPress(Key.F1, RawInputModifiers.Control, PhysicalKey.F1, null);
        window.KeyRelease(Key.F1, RawInputModifiers.Control, PhysicalKey.F1, null);
        Assert.False(ribbon.IsMinimized);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Minimized_Tab_Opens_Its_Groups_In_A_Popup()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        ribbon.IsMinimized = true;
        var window = RibbonSamples.Show(ribbon);
        var groups = GroupsBorder(ribbon);
        Assert.Null(TopLevel.GetTopLevel(groups));

        // A click on another tab selects it and opens the popup; a click on the open tab closes it.
        ribbon.NotifyMouseClickedOnTabHeader(ribbon.TabHeaders[1], 1);
        RibbonSamples.Settle(window);
        Assert.Equal(1, ribbon.SelectedIndex);
        Assert.True(ribbon.IsDropDownOpen);
        Assert.True(groups.IsEffectivelyVisible);
        Assert.Contains(groups.GetVisualAncestors(), a => a is Avalonia.Controls.Primitives.PopupRoot or Avalonia.Controls.Primitives.OverlayPopupHost);
        Assert.True(((RibbonTab)ribbon.Items[1]!).Groups.First().IsEffectivelyVisible);

        ribbon.NotifyMouseClickedOnTabHeader(ribbon.TabHeaders[1], 1);
        RibbonSamples.Settle(window);
        Assert.False(ribbon.IsDropDownOpen);

        // Restoring puts the groups back under the tabs.
        ribbon.IsMinimized = false;
        RibbonSamples.Settle(window);
        Assert.DoesNotContain(groups.GetVisualAncestors(), a => a is Avalonia.Controls.Primitives.PopupRoot or Avalonia.Controls.Primitives.OverlayPopupHost);
        Assert.Same(window, TopLevel.GetTopLevel(groups));
        Assert.True(groups.IsEffectivelyVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Collapsed_And_Expanded_Events_Follow_IsMinimized()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);
        var log = new System.Collections.Generic.List<string>();
        ribbon.Collapsed += (_, _) => log.Add("collapsed");
        ribbon.Expanded += (_, _) => log.Add("expanded");
        ribbon.IsMinimized = true;
        ribbon.IsMinimized = false;
        Assert.Equal(["collapsed", "expanded"], log);
        window.Close();
    }
}
