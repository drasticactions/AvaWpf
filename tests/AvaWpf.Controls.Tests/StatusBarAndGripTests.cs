using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Xunit;

namespace AvaWpf.Controls.Tests;

/// <summary>StatusBar dock layout and the ResizeGrip drag request.</summary>
public class StatusBarAndGripTests
{
    [AvaloniaFact]
    public void StatusBar_Docks_Items_And_The_Last_Fills()
    {
        var left = new StatusBarItem { Content = "Left", Width = 80 };
        var right = new StatusBarItem { Content = "Right", Width = 60 };
        DockPanel.SetDock(right, Dock.Right);
        var separator = new Separator();
        var fill = new StatusBarItem { Content = "Fill" };
        var bar = new StatusBar { ItemsSource = new Control[] { left, right, separator, fill }, Width = 400, VerticalAlignment = VerticalAlignment.Top };
        var window = TestHelpers.Show(bar);

        Assert.IsType<DockPanel>(bar.ItemsPanelRoot);
        Assert.Equal(0, left.Bounds.X);
        Assert.Equal(bar.ItemsPanelRoot!.Bounds.Width, right.Bounds.Right, 3);
        Assert.Equal(separator.Bounds.Right + separator.Margin.Right, fill.Bounds.X, 3);
        Assert.True(separator.Bounds.X >= left.Bounds.Right);
        Assert.Equal(right.Bounds.X, fill.Bounds.Right, 3);
        Assert.Contains(StatusBar.SeparatorClass, separator.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void StatusBar_Wraps_Plain_Items_In_StatusBarItems()
    {
        var bar = new StatusBar { ItemsSource = new object[] { "Ready", 42 } };
        var window = TestHelpers.Show(bar);
        Assert.IsType<StatusBarItem>(bar.ContainerFromIndex(0));
        Assert.Equal("Ready", ((StatusBarItem)bar.ContainerFromIndex(0)!).Content);
        Assert.Equal(new Thickness(3), ((StatusBarItem)bar.ContainerFromIndex(1)!).Padding);
        window.Close();
    }

    private static (Window Window, ResizeGrip Grip) ShowGrip(FlowDirection flow = FlowDirection.LeftToRight)
    {
        var grip = new ResizeGrip { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        var window = new Window { Content = new Panel { Children = { grip } }, Width = 300, Height = 200, FlowDirection = flow };
        window.Show();
        TestHelpers.Layout(window);
        return (window, grip);
    }

    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight, WindowEdge.SouthEast)]
    [InlineData(FlowDirection.RightToLeft, WindowEdge.SouthWest)]
    public void ResizeGrip_Press_Requests_A_Corner_Resize(FlowDirection flow, WindowEdge expected)
    {
        var (window, grip) = ShowGrip(flow);
        Assert.True(grip.Bounds.Width >= 17 && grip.Bounds.Height >= 17, $"{grip.Bounds}");
        var requests = new List<WindowEdge>();
        grip.ResizeDragRequested += (_, edge) => requests.Add(edge);

        window.MouseDown(TestHelpers.Center(grip, window), MouseButton.Left);
        window.MouseUp(TestHelpers.Center(grip, window), MouseButton.Left);

        Assert.Equal(new[] { expected }, requests);
        window.Close();
    }

    [AvaloniaFact]
    public void ResizeGrip_Hides_When_Maximized_Or_Fixed_Size()
    {
        var (window, grip) = ShowGrip();
        Assert.True(grip.IsVisible);
        window.WindowState = WindowState.Maximized;
        TestHelpers.Layout(window);
        Assert.False(grip.IsVisible);
        window.WindowState = WindowState.Normal;
        TestHelpers.Layout(window);
        Assert.True(grip.IsVisible);
        window.CanResize = false;
        TestHelpers.Layout(window);
        Assert.False(grip.IsVisible);
        window.Close();
    }
}
