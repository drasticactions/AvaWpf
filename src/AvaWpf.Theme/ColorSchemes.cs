using System;
using System.Collections.Generic;

namespace AvaWpf;

/// <summary>The color schemes of each <see cref="ThemeFamily"/>. Pass one to <see cref="AvaWpfTheme.ColorScheme"/>.</summary>
public static class ColorSchemes
{
    /// <summary>The only scheme of Aero2, AeroLite, Aero and Fluent.</summary>
    public const string Default = "Default";

    /// <summary>Luna Blue and Royale.</summary>
    public const string NormalColor = "NormalColor";

    /// <summary>Luna Silver.</summary>
    public const string Metallic = "Metallic";

    /// <summary>Luna Olive Green.</summary>
    public const string Homestead = "Homestead";

    /// <summary>Classic: Windows Standard (the Windows 2000 default).</summary>
    public const string WindowsStandard = "WindowsStandard";

    /// <summary>Classic: Windows Classic (the Windows 95/98 default).</summary>
    public const string WindowsClassic = "WindowsClassic";

    /// <summary>Classic: Brick.</summary>
    public const string Brick = "Brick";

    /// <summary>Classic: Desert.</summary>
    public const string Desert = "Desert";

    /// <summary>Classic: Eggplant.</summary>
    public const string Eggplant = "Eggplant";

    /// <summary>Classic: Lilac.</summary>
    public const string Lilac = "Lilac";

    /// <summary>Classic: Maple.</summary>
    public const string Maple = "Maple";

    /// <summary>Classic: Marine (high color).</summary>
    public const string Marine = "Marine";

    /// <summary>Classic: Plum (high color).</summary>
    public const string Plum = "Plum";

    /// <summary>Classic: Pumpkin (XP ships it only in its large size).</summary>
    public const string Pumpkin = "Pumpkin";

    /// <summary>Classic: Rainy Day.</summary>
    public const string RainyDay = "RainyDay";

    /// <summary>Classic: Red, White, and Blue (VGA).</summary>
    public const string RedWhiteBlue = "RedWhiteBlue";

    /// <summary>Classic: Rose.</summary>
    public const string Rose = "Rose";

    /// <summary>Classic: Slate.</summary>
    public const string Slate = "Slate";

    /// <summary>Classic: Spruce.</summary>
    public const string Spruce = "Spruce";

    /// <summary>Classic: Storm (VGA).</summary>
    public const string Storm = "Storm";

    /// <summary>Classic: Teal (VGA).</summary>
    public const string Teal = "Teal";

    /// <summary>Classic: Wheat.</summary>
    public const string Wheat = "Wheat";

    /// <summary>Classic: High Contrast #1.</summary>
    public const string HighContrast1 = "HighContrast1";

    /// <summary>Classic: High Contrast #2.</summary>
    public const string HighContrast2 = "HighContrast2";

    /// <summary>Classic: High Contrast Black.</summary>
    public const string HighContrastBlack = "HighContrastBlack";

    /// <summary>Classic: High Contrast White.</summary>
    public const string HighContrastWhite = "HighContrastWhite";

    private static readonly string[] s_single = [Default];
    private static readonly string[] s_luna = [NormalColor, Metallic, Homestead];
    private static readonly string[] s_royale = [NormalColor];

    private static readonly string[] s_classic =
    [
        WindowsStandard, WindowsClassic, Brick, Desert, Eggplant, Lilac, Maple, Marine, Plum, Pumpkin, RainyDay,
        RedWhiteBlue, Rose, Slate, Spruce, Storm, Teal, Wheat, HighContrast1, HighContrast2, HighContrastBlack,
        HighContrastWhite,
    ];

    /// <summary>The schemes of <paramref name="family"/>, default first.</summary>
    public static IReadOnlyList<string> For(ThemeFamily family) => family switch
    {
        ThemeFamily.Luna => s_luna,
        ThemeFamily.Royale => s_royale,
        ThemeFamily.Classic => s_classic,
        _ => s_single,
    };

    /// <summary>The default scheme of <paramref name="family"/>.</summary>
    public static string DefaultFor(ThemeFamily family) => For(family)[0];

    /// <summary>Resolves <paramref name="scheme"/> for <paramref name="family"/>; null gives the family default.</summary>
    /// <exception cref="ArgumentException">The family has no such scheme.</exception>
    public static string Resolve(ThemeFamily family, string? scheme)
    {
        if (scheme is null)
        {
            return DefaultFor(family);
        }

        foreach (var s in For(family))
        {
            if (string.Equals(s, scheme, StringComparison.OrdinalIgnoreCase))
            {
                return s;
            }
        }

        throw new ArgumentException($"The {family} theme has no color scheme '{scheme}'. Valid schemes: {string.Join(", ", For(family))}.", nameof(scheme));
    }

    /// <summary>True for the Classic high-contrast schemes.</summary>
    public static bool IsHighContrast(string scheme) => scheme.StartsWith("HighContrast", StringComparison.Ordinal);

    /// <summary>True for the Classic schemes whose colors are already dark (they serve Dark as is).</summary>
    public static bool IsDarkScheme(string scheme) => scheme is HighContrast1 or HighContrast2 or HighContrastBlack;
}
