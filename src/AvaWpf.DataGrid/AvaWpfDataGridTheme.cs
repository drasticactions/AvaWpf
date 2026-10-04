using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace AvaWpf.DataGrid;

/// <summary>Adds the AvaWpf look of <c>Avalonia.Controls.DataGrid</c> in every theme family.</summary>
/// <remarks>
/// <code>
/// &lt;Application.Styles&gt;
///   &lt;wpf:AvaWpfTheme Theme="Luna" /&gt;
///   &lt;wpf:AvaWpfDataGridTheme /&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// Add the <see cref="AvaWpfTheme"/> first; the per-family ControlThemes register with it, so each family switch and
/// <see cref="ThemeScope"/> uses its own family's look.
/// </remarks>
public class AvaWpfDataGridTheme : Styles
{
    /// <summary>The add-on id under which the ControlThemes are registered with <see cref="AvaWpfTheme"/>.</summary>
    internal const string AddonId = "AvaWpf.DataGrid";

    static AvaWpfDataGridTheme() => DataGridState.EnsureRegistered();

    /// <summary>Initializes a new instance of the <see cref="AvaWpfDataGridTheme"/> class.</summary>
    /// <param name="sp">The service provider of the parent.</param>
    public AvaWpfDataGridTheme(IServiceProvider? sp = null)
    {
        _ = sp;

        // WPF DataGridTextColumn.DefaultEditingElementStyle. A plain style, because a ControlTheme cannot select a cell's
        // content.
        Add(new Style(x => x.OfType<DataGridCell>().Child().OfType<TextBox>())
        {
            Setters =
            {
                new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
            },
        });
        OwnerChanged += OnOwnerChanged;
    }

    /// <summary>Creates the DataGrid ControlThemes of one family.</summary>
    /// <param name="family">The family.</param>
    /// <param name="scheme">The color scheme; the templates are the same for every scheme of a family.</param>
    internal static IResourceProvider CreateFamilyControls(ThemeFamily family, string scheme)
    {
        _ = scheme;
        return family switch
        {
            ThemeFamily.Aero2 => new Themes.Aero2.FamilyControls(),
            ThemeFamily.AeroLite => new Themes.AeroLite.FamilyControls(),
            ThemeFamily.Aero => new Themes.Aero.FamilyControls(),
            ThemeFamily.Luna => new Themes.Luna.FamilyControls(),
            ThemeFamily.Royale => new Themes.Royale.FamilyControls(),
            ThemeFamily.Classic => new Themes.Classic.FamilyControls(),
            _ => new Themes.Fluent.FamilyControls(),
        };
    }

    private void OnOwnerChanged(object? sender, EventArgs e)
    {
        if (Owner is null)
        {
            return;
        }

        var theme = FindIn(Owner as Application) ?? AvaWpfTheme.Find() ?? throw new InvalidOperationException(
            "AvaWpfDataGridTheme needs an AvaWpfTheme in Application.Styles. Add <wpf:AvaWpfTheme /> before <wpf:AvaWpfDataGridTheme />.");
        theme.RegisterAddon(AddonId, CreateFamilyControls);
    }

    // The owner application may not be Application.Current yet (styles added in the application's constructor).
    private static AvaWpfTheme? FindIn(Application? app)
    {
        if (app is null)
        {
            return null;
        }

        foreach (var style in app.Styles)
        {
            if (style is AvaWpfTheme theme)
            {
                return theme;
            }
        }

        return null;
    }
}
