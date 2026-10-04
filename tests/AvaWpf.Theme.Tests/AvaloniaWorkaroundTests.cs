using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>The process-wide Avalonia fixes the theme installs (AvaloniaWorkarounds).</summary>
public class AvaloniaWorkaroundTests
{
    [AvaloniaFact]
    public void Changing_Only_The_RenderTransformOrigin_Redraws()
    {
        // WPF's indeterminate ProgressBar sweep animates the origin of a quarter-width block and nothing else.
        var block = new Rectangle { Width = 100, Height = 20, Fill = Brushes.Red, RenderTransform = TransformOperations.Parse("scaleX(0.25)"), RenderTransformOrigin = new RelativePoint(0, 0.5, RelativeUnit.Relative) };
        using var scene = ChromeScene.Show(ThemeFamily.Aero2, null, block, 140, 50);
        var left = scene.BoundsOf(block);
        Assert.Equal(Colors.Red, PixelAt(scene, (int)left.X + 10, (int)left.Center.Y));

        block.RenderTransformOrigin = new RelativePoint(1, 0.5, RelativeUnit.Relative);

        Assert.NotEqual(Colors.Red, PixelAt(scene, (int)left.X + 10, (int)left.Center.Y));
        Assert.Equal(Colors.Red, PixelAt(scene, (int)left.Right - 10, (int)left.Center.Y));
    }

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Luna)]
    [InlineData(ThemeFamily.Royale)]
    public void Indeterminate_Block_Marquee_Moves(ThemeFamily family)
    {
        // The block brush steps one block per 100 ms tick; Avalonia ignores a change inside a DrawingBrush's
        // transform, so each step must reach the screen as a new transform.
        var bar = new ProgressBar { IsIndeterminate = true, Width = 300, Height = 18 };
        using var scene = ChromeScene.Show(family, null, bar, 400, 60);
        var tick = typeof(AvaWpfTheme).Assembly.GetType("AvaWpf.Converters.ProgressBarMarquee")!
            .GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Static)!;
        var row = scene.BoundsOf(bar);
        var before = Row(scene, (int)row.Center.Y, (int)row.X, (int)row.Width);

        for (var i = 0; i < 3; i++)
        {
            tick.Invoke(null, [null, EventArgs.Empty]);
        }

        var after = Row(scene, (int)row.Center.Y, (int)row.X, (int)row.Width);
        Assert.NotEqual(before, after);
    }

    [AvaloniaFact]
    public void Control_Characters_Are_Not_Typed_Into_A_TextBox()
    {
        // The Wayland backend sends an unhandled Escape on as TextInput "\x1b".
        var box = new TextBox { Width = 120 };
        using var scene = ChromeScene.Show(ThemeFamily.Aero2, null, box, 200, 60);
        box.Focus();

        scene.Window.KeyTextInput("\x1b");
        scene.Window.KeyTextInput("a");

        Assert.Equal("a", box.Text);
    }

    private static Color PixelAt(ChromeScene scene, int x, int y) => Row(scene, y, x, 1)[0];

    private static Color[] Row(ChromeScene scene, int y, int x, int width)
    {
        ChromeScene.Pump();
        using var frame = scene.Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("nothing rendered");
        var stride = frame.PixelSize.Width * 4;
        var data = new byte[stride * frame.PixelSize.Height];
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            frame.CopyPixels(new PixelRect(frame.PixelSize), handle.AddrOfPinnedObject(), data.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        var rgba = frame.Format == Avalonia.Platform.PixelFormat.Rgba8888;
        return Enumerable.Range(x, width).Select(px =>
        {
            var i = (y * stride) + (px * 4);
            return rgba ? Color.FromRgb(data[i], data[i + 1], data[i + 2]) : Color.FromRgb(data[i + 2], data[i + 1], data[i]);
        }).ToArray();
    }
}
