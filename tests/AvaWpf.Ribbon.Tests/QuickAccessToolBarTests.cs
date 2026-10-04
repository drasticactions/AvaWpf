using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Quick Access Toolbar: add by cloning, the context menu, overflow.</summary>
public class QuickAccessToolBarTests
{
    [AvaloniaFact]
    public void Add_Clones_The_Control_Once()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);
        var paste = ((RibbonTab)ribbon.Items[0]!).Groups.First().Items.OfType<RibbonButton>().First();
        paste.ToolTipTitle = "Paste";

        Assert.True(ribbon.AddToQuickAccessToolBar(paste));
        var clone = Assert.IsType<RibbonButton>(Assert.Single(ribbon.QuickAccessToolBar!.Items));
        Assert.NotSame(paste, clone);
        Assert.Equal(paste.Label, clone.Label);
        Assert.Same(paste.SmallImageSource, clone.SmallImageSource);
        Assert.Equal("Paste", clone.ToolTipTitle);
        Assert.True(clone.IsInQuickAccessToolBar);

        // The same id is not added twice.
        Assert.False(ribbon.AddToQuickAccessToolBar(paste));
        Assert.Single(ribbon.QuickAccessToolBar.Items);

        // In the toolbar the copy shows a small image without a label.
        RibbonSamples.Settle(window);
        Assert.Contains(":small", clone.Classes);
        Assert.Contains(":nolabel", clone.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Clone_Event_Can_Supply_The_Copy()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);
        var paste = ((RibbonTab)ribbon.Items[0]!).Groups.First().Items.OfType<RibbonButton>().First();
        var custom = new RibbonButton { Label = "Custom" };
        paste.AddHandler(RibbonQuickAccessToolBar.CloneEvent, (_, e) => e.CloneInstance = custom);

        Assert.True(ribbon.AddToQuickAccessToolBar(paste));
        Assert.Same(custom, Assert.Single(ribbon.QuickAccessToolBar!.Items));
        window.Close();
    }

    [AvaloniaFact]
    public void Toggle_Copies_Stay_In_Sync()
    {
        var toggle = new RibbonToggleButton { Label = "Bold", QuickAccessToolBarId = "Bold", SmallImageSource = RibbonSamples.Image(16) };
        var group = new RibbonGroup { Header = "Font" };
        group.Items.Add(toggle);
        var tab = new RibbonTab { Header = "Home" };
        tab.Items.Add(group);
        var ribbon = new Ribbon { QuickAccessToolBar = new RibbonQuickAccessToolBar() };
        ribbon.Items.Add(tab);
        var window = RibbonSamples.Show(ribbon);

        Assert.True(ribbon.AddToQuickAccessToolBar(toggle));
        var clone = Assert.IsType<RibbonToggleButton>(Assert.Single(ribbon.QuickAccessToolBar!.Items));
        clone.IsChecked = true;
        Assert.True(toggle.IsChecked);
        toggle.IsChecked = false;
        Assert.False(clone.IsChecked);
        window.Close();
    }

    [AvaloniaFact]
    public void Right_Click_Offers_Add_To_Quick_Access_Toolbar()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);
        var paste = ((RibbonTab)ribbon.Items[0]!).Groups.First().Items.OfType<RibbonButton>().First();

        paste.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = Control.ContextRequestedEvent, Source = paste });
        RibbonSamples.Settle(window);
        var items = ribbon.ContextMenuForTests.ItemsSource!.Cast<object>().OfType<RibbonMenuItem>().ToList();
        var add = items.First(i => (string?)i.Header == "_Add to Quick Access Toolbar");
        Assert.True(add.IsEnabled);
        Assert.Contains(items, i => (string?)i.Header == "Mi_nimize the Ribbon");
        add.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        ribbon.ContextMenuForTests.Close();
        Assert.Single(ribbon.QuickAccessToolBar!.Items);

        // On the toolbar item, the menu offers removal instead.
        var clone = (Control)ribbon.QuickAccessToolBar.Items[0]!;
        RibbonSamples.Settle(window);
        clone.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = Control.ContextRequestedEvent, Source = clone });
        var remove = ribbon.ContextMenuForTests.ItemsSource!.Cast<object>().OfType<RibbonMenuItem>().First();
        Assert.Equal("_Remove from Quick Access Toolbar", remove.Header);
        remove.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        ribbon.ContextMenuForTests.Close();
        Assert.Empty(ribbon.QuickAccessToolBar.Items);
        window.Close();
    }

    [AvaloniaFact]
    public void Items_That_Do_Not_Fit_Overflow_Into_The_Popup()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var qat = ribbon.QuickAccessToolBar!;
        var window = RibbonSamples.Show(ribbon);
        var buttons = ((RibbonTab)ribbon.Items[0]!).Groups.SelectMany(g => g.Items.OfType<RibbonButton>()).Take(10).ToList();
        foreach (var button in buttons)
        {
            Assert.True(ribbon.AddToQuickAccessToolBar(button));
        }

        RibbonSamples.Settle(window);
        Assert.False(qat.HasOverflowItems);
        var itemWidth = ((Control)qat.Items[0]!).Bounds.Width;
        Assert.True(itemWidth > 0);

        // Room for about four items: the rest move to the overflow panel.
        qat.MaxWidth = (itemWidth * 4) + 16;
        RibbonSamples.Settle(window);
        Assert.True(qat.HasOverflowItems);
        var overflow = qat.OverflowPanel!;
        var inOverflow = overflow.HostedItems.Count;
        Assert.InRange(inOverflow, 6, 8);
        Assert.All(overflow.HostedItems, i => Assert.True(RibbonQuickAccessToolBar.GetIsOverflowItem(i)));
        Assert.Equal(qat.Items.Cast<Control>().Skip(10 - inOverflow), overflow.HostedItems);

        // The overflow popup shows them.
        qat.IsOverflowOpen = true;
        RibbonSamples.Settle(window);
        Assert.All(overflow.HostedItems, i => Assert.True(i.IsEffectivelyVisible && i.Bounds.Width > 0));
        qat.IsOverflowOpen = false;

        // With room again, every item is back on the toolbar.
        qat.MaxWidth = double.PositiveInfinity;
        RibbonSamples.Settle(window);
        Assert.False(qat.HasOverflowItems);
        Assert.Empty(overflow.HostedItems);
        Assert.All(qat.Items.Cast<Control>(), i => Assert.False(RibbonQuickAccessToolBar.GetIsOverflowItem(i)));
        window.Close();
    }
}
