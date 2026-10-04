using System;
using Avalonia;
using Avalonia.Layout;

namespace AvaWpf.Chrome;

/// <summary>
/// Device-pixel snapping for the code-drawn chrome. Avalonia has no guideline sets, so each edge is rounded to the
/// device-pixel grid; layout rounding puts the chrome's origin on that grid.
/// </summary>
internal static class PixelSnap
{
    /// <summary>The device pixels per DIP for <paramref name="control"/> (1.0 when it is not in a visual tree).</summary>
    public static double Scale(Layoutable control)
    {
        var scale = LayoutHelper.GetLayoutScale(control);
        return scale > 0 && !double.IsNaN(scale) && !double.IsInfinity(scale) ? scale : 1.0;
    }

    /// <summary>Rounds a DIP coordinate to the nearest device-pixel edge at <paramref name="scale"/>.</summary>
    public static double Round(double value, double scale)
    {
        if (scale <= 0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            return value;
        }

        return Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
    }

    /// <summary>Snaps each edge of <paramref name="rect"/> to the device-pixel grid; a degenerate result has zero size.</summary>
    public static Rect Rect(Rect rect, double scale)
    {
        var left = Round(rect.Left, scale);
        var top = Round(rect.Top, scale);
        var right = Round(rect.Right, scale);
        var bottom = Round(rect.Bottom, scale);
        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    /// <summary>Deflates <paramref name="rect"/> by <paramref name="thickness"/> (clamped at zero size), then snaps it.</summary>
    public static Rect Inset(Rect rect, Thickness thickness, double scale)
    {
        var inner = new Rect(
            rect.Left + thickness.Left,
            rect.Top + thickness.Top,
            Math.Max(0.0, rect.Width - thickness.Left - thickness.Right),
            Math.Max(0.0, rect.Height - thickness.Top - thickness.Bottom));
        return Rect(inner, scale);
    }
}
