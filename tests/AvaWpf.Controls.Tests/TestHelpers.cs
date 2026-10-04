using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaWpf.Controls.Tests;

/// <summary>Window, layout and pointer helpers for the behavior tests.</summary>
internal static class TestHelpers
{
    public static Window Show(Control content, double width = 800, double height = 400)
    {
        var window = new Window { Content = content, Width = width, Height = height };
        window.Show();
        Layout(window);
        return window;
    }

    public static void Layout(TopLevel window)
    {
        for (var i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    public static Point Center(Visual visual, Visual window) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), window)
        ?? throw new InvalidOperationException("The visual is not in the window.");

    /// <summary>Presses the left button at <paramref name="from"/>, moves in steps to <paramref name="to"/> and releases.</summary>
    public static void Drag(Window window, Point from, Point to, int steps = 8)
    {
        window.MouseDown(from, MouseButton.Left);
        Layout(window);
        for (var i = 1; i <= steps; i++)
        {
            var p = new Point(from.X + ((to.X - from.X) * i / steps), from.Y + ((to.Y - from.Y) * i / steps));
            window.MouseMove(p, RawInputModifiers.LeftMouseButton);
            Layout(window);
        }

        window.MouseUp(to, MouseButton.Left);
        Layout(window);
    }

    public static T Find<T>(Visual root, string? name = null) where T : Control =>
        root.GetVisualDescendants().OfType<T>().FirstOrDefault(c => name is null || c.Name == name)
        ?? throw new InvalidOperationException($"No {typeof(T).Name} {name} in the visual tree.");

    public static IEnumerable<T> All<T>(Visual root) where T : Visual => root.GetVisualDescendants().OfType<T>();
}
