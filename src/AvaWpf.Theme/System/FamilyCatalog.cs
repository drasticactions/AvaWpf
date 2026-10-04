using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Styling;

namespace AvaWpf;

/// <summary>Loads each family's tokens and ControlThemes, and lists its app-facing aliases.</summary>
internal static class FamilyCatalog
{
    /// <summary>
    /// The tokens of a family and scheme, keyed by theme variant (Fluent adds high contrast).
    /// Every call creates fresh instances, because a dictionary has one owner and every scope owns its own.
    /// </summary>
    public static ResourceDictionary LoadTokens(ThemeFamily family, string scheme)
    {
        var tokens = new ResourceDictionary();
        var (light, dark) = (family, scheme) switch
        {
            (ThemeFamily.Aero2, _) => ((ResourceDictionary)new Themes.Aero2.TokensLight(), (ResourceDictionary)new Themes.Aero2.TokensDark()),
            (ThemeFamily.AeroLite, _) => (new Themes.AeroLite.TokensLight(), new Themes.AeroLite.TokensDark()),
            (ThemeFamily.Aero, _) => (new Themes.Aero.TokensLight(), new Themes.Aero.TokensDark()),
            (ThemeFamily.Luna, ColorSchemes.Metallic) => (new Themes.Luna.TokensMetallicLight(), new Themes.Luna.TokensMetallicDark()),
            (ThemeFamily.Luna, ColorSchemes.Homestead) => (new Themes.Luna.TokensHomesteadLight(), new Themes.Luna.TokensHomesteadDark()),
            (ThemeFamily.Luna, _) => (new Themes.Luna.TokensNormalColorLight(), new Themes.Luna.TokensNormalColorDark()),
            (ThemeFamily.Royale, _) => (new Themes.Royale.TokensLight(), new Themes.Royale.TokensDark()),
            (ThemeFamily.Classic, _) => (new Themes.Classic.TokensLight(), new Themes.Classic.TokensDark()),
            _ => (new Themes.Fluent.TokensLight(), new Themes.Fluent.TokensDark()),
        };
        tokens.ThemeDictionaries[ThemeVariant.Default] = light;
        tokens.ThemeDictionaries[ThemeVariant.Dark] = dark;
        if (family == ThemeFamily.Fluent)
        {
            tokens.ThemeDictionaries[WpfThemeVariants.HighContrast] = new Themes.Fluent.TokensHighContrast();
        }

        return tokens;
    }

    /// <summary>The ControlThemes of a family (<c>Themes/&lt;Family&gt;/_Family.axaml</c>), a fresh instance.</summary>
    public static IResourceProvider LoadControls(ThemeFamily family) => family switch
    {
        ThemeFamily.Aero2 => new Themes.Aero2.FamilyControls(),
        ThemeFamily.AeroLite => new Themes.AeroLite.FamilyControls(),
        ThemeFamily.Aero => new Themes.Aero.FamilyControls(),
        ThemeFamily.Luna => new Themes.Luna.FamilyControls(),
        ThemeFamily.Royale => new Themes.Royale.FamilyControls(),
        ThemeFamily.Classic => new Themes.Classic.FamilyControls(),
        _ => new Themes.Fluent.FamilyControls(),
    };

    /// <summary>
    /// The app-facing aliases: unprefixed keys apps can use in their own XAML, resolved to the family's resources.
    /// </summary>
    public static IReadOnlyDictionary<string, ResourceAlias> AppAliases(ThemeFamily family)
    {
        var fluent = family == ThemeFamily.Fluent;
        var accented = family is ThemeFamily.Aero2 or ThemeFamily.AeroLite or ThemeFamily.Fluent;
        return new Dictionary<string, ResourceAlias>(StringComparer.Ordinal)
        {
            ["WpfWindowBackgroundBrush"] = new(fluent ? "Fluent.ApplicationBackgroundBrush" : "SystemColors.WindowBrush"),
            ["WpfControlBackgroundBrush"] = new(fluent ? "Fluent.ControlFillColorDefaultBrush" : "SystemColors.ControlBrush"),
            ["WpfControlForegroundBrush"] = new(fluent ? "Fluent.TextFillColorPrimaryBrush" : "SystemColors.ControlTextBrush"),
            ["WpfAccentBrush"] = new(accented ? "SystemColors.AccentColorBrush" : "SystemColors.HighlightBrush"),
            ["WpfBorderBrush"] = new(fluent ? "Fluent.ControlStrokeColorDefaultBrush" : "SystemColors.ControlDarkBrush"),
            ["WpfHighlightBrush"] = new("SystemColors.HighlightBrush"),
            ["WpfHighlightTextBrush"] = new("SystemColors.HighlightTextBrush"),
            ["WpfGrayTextBrush"] = new(fluent ? "Fluent.TextFillColorDisabledBrush" : "SystemColors.GrayTextBrush"),
        };
    }
}
