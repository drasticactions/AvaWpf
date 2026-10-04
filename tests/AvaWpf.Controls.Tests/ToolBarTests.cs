using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Controls.Tests;

/// <summary>ToolBar overflow at each width.</summary>
public class ToolBarTests
{
    private const double ItemWidth = 30;

    private static (ToolBar Bar, List<Button> Items) CreateBar(int count = 8)
    {
        var items = Enumerable.Range(0, count).Select(i => new Button { Content = i.ToString(), Width = ItemWidth, Height = 22 }).ToList();
        var bar = new ToolBar { ItemsSource = items, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        return (bar, items);
    }

    [AvaloniaTheory]
    [InlineData(400)]
    [InlineData(200)]
    [InlineData(150)]
    [InlineData(100)]
    [InlineData(60)]
    public void Items_That_Do_Not_Fit_Overflow(double width)
    {
        var (bar, items) = CreateBar();
        bar.Width = width;
        var window = TestHelpers.Show(bar);

        var panel = bar.ToolBarPanel!;
        var main = items.Where(i => !ToolBar.GetIsOverflowItem(i)).ToList();
        var overflow = items.Where(ToolBar.GetIsOverflowItem).ToList();

        // The items stay in order: the main bar holds a prefix, the overflow the rest.
        Assert.Equal(items.Take(main.Count), main);
        Assert.True(main.Count * ItemWidth <= panel.Bounds.Width + 0.01, $"{main.Count} items do not fit {panel.Bounds.Width}");
        if (overflow.Count > 0)
        {
            Assert.True((main.Count + 1) * ItemWidth > panel.Bounds.Width, "an overflowed item would have fit");
        }

        Assert.Equal(overflow.Count > 0, bar.HasOverflowItems);
        Assert.Equal(main, panel.GetVisualChildren().Cast<Button>().ToList());
        Assert.All(main, i => Assert.Contains(ToolBar.ToolBarItemClass, i.Classes));
        window.Close();
    }

    [AvaloniaFact]
    public void Narrower_Bar_Overflows_More_And_Wider_Restores()
    {
        var (bar, items) = CreateBar();
        bar.Width = 400;
        var window = TestHelpers.Show(bar);
        var counts = new List<int>();
        foreach (var w in new double[] { 400, 220, 160, 100, 220, 400 })
        {
            bar.Width = w;
            TestHelpers.Layout(window);
            counts.Add(items.Count(ToolBar.GetIsOverflowItem));
        }

        Assert.Equal(0, counts[0]);
        Assert.True(counts[1] <= counts[2] && counts[2] <= counts[3], string.Join(",", counts));
        Assert.True(counts[3] > 0);
        Assert.Equal(counts[1], counts[4]);
        Assert.Equal(0, counts[5]);
        Assert.False(bar.HasOverflowItems);
        window.Close();
    }

    [AvaloniaFact]
    public void OverflowMode_Always_And_Never_Are_Respected()
    {
        var (bar, items) = CreateBar();
        ToolBar.SetOverflowMode(items[0], OverflowMode.Always);
        ToolBar.SetOverflowMode(items[7], OverflowMode.Never);
        bar.Width = 400;
        var window = TestHelpers.Show(bar);
        Assert.True(ToolBar.GetIsOverflowItem(items[0]));
        Assert.All(items.Skip(1), i => Assert.False(ToolBar.GetIsOverflowItem(i)));
        Assert.True(bar.HasOverflowItems);

        bar.Width = 80;
        TestHelpers.Layout(window);
        Assert.False(ToolBar.GetIsOverflowItem(items[7]));
        Assert.True(ToolBar.GetIsOverflowItem(items[0]));
        Assert.True(items.Skip(1).Take(6).Count(ToolBar.GetIsOverflowItem) > 0);

        // MinLength covers the item that can never overflow; MaxLength every item that can be in the main bar.
        Assert.True(bar.MinLength >= ItemWidth);
        Assert.True(bar.MaxLength >= ((items.Count - 1) * ItemWidth) - 0.01);
        window.Close();
    }

    [AvaloniaFact]
    public void Overflow_Panel_Hosts_The_Overflow_Items_When_Open()
    {
        var (bar, items) = CreateBar();
        bar.Width = 120;
        var window = TestHelpers.Show(bar);
        var overflowed = items.Where(ToolBar.GetIsOverflowItem).ToList();
        Assert.NotEmpty(overflowed);

        bar.IsOverflowOpen = true;
        TestHelpers.Layout(window);
        var popup = TestHelpers.Find<Popup>(bar, "OverflowPopup");
        Assert.True(popup.IsOpen);
        var panel = bar.ToolBarOverflowPanel!;
        TestHelpers.Layout(window);
        Assert.Equal(overflowed, panel.GetVisualChildren().Cast<Button>().ToList());
        Assert.All(overflowed, i => Assert.Same(bar, i.Parent));

        // Widen the bar: the items return to the main bar.
        bar.IsOverflowOpen = false;
        bar.Width = 400;
        TestHelpers.Layout(window);
        Assert.Empty(panel.GetVisualChildren());
        Assert.Equal(items, bar.ToolBarPanel!.GetVisualChildren().Cast<Button>().ToList());
        window.Close();
    }

    [AvaloniaFact]
    public void Overflow_Panel_Wraps_At_WrapWidth()
    {
        var panel = new ToolBarOverflowPanel { WrapWidth = 70 };
        for (var i = 0; i < 5; i++)
        {
            panel.Children.Add(new Border { Width = 30, Height = 20 });
        }

        var window = TestHelpers.Show(new Border { Child = panel, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
        Assert.Equal(new Size(60, 60), panel.DesiredSize);
        var rows = panel.Children.Select(c => c.Bounds.Y).ToList();
        Assert.Equal(new double[] { 0, 0, 20, 20, 40 }, rows);
        window.Close();
    }

    [AvaloniaFact]
    public void Overflow_Panel_Wraps_Items_Placed_By_Another_Host()
    {
        // The Ribbon's Quick Access Toolbar places its overflowed items' visuals here; they are not Children.
        var panel = new ToolBarOverflowPanel { WrapWidth = 70 };
        var items = Enumerable.Range(0, 3).Select(_ => new Border { Width = 30, Height = 20 }).ToList();
        for (var i = 0; i < items.Count; i++)
        {
            panel.InsertHostedItem(i, items[i]);
        }

        var window = TestHelpers.Show(new Border { Child = panel, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top });
        Assert.Empty(panel.Children);
        Assert.Equal(items, panel.HostedItems);
        Assert.Equal(new Size(60, 40), panel.DesiredSize);
        Assert.Equal(new double[] { 0, 0, 20 }, items.Select(c => c.Bounds.Y));

        panel.RemoveHostedItem(items[2]);
        window.UpdateLayout();
        Assert.Null(items[2].GetVisualParent());
        Assert.Equal(new Size(60, 20), panel.DesiredSize);
        window.Close();
    }

    [AvaloniaFact]
    public void Stock_Items_Get_The_Toolbar_Class()
    {
        var button = new Button();
        var toggle = new ToggleButton();
        var check = new CheckBox();
        var radio = new RadioButton();
        var combo = new ComboBox();
        var text = new TextBox();
        var separator = new Separator();
        var menu = new Menu();
        var custom = new RepeatButton();
        var bar = new ToolBar { ItemsSource = new Control[] { button, toggle, check, radio, combo, text, separator, menu, custom } };
        var window = TestHelpers.Show(bar);
        foreach (var c in new Control[] { button, toggle, check, radio, combo, text, separator, menu })
        {
            Assert.Contains(ToolBar.ToolBarItemClass, c.Classes);
        }

        Assert.DoesNotContain(ToolBar.ToolBarItemClass, custom.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void Orientation_Follows_The_Tray()
    {
        var bar = new ToolBar { ItemsSource = new[] { new Button { Content = "A" } } };
        var tray = new ToolBarTray { ToolBars = { bar } };
        var window = TestHelpers.Show(tray);
        Assert.Equal(Orientation.Horizontal, bar.Orientation);
        tray.Orientation = Orientation.Vertical;
        TestHelpers.Layout(window);
        Assert.Equal(Orientation.Vertical, bar.Orientation);
        Assert.Equal(Orientation.Vertical, bar.ToolBarPanel!.Orientation);
        Assert.Contains(":vertical", bar.Classes);
        window.Close();
    }
}
