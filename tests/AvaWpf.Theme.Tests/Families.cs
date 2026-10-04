using System.Collections.Generic;
using Avalonia.Styling;

namespace AvaWpf.Theme.Tests;

/// <summary>The family × scheme combinations the matrix tests cover, and the variants per family.</summary>
internal static class Families
{
    /// <summary>
    /// One scheme of every family, every Luna scheme, and the Classic default, a colored and a high-contrast scheme.
    /// Templates are identical across the schemes of a family, so the template tests use these.
    /// </summary>
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
        (ThemeFamily.Classic, ColorSchemes.Brick),
        (ThemeFamily.Classic, ColorSchemes.HighContrastBlack),
        (ThemeFamily.Fluent, ColorSchemes.Default),
    ];

    /// <summary>Every family and every one of its schemes (for the cheap resource tests).</summary>
    public static IEnumerable<(ThemeFamily Family, string Scheme)> AllSchemes()
    {
        foreach (var f in new[] { ThemeFamily.Aero2, ThemeFamily.AeroLite, ThemeFamily.Aero, ThemeFamily.Luna, ThemeFamily.Royale, ThemeFamily.Classic, ThemeFamily.Fluent })
        {
            foreach (var s in ColorSchemes.For(f))
            {
                yield return (f, s);
            }
        }
    }

    /// <summary>
    /// The families the matrix tests run for: all, or the comma-separated list in the <c>AVAWPF_TEST_FAMILY</c>
    /// environment variable (for example <c>AVAWPF_TEST_FAMILY=Aero2</c>) while working on one family.
    /// </summary>
    public static ThemeFamily[] Selected()
    {
        var all = System.Enum.GetValues<ThemeFamily>();
        var filter = System.Environment.GetEnvironmentVariable("AVAWPF_TEST_FAMILY");
        if (string.IsNullOrWhiteSpace(filter))
        {
            return all;
        }

        var names = filter.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries);
        return System.Array.FindAll(all, f => System.Array.Exists(names, n => string.Equals(n, f.ToString(), System.StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Light and Dark everywhere, plus high contrast for Fluent and Classic.</summary>
    public static ThemeVariant[] Variants(ThemeFamily family) => family is ThemeFamily.Fluent or ThemeFamily.Classic
        ? [ThemeVariant.Light, ThemeVariant.Dark, WpfThemeVariants.HighContrast]
        : [ThemeVariant.Light, ThemeVariant.Dark];
}
