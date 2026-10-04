using System;
using Avalonia;
using Avalonia.Layout;

namespace AvaWpf.Chrome;

/// <summary>
/// Snaps the rectangles of one render pass to device pixels (see <see cref="PixelSnap"/>). At a scaling of 1 every
/// whole-pixel coordinate stays where WPF puts it, so the chrome draws as WPF does at 96 DPI.
/// </summary>
internal readonly struct DeviceSnapper
{
    /// <summary>Initializes a snapper for a render scaling.</summary>
    public DeviceSnapper(double scale)
    {
        Scale = scale > 0 && !double.IsNaN(scale) ? scale : 1.0;
        Line = Math.Max(1.0, Math.Round(Scale)) / Scale;
    }

    /// <summary>The render scaling.</summary>
    public double Scale { get; }

    /// <summary>
    /// The width of a 1 px chrome line: the whole number of device pixels nearest to one device-independent pixel,
    /// and at least one.
    /// </summary>
    public double Line { get; }

    /// <summary>Creates a snapper for the scaling in effect at <paramref name="visual"/>.</summary>
    public static DeviceSnapper For(Layoutable visual) => new(PixelSnap.Scale(visual));

    /// <summary>Rounds a coordinate to the nearest device pixel.</summary>
    public double Snap(double value) => PixelSnap.Round(value, Scale);

    /// <summary>A filled rectangle with its edges on device pixels.</summary>
    public Rect Fill(double x, double y, double width, double height)
    {
        var l = Snap(x);
        var t = Snap(y);
        var r = Snap(x + width);
        var b = Snap(y + height);
        return new Rect(l, t, Math.Max(0, r - l), Math.Max(0, b - t));
    }

    /// <summary>A filled rectangle with its edges on device pixels.</summary>
    public Rect Fill(Rect rect) => Fill(rect.X, rect.Y, rect.Width, rect.Height);

    /// <summary>
    /// The rectangle <paramref name="lines"/> chrome lines inside the snapped rectangle, on device pixels. Snapping
    /// WPF's <c>new Rect(x + 1, y + 1, width - 2, height - 2)</c> directly can overlap the border at a fractional scaling.
    /// </summary>
    public Rect Inside(double x, double y, double width, double height, int lines)
    {
        var f = Fill(x, y, width, height);
        var d = Line * lines;
        return new Rect(f.X + d, f.Y + d, Math.Max(0, f.Width - (2 * d)), Math.Max(0, f.Height - (2 * d)));
    }

    /// <summary>The center line of a <see cref="Line"/>-wide stroke whose outer edge is <see cref="Inside"/>.</summary>
    public Rect StrokeInside(double x, double y, double width, double height, int lines)
    {
        var f = Inside(x, y, width, height, lines);
        var half = Line * 0.5;
        return new Rect(f.X + half, f.Y + half, Math.Max(0, f.Width - Line), Math.Max(0, f.Height - Line));
    }

    /// <summary>The center line of a <see cref="Line"/>-wide stroke whose outer edge is the given rectangle.</summary>
    public Rect Stroke(double x, double y, double width, double height)
    {
        var f = Fill(x, y, width, height);
        var half = Line * 0.5;
        return new Rect(f.X + half, f.Y + half, Math.Max(0, f.Width - Line), Math.Max(0, f.Height - Line));
    }
}
