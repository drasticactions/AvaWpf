using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using AvaWpf.Animations;

namespace AvaWpf;

/// <summary>
/// Serves the WPF <c>SystemColors</c>, <c>SystemParameters</c> and <c>SystemFonts</c> keys (WPF names without
/// <c>Key</c>, such as <c>SystemColors.ControlBrush</c>) for one family and scheme, in every variant.
/// </summary>
internal sealed class SystemResources : ResourceProvider
{
    private const string ColorsPrefix = "SystemColors.";
    private const string ParametersPrefix = "SystemParameters.";
    private const string FontsPrefix = "SystemFonts.";

    private static readonly HashSet<string> s_booleans = new(StringComparer.Ordinal)
    {
        "HighContrast", "ClientAreaAnimation", "DropShadow", "KeyboardCues", "MenuDropAlignment",
    };

    private readonly Dictionary<(string Snapshot, string Role), SolidColorBrush> _brushes = new();
    private readonly Dictionary<ThemeVariant, SystemSnapshot> _snapshots = new();
    private readonly Dictionary<string, (string Role, bool Brush)> _colorKeys = new(StringComparer.Ordinal);
    private SystemSnapshot? _defaultSnapshot;
    private readonly Dictionary<string, FontFamily> _families = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<string, Color> _overrides = new Dictionary<string, Color>();

    public SystemResources(ThemeFamily family, string scheme)
    {
        Family = family;
        Scheme = scheme;
    }

    public ThemeFamily Family { get; }

    public string Scheme { get; }

    /// <summary>Roles replaced in every variant (<see cref="AvaWpfTheme.SystemColorOverrides"/>).</summary>
    public IReadOnlyDictionary<string, Color> Overrides
    {
        get => _overrides;
        set
        {
            _overrides = value;
            _brushes.Clear();
            RaiseResourcesChanged();
        }
    }

    /// <summary>The menu popup animation override from <see cref="AvaWpfTheme"/>.</summary>
    public PopupAnimationKind? MenuPopupAnimation { get; set; }

    /// <summary>The ComboBox popup animation override from <see cref="AvaWpfTheme"/>.</summary>
    public PopupAnimationKind? ComboBoxPopupAnimation { get; set; }

    public override bool HasResources => true;

    /// <summary>Raises a change notification after an override property changed.</summary>
    public void Invalidate()
    {
        _brushes.Clear();
        RaiseResourcesChanged();
    }

    /// <summary>The snapshot in effect for <paramref name="variant"/>, cached per variant.</summary>
    public SystemSnapshot SnapshotFor(ThemeVariant? variant)
    {
        if (variant is null)
        {
            return _defaultSnapshot ??= Snapshots.All[SnapshotName(Family, Scheme, null)];
        }

        if (!_snapshots.TryGetValue(variant, out var snapshot))
        {
            _snapshots[variant] = snapshot = Snapshots.All[SnapshotName(Family, Scheme, variant)];
        }

        return snapshot;
    }

    /// <summary>The snapshot name for a family, scheme and variant.</summary>
    public static string SnapshotName(ThemeFamily family, string scheme, ThemeVariant? variant)
    {
        var dark = WpfThemeVariants.IsDark(variant);
        var hc = WpfThemeVariants.IsHighContrast(variant);
        switch (family)
        {
            case ThemeFamily.Classic:
                if (dark && !ColorSchemes.IsHighContrast(scheme))
                {
                    return hc ? "Classic.HighContrastBlack" : "Classic.Dark";
                }

                return "Classic." + scheme;
            case ThemeFamily.Fluent:
                return hc ? "Classic.HighContrastBlack" : dark ? "Fluent.Dark" : "Fluent";
            case ThemeFamily.Luna:
                return "Luna." + scheme + (dark ? ".Dark" : string.Empty);
            case ThemeFamily.Royale:
                return "Royale.NormalColor" + (dark ? ".Dark" : string.Empty);
            default:
                return family + (dark ? ".Dark" : string.Empty);
        }
    }

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        value = null;
        if (key is not string s)
        {
            return false;
        }

        if (s.StartsWith(ColorsPrefix, StringComparison.Ordinal))
        {
            return TryGetColor(s, theme, out value);
        }

        if (s.StartsWith(ParametersPrefix, StringComparison.Ordinal))
        {
            return TryGetParameter(s[ParametersPrefix.Length..], theme, out value);
        }

        if (s.StartsWith(FontsPrefix, StringComparison.Ordinal))
        {
            return TryGetFont(s[FontsPrefix.Length..], theme, out value);
        }

