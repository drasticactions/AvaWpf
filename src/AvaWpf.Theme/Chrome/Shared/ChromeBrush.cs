using System;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaWpf.Chrome;

/// <summary>
/// Brush helpers for the animated chrome: each render pass builds an immutable copy of a token brush with the animated
/// opacity and stop colors.
/// </summary>
internal static class ChromeBrush
{
    /// <summary>
    /// The colors of the first <paramref name="count"/> stops of a gradient brush (the color of a solid brush). Missing
    /// stops repeat the last color; a null brush gives transparent colors.
    /// </summary>
    public static Color[] StopColors(IBrush? brush, int count)
    {
        var result = new Color[count];
        var last = Colors.Transparent;
        for (var i = 0; i < count; i++)
        {
            switch (brush)
            {
                case IGradientBrush g when i < g.GradientStops.Count:
                    last = g.GradientStops[i].Color;
                    break;
                case ISolidColorBrush s:
                    last = s.Color;
                    break;
            }

            result[i] = last;
        }

        return result;
    }

    /// <summary>
    /// A copy of <paramref name="template"/> (same kind, points, offsets and spread) with the colors of its first stops
    /// replaced by <paramref name="colors"/> and its opacity set to <paramref name="opacity"/>. A solid brush takes the
    /// first color.
    /// </summary>
    public static IImmutableBrush? With(IBrush? template, ReadOnlySpan<Color> colors, double opacity)
    {
        switch (template)
        {
            case ILinearGradientBrush lg:
                return new ImmutableLinearGradientBrush(Stops(lg, colors), opacity, null, null, lg.SpreadMethod, lg.StartPoint, lg.EndPoint);
            case IRadialGradientBrush rg:
                return new ImmutableRadialGradientBrush(Stops(rg, colors), opacity, null, null, rg.SpreadMethod, rg.Center, rg.GradientOrigin, rg.RadiusX, rg.RadiusY);
            case ISolidColorBrush s:
                return new ImmutableSolidColorBrush(colors.Length > 0 ? colors[0] : s.Color, opacity);
            default:
                return null;
        }
    }

    /// <summary>A copy of <paramref name="template"/> with its opacity set to <paramref name="opacity"/>.</summary>
    public static IImmutableBrush? WithOpacity(IBrush? template, double opacity) => With(template, default, opacity);

    /// <summary>A pen of <paramref name="thickness"/> with an immutable brush, or null for a null brush.</summary>
    public static IPen? Pen(IImmutableBrush? brush, double thickness) => brush is null ? null : new ImmutablePen(brush, thickness);

    private static ImmutableGradientStop[] Stops(IGradientBrush brush, ReadOnlySpan<Color> colors)
    {
        var stops = new ImmutableGradientStop[brush.GradientStops.Count];
        for (var i = 0; i < stops.Length; i++)
        {
            var stop = brush.GradientStops[i];
            stops[i] = new ImmutableGradientStop(stop.Offset, i < colors.Length ? colors[i] : stop.Color);
        }

        return stops;
    }
}
