using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace AvaWpf.Controls;

/// <summary>Adds the AvaWpf.Controls ControlThemes to an application, after the <see cref="AvaWpfTheme"/>.</summary>
/// <remarks>
/// <code>
/// &lt;Application.Styles&gt;
///   &lt;wpf:AvaWpfTheme Theme="Luna" /&gt;
///   &lt;wpf:AvaWpfControlsTheme /&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// The themes register with the <see cref="AvaWpfTheme"/>, so each family switch and <see cref="ThemeScope"/> uses the
/// ControlThemes of its own family.
/// </remarks>
public class AvaWpfControlsTheme : Styles
{
    /// <summary>The add-on id under which the ControlThemes are registered.</summary>
    internal const string AddonId = "AvaWpf.Controls";

    /// <summary>Initializes a new instance of the <see cref="AvaWpfControlsTheme"/> class.</summary>
    /// <param name="sp">The service provider of the parent.</param>
    public AvaWpfControlsTheme(IServiceProvider? sp = null)
    {
        _ = sp;
        OwnerChanged += OnOwnerChanged;
    }

    /// <summary>Creates a new instance of the ControlThemes of one family.</summary>
    /// <param name="family">The theme family.</param>
    /// <returns>The family's ControlThemes.</returns>
    internal static IResourceProvider CreateFamilyResources(ThemeFamily family) => family switch
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

        var theme = FindTheme(Owner) ?? throw new InvalidOperationException(
            "AvaWpfControlsTheme needs an AvaWpfTheme. Add <wpf:AvaWpfTheme /> to the same Styles collection " +
            "(normally Application.Styles) before <wpf:AvaWpfControlsTheme />.");
        theme.RegisterAddon(AddonId, (family, _) => CreateFamilyResources(family));
    }

    private static AvaWpfTheme? FindTheme(IResourceHost owner)
    {
        var styles = owner switch
        {
            Application app => app.Styles,
            StyledElement element => element.Styles,
            _ => null,
        };

        if (styles is not null)
        {
            foreach (var s in styles)
            {
                if (s is AvaWpfTheme t)
                {
                    return t;
                }
            }
        }

        return AvaWpfTheme.Find();
    }
}