        return false;
    }

    /// <summary>The color of a role in <paramref name="variant"/>, overrides applied.</summary>
    public bool TryGetRoleColor(string role, ThemeVariant? variant, out Color color)
    {
        if (_overrides.TryGetValue(role, out color))
        {
            return true;
        }

        var snapshot = SnapshotFor(variant);
        switch (role)
        {
            // WPF: no system color; HighlightBrush under high contrast, otherwise ControlBrush (SystemColors.cs).
            case "InactiveSelectionHighlight":
                return TryGetRoleColor(IsHighContrast(variant) ? "Highlight" : "Control", variant, out color);
            case "InactiveSelectionHighlightText":
                return TryGetRoleColor(IsHighContrast(variant) ? "HighlightText" : "ControlText", variant, out color);
        }

        if (snapshot.Colors.TryGetValue(role, out var argb))
        {
            color = Color.FromUInt32(argb);
            return true;
        }

        return false;
    }

    private bool IsHighContrast(ThemeVariant? variant) => SnapshotFor(variant).Numbers.TryGetValue("HighContrast", out var hc) && hc != 0;

    private bool TryGetColor(string key, ThemeVariant? theme, out object? value)
    {
        value = null;
        if (!_colorKeys.TryGetValue(key, out var parsed))
        {
            // "SystemColors.<Role>Brush" or "SystemColors.<Role>Color"; anything else gets an empty role.
            var name = key.AsSpan(ColorsPrefix.Length);
            parsed = name.EndsWith("Brush", StringComparison.Ordinal) ? (name[..^5].ToString(), true)
                : name.EndsWith("Color", StringComparison.Ordinal) ? (name[..^5].ToString(), false)
                : (string.Empty, false);
            _colorKeys[key] = parsed;
        }

        var (role, brush) = parsed;
        if (role.Length == 0)
        {
            return false;
        }

        if (role.StartsWith("Accent", StringComparison.Ordinal))
        {
            return false; // Served by AccentColors.
        }

        if (!TryGetRoleColor(role, theme, out var color))
        {
            return false;
        }

        if (!brush)
        {
            value = color;
            return true;
        }

        var cacheKey = (SnapshotFor(theme).Name, role);
        if (!_brushes.TryGetValue(cacheKey, out var b) || b.Color != color)
        {
            // Mutable brushes: some properties (TopLevel.SystemBarColor) are typed SolidColorBrush.
            _brushes[cacheKey] = b = new SolidColorBrush(color);
        }

        value = b;
        return true;
    }

    private bool TryGetParameter(string name, ThemeVariant? theme, out object? value)
    {
        var snapshot = SnapshotFor(theme);
        value = null;
        switch (name)
        {
            case "MenuPopupAnimation" when MenuPopupAnimation is { } menu:
                value = menu;
                return true;
            case "ComboBoxPopupAnimation" when ComboBoxPopupAnimation is { } combo:
                value = combo;
                return true;
        }

        if (snapshot.Numbers.TryGetValue(name, out var d))
        {
            value = s_booleans.Contains(name) ? d != 0 : d;
            return true;
        }

        if (snapshot.Strings.TryGetValue(name, out var str))
        {
            value = name.EndsWith("Animation", StringComparison.Ordinal) && Enum.TryParse<PopupAnimationKind>(str, out var kind) ? kind : str;
            return true;
        }

        return false;
    }

    private bool TryGetFont(string name, ThemeVariant? theme, out object? value)
    {
        var snapshot = SnapshotFor(theme);
        value = null;
        if (name.EndsWith("FontFamily", StringComparison.Ordinal) && snapshot.Strings.TryGetValue(name, out var family))
        {
            if (!_families.TryGetValue(family, out var f))
            {
                _families[family] = f = new FontFamily(family);
            }

            value = f;
            return true;
        }

        if (name.EndsWith("FontSize", StringComparison.Ordinal) && snapshot.Numbers.TryGetValue(name, out var size))
        {
            value = size;
            return true;
        }

        if (name.EndsWith("FontWeight", StringComparison.Ordinal) && snapshot.Numbers.TryGetValue(name, out var weight))
        {
            value = (FontWeight)(int)weight;
            return true;
        }

        if (name.EndsWith("FontStyle", StringComparison.Ordinal) && snapshot.Strings.TryGetValue(name, out var style))
        {
            value = Enum.TryParse<FontStyle>(style, out var fs) ? fs : FontStyle.Normal;
            return true;
        }

        return false;
    }
}
