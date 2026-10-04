using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using AvaWpf.Animations;

namespace AvaWpf;

/// <summary>
/// Adds the AvaWpf theme to an application: every stock Avalonia control in one of the WPF theme families.
/// </summary>
/// <remarks>
/// <code>
/// &lt;Application.Styles&gt;
///   &lt;wpf:AvaWpfTheme Theme="Luna" ColorScheme="Metallic" /&gt;
///   &lt;wpf:AvaWpfControlsTheme /&gt;   &lt;!-- from AvaWpf.Controls, optional --&gt;
/// &lt;/Application.Styles&gt;
/// </code>
/// Call <c>WithAvaWpfFonts()</c> on the <c>AppBuilder</c> so the substitute fonts resolve.
/// Changing <see cref="Theme"/> at run time re-templates every open window.
/// </remarks>
public class AvaWpfTheme : Styles
{
    /// <summary>Defines the <see cref="Theme"/> property.</summary>
    public static readonly DirectProperty<AvaWpfTheme, ThemeFamily> ThemeProperty =
        AvaloniaProperty.RegisterDirect<AvaWpfTheme, ThemeFamily>(nameof(Theme), o => o.Theme, (o, v) => o.Theme = v);

    /// <summary>Defines the <see cref="ColorScheme"/> property.</summary>
    public static readonly DirectProperty<AvaWpfTheme, string?> ColorSchemeProperty =
        AvaloniaProperty.RegisterDirect<AvaWpfTheme, string?>(nameof(ColorScheme), o => o.ColorScheme, (o, v) => o.ColorScheme = v);

    /// <summary>Defines the <see cref="AccentColor"/> property.</summary>
    public static readonly DirectProperty<AvaWpfTheme, Color?> AccentColorProperty =
        AvaloniaProperty.RegisterDirect<AvaWpfTheme, Color?>(nameof(AccentColor), o => o.AccentColor, (o, v) => o.AccentColor = v);

    /// <summary>Defines the <see cref="FollowHighContrast"/> property.</summary>
    public static readonly DirectProperty<AvaWpfTheme, bool> FollowHighContrastProperty =
        AvaloniaProperty.RegisterDirect<AvaWpfTheme, bool>(nameof(FollowHighContrast), o => o.FollowHighContrast, (o, v) => o.FollowHighContrast = v);

    /// <summary>Defines the <see cref="MenuPopupAnimation"/> property.</summary>
    public static readonly DirectProperty<AvaWpfTheme, PopupAnimationKind?> MenuPopupAnimationProperty =
        AvaloniaProperty.RegisterDirect<AvaWpfTheme, PopupAnimationKind?>(nameof(MenuPopupAnimation), o => o.MenuPopupAnimation, (o, v) => o.MenuPopupAnimation = v);

    /// <summary>Defines the <see cref="ComboBoxPopupAnimation"/> property.</summary>
    public static readonly DirectProperty<AvaWpfTheme, PopupAnimationKind?> ComboBoxPopupAnimationProperty =
        AvaloniaProperty.RegisterDirect<AvaWpfTheme, PopupAnimationKind?>(nameof(ComboBoxPopupAnimation), o => o.ComboBoxPopupAnimation, (o, v) => o.ComboBoxPopupAnimation = v);

    private readonly Dictionary<string, Func<ThemeFamily, string, IResourceProvider?>> _addons = new(StringComparer.Ordinal);
    private ThemeFamily _theme = ThemeFamily.Aero2;
    private string? _colorScheme;
    private Color? _accentColor;
    private bool _followHighContrast = true;
    private PopupAnimationKind? _menuPopupAnimation;
    private PopupAnimationKind? _comboBoxPopupAnimation;
    private bool _highContrast;
    private bool _highContrastLight;
    private ThemeVariant? _variantBeforeHighContrast;
    private IPlatformSettings? _platformSettings;
    private ThemeResources _resources;
    private bool _initializing = true;

