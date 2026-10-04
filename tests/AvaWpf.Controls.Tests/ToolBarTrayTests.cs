using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Xunit;

namespace AvaWpf.Controls.Tests;

/// <summary>ToolBarTray band layout and gripper drag with pointer simulation.</summary>
public class ToolBarTrayTests
{
    private static ToolBar Bar(int items, int band = 0, int bandIndex = 0) => new()
    {
        Band = band,
        BandIndex = bandIndex,
        ItemsSource = Enumerable.Range(0, items).Select(i => new Button { Content = i.ToString(), Width = 30, Height = 22 }).ToList(),
    };

    private static (Window Window, ToolBarTray Tray) Show(params ToolBar[] bars)
    {
        var tray = new ToolBarTray { VerticalAlignment = VerticalAlignment.Top };
        tray.ToolBars.AddRange(bars);
        var window = TestHelpers.Show(tray, 800, 300);
        return (window, tray);
    }

    private static Point Gripper(ToolBar bar, Window window) => TestHelpers.Center(TestHelpers.Find<Thumb>(bar, "PART_Gripper"), window);

    [AvaloniaFact]
    public void Bands_Are_Grouped_By_Band_And_Ordered_By_BandIndex()
    {
        var a = Bar(2, band: 1, bandIndex: 5);
        var b = Bar(2, band: 1, bandIndex: 2);
        var c = Bar(2, band: 0, bandIndex: 0);
        var (window, tray) = Show(a, b, c);

        // Normalized to 0, 1, ... in order.
        Assert.Equal((0, 0), (c.Band, c.BandIndex));
        Assert.Equal((1, 0), (b.Band, b.BandIndex));
        Assert.Equal((1, 1), (a.Band, a.BandIndex));
        Assert.Equal(0, c.Bounds.Y);
        Assert.Equal(c.Bounds.Height, b.Bounds.Y);
        Assert.Equal(b.Bounds.Right, a.Bounds.X);
        Assert.Equal(new[] { c, b, a }, tray.OrderedToolBars());
        window.Close();
    }

    [AvaloniaFact]
    public void Dragging_Below_The_Bands_Makes_A_New_Band()
    {
        var a = Bar(3);
        var b = Bar(3, bandIndex: 1);
        var (window, tray) = Show(a, b);
        Assert.Equal(a.Bounds.Right, b.Bounds.X);
        var start = Gripper(b, window);

        TestHelpers.Drag(window, start, new Point(start.X, start.Y + a.Bounds.Height + 5));

        Assert.Equal(0, a.Band);
        Assert.Equal(1, b.Band);
        Assert.Equal(a.Bounds.Height, b.Bounds.Y);
        window.Close();
    }

    [AvaloniaFact]
    public void Dragging_Into_Another_Band_At_Its_Start_Inserts_First()
    {
        var a = Bar(3);
        var b = Bar(3, band: 1);
        var (window, tray) = Show(a, b);
        var start = Gripper(b, window);

        // Up into band 0, left of every toolbar.
        TestHelpers.Drag(window, start, new Point(start.X - 20, start.Y - b.Bounds.Height));

        Assert.Equal(0, b.Band);
        Assert.Equal(0, a.Band);
        Assert.Equal(0, b.BandIndex);
        Assert.Equal(1, a.BandIndex);
        Assert.Equal(new[] { b, a }, tray.OrderedToolBars());
        window.Close();
    }

    [AvaloniaFact]
    public void Dragging_Left_Shrinks_The_Toolbars_Before_To_Their_Minimum()
    {
        var a = Bar(8);
        var b = Bar(2, bandIndex: 1);
        var (window, tray) = Show(a, b);
        var widthBefore = a.Bounds.Width;
        Assert.False(a.HasOverflowItems);
        var start = Gripper(b, window);

        TestHelpers.Drag(window, start, new Point(start.X - 70, start.Y));

        Assert.True(a.Bounds.Width < widthBefore - 30, $"{a.Bounds.Width} vs {widthBefore}");
        Assert.True(a.Bounds.Width >= a.MinLength);
        Assert.True(a.HasOverflowItems);
        Assert.Equal(a.Bounds.Right, b.Bounds.X);
        Assert.Equal(0, b.Band);

        // Far past the minimum: a stops at its minimum length, then the toolbars swap.
        start = Gripper(b, window);
        TestHelpers.Drag(window, start, new Point(-5, start.Y));
        Assert.Equal(new[] { b, a }, tray.OrderedToolBars());
        window.Close();
    }

    [AvaloniaFact]
    public void Locked_Tray_Does_Not_Move_Toolbars()
    {
        var a = Bar(3);
        var b = Bar(3, bandIndex: 1);
        var (window, tray) = Show(a, b);
        var start = Gripper(b, window);
        tray.IsLocked = true;
        TestHelpers.Layout(window);
        Assert.Contains(":locked", b.Classes);
        Assert.False(TestHelpers.Find<Thumb>(b, "PART_Gripper").IsVisible);

        TestHelpers.Drag(window, start, new Point(start.X, start.Y + 60));
        Assert.Equal(0, b.Band);
        window.Close();
    }

    [AvaloniaFact]
    public void Ctrl_Tab_Moves_Between_Toolbars()
    {
        var a = Bar(2);
        var b = Bar(2, bandIndex: 1);
        var (window, _) = Show(a, b);
        var first = (Button)a.ItemsView[0]!;
        Assert.True(first.Focus());

        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);
        TestHelpers.Layout(window);
        Assert.Same(b.ItemsView[0], window.FocusManager!.GetFocusedElement());

        // Arrows move within a toolbar.
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Same(b.ItemsView[1], window.FocusManager!.GetFocusedElement());
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Assert.Same(b.ItemsView[0], window.FocusManager!.GetFocusedElement());
        window.Close();
    }
}
