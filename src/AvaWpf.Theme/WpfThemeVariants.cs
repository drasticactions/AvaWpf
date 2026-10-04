using Avalonia.Styling;

namespace AvaWpf;

/// <summary>The theme variants AvaWpf adds to <see cref="ThemeVariant.Light"/> and <see cref="ThemeVariant.Dark"/>.</summary>
public static class WpfThemeVariants
{
    /// <summary>
    /// High contrast, inheriting <see cref="ThemeVariant.Dark"/>. Families other than Fluent render as Classic with a
    /// high-contrast scheme, as WPF does.
    /// </summary>
    public static ThemeVariant HighContrast { get; } = new("HighContrast", ThemeVariant.Dark);

    /// <summary>True when <paramref name="variant"/> is <see cref="HighContrast"/> or inherits it.</summary>
    public static bool IsHighContrast(ThemeVariant? variant)
    {
        for (var v = variant; v is not null; v = v.InheritVariant)
        {
            if (v == HighContrast)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when <paramref name="variant"/> is <see cref="ThemeVariant.Dark"/> or inherits it.</summary>
    public static bool IsDark(ThemeVariant? variant)
    {
        for (var v = variant; v is not null; v = v.InheritVariant)
        {
            if (v == ThemeVariant.Dark)
            {
                return true;
            }
        }

        return false;
    }
}
