using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace AvaWpf;

/// <summary>
/// Shows its child in another theme family, scheme or variant (<see cref="ThemeVariantScope.RequestedThemeVariant"/>)
/// than the rest of the window. Scopes nest; the nearest one wins.
/// </summary>
/// <remarks>
/// The scope holds its own copy of the family's resources. An <see cref="AvaWpfTheme"/> must be in the application
/// styles.
/// </remarks>
public class ThemeScope : ThemeVariantScope
{
    /// <summary>Defines the <see cref="Theme"/> property.</summary>
    public static new readonly StyledProperty<ThemeFamily> ThemeProperty =
        AvaloniaProperty.Register<ThemeScope, ThemeFamily>(nameof(Theme), ThemeFamily.Aero2);

    /// <summary>Defines the <see cref="ColorScheme"/> property.</summary>
    public static readonly StyledProperty<string?> ColorSchemeProperty =
        AvaloniaProperty.Register<ThemeScope, string?>(nameof(ColorScheme));

    /// <summary>Defines the <see cref="AccentColor"/> property.</summary>
    public static readonly StyledProperty<Color?> AccentColorProperty =
        AvaloniaProperty.Register<ThemeScope, Color?>(nameof(AccentColor));

    private ThemeResources? _resources;

    /// <summary>The theme family of the child. Default <see cref="ThemeFamily.Aero2"/>.</summary>
    public new ThemeFamily Theme
    {
        get => GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    /// <summary>The color scheme of the child (see <see cref="ColorSchemes"/>); null is the family default.</summary>
    public string? ColorScheme
    {
        get => GetValue(ColorSchemeProperty);
        set => SetValue(ColorSchemeProperty, value);
    }

    /// <summary>The accent of the child; null is the family default.</summary>
    public Color? AccentColor
    {
        get => GetValue(AccentColorProperty);
        set => SetValue(AccentColorProperty, value);
    }

    /// <summary>The family in effect in the scope.</summary>
    public ThemeFamily ActualTheme => _resources?.Family ?? Theme;

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ThemeVariantScope);

    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        Build();
        base.OnAttachedToLogicalTree(e);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ThemeProperty || change.Property == ColorSchemeProperty)
        {
            if (_resources is null)
            {
                return;
            }

            if (_resources.Family != Theme)
            {
                // The child is detached while the resources change, so the change never walks the discarded tree.
                ThemeReattach.ReattachChild(this, Build);
            }
            else
            {
                Build();
            }
        }
        else if (change.Property == AccentColorProperty && _resources is not null)
        {
            _resources.Accent.Override = AccentColor;
        }
    }

    private void Build()
    {
        var scheme = ColorSchemes.Resolve(Theme, ColorScheme);
        if (_resources is { } current && current.Family == Theme && current.Scheme == scheme)
        {
            return;
        }

        var theme = AvaWpfTheme.Find()
            ?? throw new InvalidOperationException("ThemeScope needs an AvaWpfTheme in Application.Styles.");
        var next = theme.CreateResources(Theme, scheme, AccentColor);
        if (_resources is { } old)
        {
            Resources.MergedDictionaries.Remove(old);
        }

        _resources = next;
        Resources.MergedDictionaries.Add(next);
    }
}
