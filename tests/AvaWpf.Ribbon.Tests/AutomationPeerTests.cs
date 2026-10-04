using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaWpf.Ribbon.Automation.Peers;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Automation peers of the Ribbon controls.</summary>
public class AutomationPeerTests
{
    private static AutomationPeer Peer(Control control) => ControlAutomationPeer.CreatePeerForElement(control);

    private static RibbonTab Home(Ribbon ribbon) => (RibbonTab)ribbon.Items[0]!;

    [AvaloniaFact]
    public void Ribbon_Is_A_Tab_Control_With_The_QAT_And_Tabs_As_Children()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);

        var peer = Peer(ribbon);
        var children = peer.GetChildren();

        Assert.IsType<RibbonAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.Tab, peer.GetAutomationControlType());
        Assert.Equal(AutomationControlType.ToolBar, children[0].GetAutomationControlType());
        Assert.Equal("Quick Access Toolbar", children[0].GetName());
        Assert.Equal(["Home", "Insert"], children.Skip(1).Select(c => c.GetName()));
        Assert.All(children.Skip(1), c => Assert.Equal(AutomationControlType.TabItem, c.GetAutomationControlType()));
        Assert.Same(Peer(Home(ribbon)), Assert.Single(peer.GetProvider<ISelectionProvider>()!.GetSelection()));
        window.Close();
    }

    [AvaloniaFact]
    public void Ribbon_Minimizes_Through_Expand_Collapse()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);
        var expand = Peer(ribbon).GetProvider<IExpandCollapseProvider>()!;

        Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        expand.Collapse();
        Assert.True(ribbon.IsMinimized);
        Assert.Equal(ExpandCollapseState.Collapsed, expand.ExpandCollapseState);
        expand.Expand();
        Assert.False(ribbon.IsMinimized);
        window.Close();
    }

    [AvaloniaFact]
    public void RibbonTab_Is_A_Selectable_TabItem_With_Its_Groups_And_KeyTip()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);
        var insert = (RibbonTab)ribbon.Items[1]!;

        var home = Peer(Home(ribbon));
        var insertPeer = Peer(insert);

        Assert.IsType<RibbonTabAutomationPeer>(home);
        Assert.Equal(AutomationControlType.TabItem, home.GetAutomationControlType());
        Assert.Equal("H", home.GetAccessKey());
        Assert.Equal(["Clipboard", "Font", "Paragraph"], home.GetChildren().Select(c => c.GetName()));
        Assert.True(home.GetProvider<ISelectionItemProvider>()!.IsSelected);

        insertPeer.GetProvider<ISelectionItemProvider>()!.Select();
        RibbonSamples.Settle(window);
        Assert.Same(insert, ribbon.SelectedItem);
        window.Close();
    }

    [AvaloniaFact]
    public void RibbonGroup_Is_A_Group_That_Expands_When_Collapsed()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        ribbon.CollapseWidth = 0;
        var window = RibbonSamples.Show(ribbon);
        var font = Home(ribbon).Groups.Single(g => g.Name == "Font");

        var peer = Peer(font);

        Assert.IsType<RibbonGroupAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Equal("Font", peer.GetName());
        Assert.Equal(Enumerable.Range(1, 6).Select(i => $"Font {i}"), peer.GetChildren().Select(c => c.GetName()));
        Assert.Equal(ExpandCollapseState.LeafNode, peer.GetProvider<IExpandCollapseProvider>()!.ExpandCollapseState);

        window.Width = 150;
        RibbonSamples.Settle(window);
        Assert.True(font.IsCollapsed);
        Assert.Equal(ExpandCollapseState.Collapsed, peer.GetProvider<IExpandCollapseProvider>()!.ExpandCollapseState);
        peer.GetProvider<IExpandCollapseProvider>()!.Expand();
        Assert.True(font.IsDropDownOpen);
        Assert.Equal(6, peer.GetChildren().Count);
        window.Close();
    }

    [AvaloniaFact]
    public void RibbonButton_Is_A_Button_Named_By_Its_Label_With_Its_KeyTip()
    {
        var button = RibbonSamples.Button("Paste", "V");
        button.ToolTipDescription = "Paste the contents of the Clipboard.";
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        var window = RibbonSamples.Show(new StackPanel { Children = { button } });

        var peer = Peer(button);

        Assert.IsType<RibbonButtonAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("Paste", peer.GetName());
        Assert.Equal("V", peer.GetAccessKey());
        Assert.Equal("Paste the contents of the Clipboard.", peer.GetHelpText());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, clicks);
        window.Close();
    }

    [AvaloniaFact]
    public void RibbonToggleButton_Toggles()
    {
        var toggle = new RibbonToggleButton { Label = "Bold", KeyTip = "B", SmallImageSource = RibbonSamples.Image(16) };
        var window = RibbonSamples.Show(new StackPanel { Children = { toggle } });

        var peer = Peer(toggle);
        var toggleProvider = peer.GetProvider<IToggleProvider>()!;

        Assert.IsType<RibbonToggleButtonAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("Bold", peer.GetName());
        Assert.Equal("B", peer.GetAccessKey());
        toggleProvider.Toggle();
        Assert.True(toggle.IsChecked);
        Assert.Equal(ToggleState.On, toggleProvider.ToggleState);
        window.Close();
    }

    [AvaloniaFact]
    public void RibbonMenuButton_Is_A_MenuItem_That_Opens_Its_Menu()
    {
        var menu = new RibbonMenuButton
        {
            Label = "Insert",
            KeyTip = "I",
            Items = { new RibbonMenuItem { Header = "Picture", KeyTip = "P" }, new RibbonMenuItem { Header = "Chart" } },
        };
        var window = RibbonSamples.Show(new StackPanel { Children = { menu } });

        var peer = Peer(menu);
        var expand = peer.GetProvider<IExpandCollapseProvider>()!;

        Assert.IsType<RibbonMenuButtonAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.MenuItem, peer.GetAutomationControlType());
        Assert.Equal("Insert", peer.GetName());
        Assert.Equal("I", peer.GetAccessKey());
        Assert.Equal(ExpandCollapseState.Collapsed, expand.ExpandCollapseState);
        expand.Expand();
        RibbonSamples.Settle(window);
        Assert.True(menu.IsDropDownOpen);
        var items = peer.GetChildren();
        Assert.Equal(["Picture", "Chart"], items.Select(c => c.GetName()));
        Assert.IsType<RibbonMenuItemAutomationPeer>(items[0]);
        Assert.Equal("P", items[0].GetAccessKey());
        expand.Collapse();
        Assert.False(menu.IsDropDownOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void RibbonSplitButton_Is_A_SplitButton_That_Invokes_And_Opens()
    {
        var split = new RibbonSplitButton { Label = "Paste", KeyTip = "V", LargeImageSource = RibbonSamples.Image(), Items = { new RibbonMenuItem { Header = "Paste Special" } } };
        var clicks = 0;
        split.Click += (_, _) => clicks++;
        var window = RibbonSamples.Show(new StackPanel { Children = { split } });

        var peer = Peer(split);

        Assert.IsType<RibbonSplitButtonAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.SplitButton, peer.GetAutomationControlType());
        Assert.Equal("Paste", peer.GetName());
        Assert.Equal("V", peer.GetAccessKey());
        Assert.Null(peer.GetProvider<IToggleProvider>());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, clicks);
        peer.GetProvider<IExpandCollapseProvider>()!.Expand();
        Assert.True(split.IsDropDownOpen);
        Assert.Equal(["Paste Special"], peer.GetChildren().Select(c => c.GetName()));

        split.IsDropDownOpen = false;
        split.IsCheckable = true;
        peer.GetProvider<IToggleProvider>()!.Toggle();
        Assert.True(split.IsChecked);
        window.Close();
    }

    [AvaloniaFact]
    public void RibbonGallery_Is_A_List_Of_Selectable_ListItems()
    {
        var a = new RibbonGalleryItem { Content = "Normal", KeyTip = "N" };
        var b = new RibbonGalleryItem { Content = "Heading 1" };
        var c = new RibbonGalleryItem { Content = "Title" };
        var gallery = new RibbonGallery
        {
            Items =
            {
                new RibbonGalleryCategory { Header = "Body", Items = { a, b } },
                new RibbonGalleryCategory { Header = "Headings", Items = { c } },
            },
        };
        var window = RibbonSamples.Show(new StackPanel { Children = { gallery } });

        var peer = Peer(gallery);
        var items = peer.GetChildren();

        Assert.IsType<RibbonGalleryAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.List, peer.GetAutomationControlType());
        Assert.Equal(["Normal", "Heading 1", "Title"], items.Select(i => i.GetName()));
        Assert.All(items, i => Assert.Equal(AutomationControlType.ListItem, i.GetAutomationControlType()));
        Assert.IsType<RibbonGalleryItemAutomationPeer>(items[0]);
        Assert.Equal("N", items[0].GetAccessKey());
        Assert.Empty(peer.GetProvider<ISelectionProvider>()!.GetSelection());

        items[1].GetProvider<ISelectionItemProvider>()!.Select();
        Assert.True(b.IsSelected);
        Assert.Same(items[1], Assert.Single(peer.GetProvider<ISelectionProvider>()!.GetSelection()));
        window.Close();
    }

    [AvaloniaFact]
    public void QuickAccessToolBar_Is_A_ToolBar_Of_Its_Controls()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        var window = RibbonSamples.Show(ribbon);
        var paste = (RibbonButton)Home(ribbon).Groups.First().Items[0]!;
        Assert.True(ribbon.AddToQuickAccessToolBar(paste));
        RibbonSamples.Settle(window);

        var peer = Peer(ribbon.QuickAccessToolBar!);

        Assert.IsType<RibbonQuickAccessToolBarAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.ToolBar, peer.GetAutomationControlType());
        var child = Assert.Single(peer.GetChildren());
        Assert.Equal(paste.Label, child.GetName());
        Assert.Equal(AutomationControlType.Button, child.GetAutomationControlType());
        Assert.Equal(ExpandCollapseState.LeafNode, peer.GetProvider<IExpandCollapseProvider>()!.ExpandCollapseState);
        window.Close();
    }

    [AvaloniaFact]
    public void Other_Ribbon_Controls_Expose_Label_And_KeyTip()
    {
        var check = new RibbonCheckBox { Label = "Ruler", KeyTip = "R" };
        var radio = new RibbonRadioButton { Label = "Portrait", KeyTip = "O" };
        var text = new RibbonTextBox { Label = "Find", KeyTip = "F" };
        var combo = new RibbonComboBox { Label = "Font", KeyTip = "FF", Items = { new RibbonGallery() } };
        var window = RibbonSamples.Show(new StackPanel { Children = { check, radio, text, combo } });

        Assert.Equal((AutomationControlType.CheckBox, "Ruler", "R"), (Peer(check).GetAutomationControlType(), Peer(check).GetName(), Peer(check).GetAccessKey()));
        Assert.Equal((AutomationControlType.RadioButton, "Portrait", "O"), (Peer(radio).GetAutomationControlType(), Peer(radio).GetName(), Peer(radio).GetAccessKey()));
        Assert.Equal((AutomationControlType.Edit, "Find", "F"), (Peer(text).GetAutomationControlType(), Peer(text).GetName(), Peer(text).GetAccessKey()));
        Assert.Equal((AutomationControlType.ComboBox, "Font", "FF"), (Peer(combo).GetAutomationControlType(), Peer(combo).GetName(), Peer(combo).GetAccessKey()));
        window.Close();
    }
}