    /// <summary>Initializes a new instance of the <see cref="AvaWpfTheme"/> class.</summary>
    /// <param name="sp">The service provider of the parent.</param>
    public AvaWpfTheme(IServiceProvider? sp = null)
    {
        _ = sp;
        SystemColorOverrides = new SystemColorOverrides();
        _resources = Build();
        SystemColorOverrides.Changed += (_, _) => _resources.System.Overrides = SystemColorOverrides;
        Resources.MergedDictionaries.Add(_resources);
        Add(new Themes.GlobalStyles());
        AvaloniaWorkarounds.EnsureRegistered();
        TextLineRounding.EnsureRegistered();
        ButtonState.EnsureRegistered();
        SeparatorState.EnsureRegistered();
        SliderState.EnsureRegistered();
        ButtonFlyoutState.EnsureRegistered();
        ThemeReattach.EnsureTracking();
        OwnerChanged += OnOwnerChanged;
        _initializing = false;
    }

    /// <summary>Raised after the family or scheme in effect changed and the windows were re-templated.</summary>
    public event EventHandler? ThemeChanged;

    /// <summary>The theme family. Default <see cref="ThemeFamily.Aero2"/>.</summary>
    public ThemeFamily Theme
    {
        get => _theme;
        set => SetAndRaise(ThemeProperty, ref _theme, value);
    }

    /// <summary>The color scheme of the family (see <see cref="ColorSchemes"/>), or null for the family default; an unknown scheme throws when applied.</summary>
    public string? ColorScheme
    {
        get => _colorScheme;
        set => SetAndRaise(ColorSchemeProperty, ref _colorScheme, value);
    }

    /// <summary>The accent color, or null for the family default (#0078D7 for Aero2 and AeroLite, #0078D4 for Fluent).</summary>
    public Color? AccentColor
    {
        get => _accentColor;
        set => SetAndRaise(AccentColorProperty, ref _accentColor, value);
    }

    /// <summary>
    /// Whether the platform high-contrast setting applies the <see cref="WpfThemeVariants.HighContrast"/> variant.
    /// As in WPF, every family except Fluent then renders as Classic high contrast.
    /// </summary>
    public bool FollowHighContrast
    {
        get => _followHighContrast;
        set => SetAndRaise(FollowHighContrastProperty, ref _followHighContrast, value);
    }

    /// <summary>The menu popup animation, or null for the family default.</summary>
    public PopupAnimationKind? MenuPopupAnimation
    {
        get => _menuPopupAnimation;
        set => SetAndRaise(MenuPopupAnimationProperty, ref _menuPopupAnimation, value);
    }

    /// <summary>The ComboBox popup animation, or null for the family default.</summary>
    public PopupAnimationKind? ComboBoxPopupAnimation
    {
        get => _comboBoxPopupAnimation;
        set => SetAndRaise(ComboBoxPopupAnimationProperty, ref _comboBoxPopupAnimation, value);
    }

    /// <summary>SystemColors roles replaced in every family and variant, such as <c>["Highlight"] = Colors.Red</c>.</summary>
    public SystemColorOverrides SystemColorOverrides { get; }

    /// <summary>The family in effect: <see cref="Theme"/>, or Classic while high contrast forces it.</summary>
    public ThemeFamily ActualTheme => _resources.Family;

    /// <summary>The scheme in effect.</summary>
    public string ActualColorScheme => _resources.Scheme;

    /// <summary>The accent ramp in effect.</summary>
    public AccentRamp AccentRamp => _resources.Accent.Ramp;

    /// <summary>
    /// Registers the per-family ControlThemes of an add-on package. The factory runs for every family change and every
    /// <see cref="ThemeScope"/>.
    /// </summary>
    internal void RegisterAddon(string id, Func<ThemeFamily, string, IResourceProvider?> factory)
    {
        if (_addons.ContainsKey(id))
        {
            return;
        }

        _addons[id] = factory;
        if (factory(_resources.Family, _resources.Scheme) is { } provider)
        {
            _resources.AddAddon(provider);
        }
    }

    internal IEnumerable<Func<ThemeFamily, string, IResourceProvider?>> Addons => _addons.Values;

