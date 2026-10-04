using System;
using System.Collections.Generic;
using Avalonia.Styling;

namespace AvaWpf.DataGrid.Tests;

/// <summary>The family × scheme combinations and the variants the DataGrid matrix covers.</summary>
internal static class Families
{
    /// <summary>Every family once, every Luna scheme, and the Classic default and high-contrast schemes.</summary>
    public static readonly (ThemeFamily Family, string Scheme)[] Matrix =
    [
        (ThemeFamily.Aero2, ColorSchemes.Default),
        (ThemeFamily.AeroLite, ColorSchemes.Default),
        (ThemeFamily.Aero, ColorSchemes.Default),
        (ThemeFamily.Luna, ColorSchemes.NormalColor),
        (ThemeFamily.Luna, ColorSchemes.Metallic),
        (ThemeFamily.Luna, ColorSchemes.Homestead),
        (ThemeFamily.Royale, ColorSchemes.NormalColor),
        (ThemeFamily.Classic, ColorSchemes.WindowsStandard),
        (ThemeFamily.Classic, ColorSchemes.HighContrastBlack),
        (ThemeFamily.Fluent, ColorSchemes.Default),
    ];

    /// <summary>
    /// The families the matrix runs for: all, or the comma-separated list in <c>AVAWPF_TEST_FAMILY</c>.
    /// </summary>
    public static ThemeFamily[] Selected()
    {
        var all = Enum.GetValues<ThemeFamily>();
        var filter = Environment.GetEnvironmentVariable("AVAWPF_TEST_FAMILY");
        if (string.IsNullOrWhiteSpace(filter))
        {
            return all;
        }

        var names = filter.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return Array.FindAll(all, f => Array.Exists(names, n => string.Equals(n, f.ToString(), StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>The matrix rows of the selected families.</summary>
    public static IEnumerable<(ThemeFamily Family, string Scheme)> SelectedMatrix()
    {
        var selected = Selected();
        foreach (var m in Matrix)
        {
            if (Array.IndexOf(selected, m.Family) >= 0)
            {
                yield return m;
            }
        }
    }

    /// <summary>Light and Dark everywhere, plus high contrast for Fluent and Classic.</summary>
    public static ThemeVariant[] Variants(ThemeFamily family) => family is ThemeFamily.Fluent or ThemeFamily.Classic
        ? [ThemeVariant.Light, ThemeVariant.Dark, WpfThemeVariants.HighContrast]
        : [ThemeVariant.Light, ThemeVariant.Dark];

    /// <summary>The variant of a test-case key.</summary>
    public static ThemeVariant Variant(string key) => key switch
    {
        "Dark" => ThemeVariant.Dark,
        "HighContrast" => WpfThemeVariants.HighContrast,
        _ => ThemeVariant.Light,
    };
}
