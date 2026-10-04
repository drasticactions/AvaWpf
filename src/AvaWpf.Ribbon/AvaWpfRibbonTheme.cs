using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace AvaWpf.Ribbon;

/// <summary>
/// Adds the Ribbon ControlThemes of every AvaWpf family. Add it after <see cref="AvaWpfTheme"/>:
/// <code>
/// &lt;Application.Styles&gt;
///   &lt;wpf:AvaWpfTheme Theme="Luna" /&gt;
///   &lt;wpf:AvaWpfRibbonTheme /&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// It registers its per-family dictionaries with the <see cref="AvaWpfTheme"/>, so a family switch (and every
/// <see cref="ThemeScope"/>) swaps the Ribbon looks too.
/// </summary>
public class AvaWpfRibbonTheme : Styles
{
    /// <summary>The add-on id registered with <see cref="AvaWpfTheme"/>.</summary>
    internal const string AddonId = "AvaWpf.Ribbon";

    /// <summary>Initializes a new instance of the <see cref="AvaWpfRibbonTheme"/> class.</summary>
    /// <param name="sp">The service provider of the parent.</param>
    public AvaWpfRibbonTheme(IServiceProvider? sp = null)
    {
        _ = sp;
        OwnerChanged += OnOwnerChanged;
    }

    /// <summary>The Ribbon ControlThemes of a family, a fresh instance.</summary>
    /// <param name="family">The family.</param>
    /// <returns>The dictionary.</returns>
    internal static ResourceDictionary LoadControls(ThemeFamily family) => family switch
    {
        ThemeFamily.Aero2 => new Themes.Aero2.FamilyControls(),
        ThemeFamily.AeroLite => new Themes.AeroLite.FamilyControls(),
        ThemeFamily.Aero => new Themes.Aero.FamilyControls(),
        ThemeFamily.Luna => new Themes.Luna.FamilyControls(),
        ThemeFamily.Royale => new Themes.Royale.FamilyControls(),
        ThemeFamily.Classic => new Themes.Classic.FamilyControls(),
        _ => new Themes.Fluent.FamilyControls(),
    };

    private void OnOwnerChanged(object? sender, EventArgs e)
    {
        if (Owner is null)
        {
            return;
        }

        // Application.Current is not set yet while the application constructor adds its styles, so look in the
        // owner's own styles first.
        var theme = FindIn(Owner) ?? AvaWpfTheme.Find() ?? throw new InvalidOperationException(
            "AvaWpfRibbonTheme needs an AvaWpfTheme in Application.Styles, added before it: " +
            "<wpf:AvaWpfTheme /> then <wpf:AvaWpfRibbonTheme />.");
        theme.RegisterAddon(AddonId, (family, _) => LoadControls(family));
    }

    private static AvaWpfTheme? FindIn(IResourceHost? owner)
    {
        var styles = owner switch
        {
            Application app => app.Styles,
            StyledElement element => element.Styles,
            _ => null,
        };
        if (styles is not null)
        {
            foreach (var style in styles)
            {
                if (style is AvaWpfTheme theme)
                {
                    return theme;
                }
            }
        }

        return null;
    }
}
