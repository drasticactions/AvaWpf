using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWpf.Chrome;

/// <summary>
/// Looks up chrome tokens (<c>&lt;Family&gt;.Chrome.&lt;name&gt;</c>) for the family in effect, so the Dark variant
/// and overrides apply to the code-drawn chrome.
/// </summary>
public static class ChromeResources
{
    /// <summary>The resource key that names the <see cref="ThemeFamily"/> in effect.</summary>
    public const string FamilyKey = "AvaWpf.Family";

    /// <summary>The resource key that names the color scheme in effect.</summary>
    public const string SchemeKey = "AvaWpf.ColorScheme";

    /// <summary>The family in effect at <paramref name="visual"/>, or <see cref="ThemeFamily.Aero2"/> outside any theme.</summary>
    public static ThemeFamily Family(StyledElement visual) =>
        visual.TryFindResource(FamilyKey, visual.ActualThemeVariant, out var v) && v is ThemeFamily f ? f : ThemeFamily.Aero2;

    /// <summary>The full key of a chrome token in the family in effect.</summary>
    public static string Key(StyledElement visual, string name) => Family(visual) + ".Chrome." + name;

    /// <summary>Resolves a chrome brush token, or null when the family does not define it.</summary>
    public static IBrush? Brush(StyledElement visual, string name) =>
        visual.TryFindResource(Key(visual, name), visual.ActualThemeVariant, out var v) ? v switch
        {
            IBrush b => b,
            Color c => new SolidColorBrush(c),
            _ => null,
        } : null;

    /// <summary>Resolves a chrome color token; a solid brush token gives its color.</summary>
    public static Color? Color(StyledElement visual, string name) =>
        visual.TryFindResource(Key(visual, name), visual.ActualThemeVariant, out var v) ? v switch
        {
            Color c => c,
            ISolidColorBrush b => b.Color,
            _ => null,
        } : null;

    /// <summary>Resolves any resource by its full key in the theme in effect at <paramref name="visual"/>.</summary>
    public static object? Resource(StyledElement visual, string key) =>
        visual.TryFindResource(key, visual.ActualThemeVariant, out var v) ? v : null;

    /// <summary>A pen of <paramref name="thickness"/> with a chrome brush token, or null.</summary>
    public static IPen? Pen(StyledElement visual, string name, double thickness = 1) =>
        Brush(visual, name) is { } b ? new Pen(b, thickness) : null;
}
