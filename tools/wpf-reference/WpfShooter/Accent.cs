using System;
using System.Windows;
using System.Windows.Media;

namespace WpfShooter;

/// <summary>
/// Sets the accent ramp explicitly. WPF reads the accent from WinRT <c>UISettings</c>, which Wine does not have, so it
/// would fall back to one flat color. The ramp uses the same HSL lightness steps as AvaWpf (<c>AccentColors.Compute</c>),
/// so both sides see the same seven colors.
/// </summary>
public static class Accent
{
    public static void Apply(ResourceDictionary resources, Color accent)
    {
        var (h, s, l) = ToHsl(accent);
        Color L(double delta) => FromHsl(h, s, Math.Clamp(l + delta, 0, 1));
        Put(resources, SystemColors.AccentColorKey, SystemColors.AccentColorBrushKey, accent);
        Put(resources, SystemColors.AccentColorLight1Key, SystemColors.AccentColorLight1BrushKey, L(39 / 255d));
        Put(resources, SystemColors.AccentColorLight2Key, SystemColors.AccentColorLight2BrushKey, L(70 / 255d));
        Put(resources, SystemColors.AccentColorLight3Key, SystemColors.AccentColorLight3BrushKey, L(103 / 255d));
        Put(resources, SystemColors.AccentColorDark1Key, SystemColors.AccentColorDark1BrushKey, L(-28.5 / 255d));
        Put(resources, SystemColors.AccentColorDark2Key, SystemColors.AccentColorDark2BrushKey, L(-49 / 255d));
        Put(resources, SystemColors.AccentColorDark3Key, SystemColors.AccentColorDark3BrushKey, L(-74.5 / 255d));
    }

    private static void Put(ResourceDictionary resources, object colorKey, object brushKey, Color c)
    {
        resources[colorKey] = c;
        var brush = new SolidColorBrush(c);
        brush.Freeze();
        resources[brushKey] = brush;
    }

    private static (double H, double S, double L) ToHsl(Color c)
    {
        double r = c.R / 255d, g = c.G / 255d, b = c.B / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var chroma = max - min;
        var l = (max + min) / 2;
        double h = 0, s = 0;
        if (chroma > 0)
        {
            s = chroma / (1 - Math.Abs(2 * l - 1));
            if (max == r)
            {
                h = (g - b) / chroma % 6;
            }
            else if (max == g)
            {
                h = (b - r) / chroma + 2;
            }
            else
            {
                h = (r - g) / chroma + 4;
            }

            h *= 60;
            if (h < 0)
            {
                h += 360;
            }
        }

        return (h, s, l);
    }

    private static Color FromHsl(double h, double s, double l)
    {
        var chroma = (1 - Math.Abs(2 * l - 1)) * s;
        var h1 = h / 60;
        var x = chroma * (1 - Math.Abs(h1 % 2 - 1));
        var (r, g, b) = h1 switch
        {
            < 1 => (chroma, x, 0d),
            < 2 => (x, chroma, 0d),
            < 3 => (0d, chroma, x),
            < 4 => (0d, x, chroma),
            < 5 => (x, 0d, chroma),
            _ => (chroma, 0d, x),
        };
        var m = l - chroma / 2;
        static byte B(double v) => (byte)Math.Round(Math.Clamp(v, 0, 1) * 255);
        return Color.FromRgb(B(r + m), B(g + m), B(b + m));
    }
}