    /// <summary>Finds the <see cref="AvaWpfTheme"/> of the current application, or null.</summary>
    public static AvaWpfTheme? Find()
    {
        if (Application.Current is not { } app)
        {
            return null;
        }

        foreach (var s in app.Styles)
        {
            if (s is AvaWpfTheme t)
            {
                return t;
            }
        }

        return null;
    }

    /// <summary>Builds the resources of one family and scheme, with every registered add-on.</summary>
    internal ThemeResources CreateResources(ThemeFamily family, string scheme, Color? accent)
    {
        var r = new ThemeResources(family, scheme, accent)
        {
            System =
            {
                Overrides = SystemColorOverrides,
                MenuPopupAnimation = _menuPopupAnimation,
                ComboBoxPopupAnimation = _comboBoxPopupAnimation,
            },
        };
        foreach (var factory in _addons.Values)
        {
            if (factory(family, scheme) is { } provider)
            {
                r.AddAddon(provider);
            }
        }

        return r;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_initializing)
        {
            return;
        }

        if (change.Property == ThemeProperty || change.Property == ColorSchemeProperty || change.Property == FollowHighContrastProperty)
        {
            Apply();
        }
        else if (change.Property == AccentColorProperty)
        {
            _resources.Accent.Override = _accentColor;
        }
        else if (change.Property == MenuPopupAnimationProperty || change.Property == ComboBoxPopupAnimationProperty)
        {
            _resources.System.MenuPopupAnimation = _menuPopupAnimation;
            _resources.System.ComboBoxPopupAnimation = _comboBoxPopupAnimation;
            _resources.System.Invalidate();
        }
    }

    private (ThemeFamily Family, string Scheme) Effective()
    {
        if (_followHighContrast && _highContrast && _theme != ThemeFamily.Fluent)
        {
            return (ThemeFamily.Classic, _highContrastLight ? ColorSchemes.HighContrastWhite : ColorSchemes.HighContrastBlack);
        }

        return (_theme, ColorSchemes.Resolve(_theme, _colorScheme));
    }

    private ThemeResources Build()
    {
        var (family, scheme) = Effective();
        return CreateResources(family, scheme, _accentColor);
    }

    private void Apply()
    {
        var (family, scheme) = Effective();
        if (family == _resources.Family && scheme == _resources.Scheme)
        {
            return;
        }

        var familyChanged = family != _resources.Family;
        var next = CreateResources(family, scheme, _accentColor);
        void Swap()
        {
            var index = Resources.MergedDictionaries.IndexOf(_resources);
            _resources = next;
            Resources.MergedDictionaries[index] = next;
        }

        // Schemes of one family share templates, so a scheme change only swaps resources.
        if (familyChanged && Application.Current is { } app)
        {
            ThemeReattach.ReattachApplication(app, Swap);
        }
        else
        {
            Swap();
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnOwnerChanged(object? sender, EventArgs e)
    {
        if (_platformSettings is { } old)
        {
            old.ColorValuesChanged -= OnColorValuesChanged;
            _platformSettings = null;
        }

        if (Owner is Application app && app.PlatformSettings is { } ps)
        {
            _platformSettings = ps;
            ps.ColorValuesChanged += OnColorValuesChanged;
            OnColorValuesChanged(ps, ps.GetColorValues());
        }
    }

    private void OnColorValuesChanged(object? sender, PlatformColorValues values) => ApplyColorValues(values);

    /// <summary>Applies the platform's contrast preference.</summary>
    internal void ApplyColorValues(PlatformColorValues values)
    {
        var hc = values.ContrastPreference == ColorContrastPreference.High;
        _highContrastLight = values.ThemeVariant == PlatformThemeVariant.Light;
        if (hc == _highContrast)
        {
            return;
        }

        _highContrast = hc;
        if (_followHighContrast && Application.Current is { } app)
        {
            if (hc)
            {
                _variantBeforeHighContrast = app.RequestedThemeVariant;
                app.RequestedThemeVariant = WpfThemeVariants.HighContrast;
            }
            else
            {
                app.RequestedThemeVariant = _variantBeforeHighContrast ?? ThemeVariant.Default;
            }
        }

        Apply();
    }
}
