using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using AvaWpf.Controls.Automation.Peers;
using Xunit;

namespace AvaWpf.Controls.Tests;

/// <summary>Automation peers of the AvaWpf.Controls controls.</summary>
public class AutomationPeerTests
{
    private static AutomationPeer Peer(Control control) => ControlAutomationPeer.CreatePeerForElement(control);

    private static List<string> Names(AutomationPeer peer) => peer.GetChildren().Select(c => c.GetName()).ToList();

    [AvaloniaFact]
    public void ToolBar_Is_A_ToolBar_Named_By_Its_Header_With_Its_Items_As_Children()
    {
        var toolBar = new ToolBar
        {
            Header = "Standard",
            Items = { new Button { Content = "New" }, new Button { Content = "Open" }, new Separator(), new CheckBox { Content = "Bold" } },
        };
        var window = TestHelpers.Show(new StackPanel { Children = { toolBar } });

        var peer = Peer(toolBar);

        Assert.IsType<ToolBarAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.ToolBar, peer.GetAutomationControlType());
        Assert.Equal("Standard", peer.GetName());
        Assert.Equal(["New", "Open", string.Empty, "Bold"], Names(peer));
        Assert.Equal(AutomationControlType.Button, peer.GetChildren()[0].GetAutomationControlType());
        Assert.Equal(ExpandCollapseState.LeafNode, peer.GetProvider<IExpandCollapseProvider>()!.ExpandCollapseState);
        window.Close();
    }

    [AvaloniaFact]
    public void ToolBar_Overflow_Items_Stay_Children_And_The_Overflow_Expands()
    {
        var toolBar = new ToolBar { Header = "Narrow", Width = 90 };
        for (var i = 0; i < 8; i++)
        {
            toolBar.Items.Add(new Button { Content = "B" + i });
        }

        var window = TestHelpers.Show(new StackPanel { Children = { toolBar } });
        var peer = Peer(toolBar);
        var expand = peer.GetProvider<IExpandCollapseProvider>()!;

        Assert.True(toolBar.HasOverflowItems);
        Assert.Equal(8, peer.GetChildren().Count);
        Assert.Equal(ExpandCollapseState.Collapsed, expand.ExpandCollapseState);
        expand.Expand();
        Assert.True(toolBar.IsOverflowOpen);
        Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        expand.Collapse();
        Assert.False(toolBar.IsOverflowOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void ToolBarTray_Is_A_Pane_Holding_Its_ToolBars()
    {
        var tray = new ToolBarTray();
        tray.ToolBars.Add(new ToolBar { Header = "One", Items = { new Button { Content = "A" } } });
        tray.ToolBars.Add(new ToolBar { Header = "Two", Band = 1, Items = { new Button { Content = "B" } } });
        var window = TestHelpers.Show(new StackPanel { Children = { tray } });

        var peer = Peer(tray);

        Assert.IsType<ToolBarTrayAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
        Assert.False(peer.IsContentElement());
        Assert.Equal(["One", "Two"], Names(peer));
        Assert.All(peer.GetChildren(), c => Assert.Equal(AutomationControlType.ToolBar, c.GetAutomationControlType()));
        window.Close();
    }

    [AvaloniaFact]
    public void StatusBar_Flattens_Items_As_WPF_Does()
    {
        var progress = new ProgressBar { Value = 40, Width = 100 };
        var statusBar = new StatusBar
        {
            Items = { "Ready", new Separator(), new StatusBarItem { Content = "Ln 1" }, new StatusBarItem { Content = progress } },
        };
        var window = TestHelpers.Show(new DockPanel { Children = { statusBar } });

        var peer = Peer(statusBar);
        var children = peer.GetChildren();

        Assert.IsType<StatusBarAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.StatusBar, peer.GetAutomationControlType());
        Assert.Equal(4, children.Count);
        Assert.Equal(AutomationControlType.Text, children[0].GetAutomationControlType());
        Assert.Equal("Ready", children[0].GetName());
        Assert.IsType<StatusBarItemAutomationPeer>(children[2]);
        Assert.Equal("Ln 1", children[2].GetName());
        Assert.Same(Peer(progress), children[3]);
        Assert.Equal(AutomationControlType.ProgressBar, children[3].GetAutomationControlType());
        window.Close();
    }

    [AvaloniaFact]
    public void StatusBarItem_Is_Text()
    {
        var item = new StatusBarItem { Content = "Saved" };
        var window = TestHelpers.Show(new StatusBar { Items = { item } });

        var peer = Peer(item);

        Assert.Equal(AutomationControlType.Text, peer.GetAutomationControlType());
        Assert.Equal("Saved", peer.GetName());
        Assert.True(peer.IsContentElement());
        Assert.True(peer.IsControlElement());
        window.Close();
    }

    [AvaloniaFact]
    public void ResizeGrip_Is_A_Thumb_That_Is_Not_Content()
    {
        var grip = new ResizeGrip { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        var window = TestHelpers.Show(new Panel { Children = { grip } });

        var peer = Peer(grip);

        Assert.IsType<ResizeGripAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.Thumb, peer.GetAutomationControlType());
        Assert.False(peer.IsContentElement());
        Assert.True(peer.IsControlElement());
        window.Close();
    }

    [AvaloniaFact]
    public void ListView_With_GridView_Is_A_DataGrid_With_A_Header_Row_And_Data_Items()
    {
        var list = new ListView
        {
            View = new GridView
            {
                Columns =
                {
                    new GridViewColumn { Header = "Name", Width = 100, DisplayMemberBinding = new ReflectionBinding(nameof(GridViewTests.Person.Name)) },
                    new GridViewColumn { Header = "City", Width = 120, DisplayMemberBinding = new ReflectionBinding(nameof(GridViewTests.Person.City)) },
                },
            },
            ItemsSource = new List<GridViewTests.Person> { new("Ann", "Oslo"), new("Bob", "Rio") },
            Width = 400,
            Height = 200,
        };
        var window = TestHelpers.Show(list);

        var peer = Peer(list);
        var children = peer.GetChildren();

        Assert.IsType<ListViewAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.DataGrid, peer.GetAutomationControlType());
        Assert.NotNull(peer.GetProvider<ISelectionProvider>());
        Assert.Equal(3, children.Count);

        var header = children[0];
        Assert.Equal(AutomationControlType.Header, header.GetAutomationControlType());
        Assert.False(header.IsContentElement());
        Assert.Equal(["Name", "City"], Names(header));
        Assert.All(header.GetChildren(), h => Assert.Equal(AutomationControlType.HeaderItem, h.GetAutomationControlType()));

        var row = children[1];
        Assert.Equal(AutomationControlType.DataItem, row.GetAutomationControlType());
        Assert.Equal("Ann", row.GetName());
        Assert.Equal(["Ann", "Oslo"], Names(row));
        Assert.NotNull(row.GetProvider<ISelectionItemProvider>());
        row.GetProvider<ISelectionItemProvider>()!.Select();
        Assert.Equal(0, list.SelectedIndex);
        window.Close();
    }

    [AvaloniaFact]
    public void ListView_Without_A_View_Is_A_List_Of_List_Items()
    {
        var list = new ListView { ItemsSource = new[] { "One", "Two" }, Height = 100 };
        var window = TestHelpers.Show(list);

        var peer = Peer(list);
        var item = Peer(list.ContainerFromIndex(0)!);

        Assert.Equal(AutomationControlType.List, peer.GetAutomationControlType());
        Assert.IsType<ListViewItemAutomationPeer>(item);
        Assert.Equal(AutomationControlType.ListItem, item.GetAutomationControlType());
        Assert.Equal("One", item.GetName());
        window.Close();
    }

    [AvaloniaFact]
    public void GridViewColumnHeader_Is_An_Invokable_HeaderItem()
    {
        var list = new ListView
        {
            View = new GridView { Columns = { new GridViewColumn { Header = "Name", Width = 100 } } },
            ItemsSource = new[] { "Ann" },
            Height = 100,
        };
        var window = TestHelpers.Show(list);
        var header = TestHelpers.All<GridViewColumnHeader>(list).First(h => h.Role == GridViewColumnHeaderRole.Normal);
        var clicks = 0;
        header.Click += (_, _) => clicks++;

        var peer = Peer(header);

        Assert.IsType<GridViewColumnHeaderAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.HeaderItem, peer.GetAutomationControlType());
        Assert.Equal("Name", peer.GetName());
        Assert.False(peer.IsContentElement());
        Assert.DoesNotContain(peer.GetChildren(), c => c.GetAutomationControlType() == AutomationControlType.Button);
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, clicks);
        window.Close();
    }
}
