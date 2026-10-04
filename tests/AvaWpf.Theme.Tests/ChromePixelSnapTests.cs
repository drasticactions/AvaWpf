using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using AvaWpf.Chrome.Aero;
using AvaWpf.Chrome.Classic;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// Chrome pixel snapping: rendered at a render scaling of 1.0, 1.25, 1.5 and 2.0, every 1 px bevel line of
/// the chrome has its edges on the device-pixel grid, as WPF's guideline snapping puts them: a line from DIP a to DIP
/// b covers exactly the device pixels round(a × scale) to round(b × scale), in one pure color, with no antialiased
/// pixel on either side. At 1.0 and 1.25 a line is one device pixel; at 1.5 the two lines of a bevel take 2 and 1
/// pixels (3 for the 2 DIPs); at 2.0 each takes two.
/// </summary>
public class ChromePixelSnapTests
{
    public static TheoryData<double> Scalings => new() { 1.0, 1.25, 1.5, 2.0 };

    [AvaloniaTheory]
    [MemberData(nameof(Scalings))]
    public void ClassicBorderDecorator_Raised_Bevel_Lines_Stay_Whole_Device_Pixels(double scaling)
    {
        // Distinct colors for the four bevel roles and the face (in Windows Standard ControlLight equals Control).
        var theme = TestApplication.Instance.Theme;
        var lightLight = Color.Parse("#FFFFFF");
        var light = Color.Parse("#E0E0E0");
        var face = Color.Parse("#C0C0C0");
        var dark = Color.Parse("#808080");
        var darkDark = Color.Parse("#404040");
        theme.SystemColorOverrides["ControlLightLight"] = lightLight;
        theme.SystemColorOverrides["ControlLight"] = light;
        theme.SystemColorOverrides["Control"] = face;
        theme.SystemColorOverrides["ControlDark"] = dark;
        theme.SystemColorOverrides["ControlDarkDark"] = darkDark;
        try
        {
            // WPF Button.xaml Classic: Raised with BorderThickness 3 (three 1 DIP rings; Raised draws the outer two).
            var decorator = new ClassicBorderDecorator
            {
                Width = 40,
                Height = 30,
                BorderStyle = ClassicBorderStyle.Raised,
                BorderBrush = ClassicBorderDecorator.ClassicBorderBrush,
                BorderThickness = new Thickness(3),
                [!ClassicBorderDecorator.BackgroundProperty] = new DynamicResourceExtension("SystemColors.ControlBrush"),
            };
            using var scene = ChromeScene.Show(ThemeFamily.Classic, ColorSchemes.WindowsStandard, decorator, 56, 46, scaling);
            var pixels = Capture(scene);

            // WPF ClassicBorderDecorator.DrawRaisedBorder: ControlLightLight / ControlDarkDark outside, ControlLight /
            // ControlDark inside, then the face. The chrome snaps each ring edge, measured from its own origin (which
            // layout rounding puts on a device pixel), to the nearest device pixel.
            var bounds = scene.BoundsOf(decorator);
            var x0 = Device(bounds.X, scaling);
            var y0 = Device(bounds.Y, scaling);
            var midY = Device(bounds.Y + 15, scaling);
            var midX = Device(bounds.X + 20, scaling);
            AssertBands($"left at {scaling}", x => pixels.At(x, midY), x0, scaling, (0, 1, lightLight), (1, 2, light), (2, 4, face));
            AssertBands($"right at {scaling}", x => pixels.At(x, midY), x0, scaling, (bounds.Width - 4, bounds.Width - 2, face), (bounds.Width - 2, bounds.Width - 1, dark), (bounds.Width - 1, bounds.Width, darkDark));
            AssertBands($"top at {scaling}", y => pixels.At(midX, y), y0, scaling, (0, 1, lightLight), (1, 2, light), (2, 4, face));
            AssertBands($"bottom at {scaling}", y => pixels.At(midX, y), y0, scaling, (bounds.Height - 4, bounds.Height - 2, face), (bounds.Height - 2, bounds.Height - 1, dark), (bounds.Height - 1, bounds.Height, darkDark));
        }
        finally
        {
            theme.SystemColorOverrides.Clear();
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Scalings))]
    public void Aero_ButtonChrome_Border_Stays_Whole_Device_Pixels(double scaling)
    {
        var chrome = new ButtonChrome
        {
            Width = 75,
            Height = 23,
            RoundCorners = true,
            [!ButtonChrome.BackgroundProperty] = new DynamicResourceExtension("Aero.ButtonNormalBackground"),
            [!ButtonChrome.BorderBrushProperty] = new DynamicResourceExtension("Aero.ButtonNormalBorder"),
        };
        using var scene = ChromeScene.Show(ThemeFamily.Aero, null, chrome, 91, 39, scaling);
        var pixels = Capture(scene);

        // WPF Aero ButtonChrome: the 1 DIP #707070 border (ButtonNormalBorder) on the outside, then the white inner
        // border. The chrome strokes its lines max(1, round(scale)) device pixels wide on the device grid
        // (DeviceSnapper), as WPF's guidelines do: the border is a run of pure #707070 with the surface (F0F0F0) right
        // outside it and no blend with the border color right inside it. Mid height and mid width are away from the
        // rounded corners.
        var border = Color.Parse("#707070");
        var surface = Color.Parse("#F0F0F0");
        var line = Math.Max(1, (int)Math.Round(scaling, MidpointRounding.ToEven));
        var bounds = scene.BoundsOf(chrome);
        var x0 = Device(bounds.X, scaling);
        var x1 = Device(bounds.Right, scaling);
        var y0 = Device(bounds.Y, scaling);
        var midY = Device(bounds.Y + 11, scaling);
        var midX = Device(bounds.X + 37, scaling);
        AssertLine($"left at {scaling}", Enumerable.Range(x0 - 1, line + 2).Select(x => pixels.At(x, midY)).ToArray(), surface, border, line);
        AssertLine($"right at {scaling}", Enumerable.Range(x1 - line - 1, line + 2).Select(x => pixels.At(x, midY)).Reverse().ToArray(), surface, border, line);
        AssertLine($"top at {scaling}", Enumerable.Range(y0 - 1, line + 2).Select(y => pixels.At(midX, y)).ToArray(), surface, border, line);
    }

    /// <summary>
    /// Asserts outside, then <paramref name="line"/> pixels of the line color, then an inside pixel that is no blend
    /// with the line color (much lighter than the dark border).
    /// </summary>
    private static void AssertLine(string what, Color[] run, Color outside, Color lineColor, int line)
    {
        var text = Describe(run);
        Assert.True(run[0] == outside, $"{what}: outside should be {Describe([outside])}: {text}");
        Assert.True(run.Skip(1).Take(line).All(c => c == lineColor), $"{what}: the line should be {line} px of {Describe([lineColor])}: {text}");
        Assert.True(run[line + 1].R >= 0xD0, $"{what}: the pixel inside the line is antialiased with it: {text}");
    }

    /// <summary>
    /// Asserts that each band (start, end, color) in DIPs from the chrome origin <paramref name="origin"/> (a device
    /// pixel) covers exactly the device pixels between its snapped edges, in that one color.
    /// </summary>
    private static void AssertBands(string what, Func<int, Color> at, int origin, double scaling, params (double From, double To, Color Color)[] bands)
    {
        int Edge(double dip) => origin + Device(dip, scaling);
        var first = Edge(bands[0].From);
        var last = Edge(bands[^1].To);
        var actual = Enumerable.Range(first, last - first).Select(at).ToArray();
        var expected = bands.SelectMany(b => Enumerable.Repeat(b.Color, Edge(b.To) - Edge(b.From))).ToArray();
        Assert.True(expected.SequenceEqual(actual), $"{what}: expected {Describe(expected)}, rendered {Describe(actual)}");
    }

    private static int Device(double dip, double scaling) => (int)Math.Round(dip * scaling, MidpointRounding.AwayFromZero);

    private static string Describe(IEnumerable<Color> colors) => string.Join(" ", colors.Select(c => $"{c.R:X2}{c.G:X2}{c.B:X2}"));

    private static Pixels Capture(ChromeScene scene)
    {
        ChromeScene.Pump();
        using var frame = scene.Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("nothing rendered");
        var size = frame.PixelSize;
        var stride = size.Width * 4;
        var data = new byte[stride * size.Height];
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            frame.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), data.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        return new Pixels(data, size.Width, frame.Format ?? Avalonia.Platform.PixelFormat.Bgra8888);
    }

    private sealed class Pixels(byte[] data, int width, Avalonia.Platform.PixelFormat format)
    {
        public Color At(int x, int y)
        {
            var i = ((y * width) + x) * 4;
            return format == Avalonia.Platform.PixelFormat.Rgba8888
                ? Color.FromRgb(data[i], data[i + 1], data[i + 2])
                : Color.FromRgb(data[i + 2], data[i + 1], data[i]);
        }
    }
}
