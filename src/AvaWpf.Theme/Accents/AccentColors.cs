// The HSL ramp is a port of Avalonia.Themes.Fluent/Accents/SystemAccentColors.cs (MIT, see NOTICE.md).
using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace AvaWpf;

/// <summary>
/// Serves the accent ramp under the Avalonia <c>SystemAccentColor*</c> and WPF <c>SystemColors.AccentColor*</c> keys,
/// from <see cref="AvaWpfTheme.AccentColor"/> or the family default.
/// </summary>
internal sealed class AccentColors : ResourceProvider
{
    /// <summary>The Windows 10 default accent.</summary>
    public static readonly Color Windows10Accent = Color.FromRgb(0x00, 0x78, 0xD7);

    /// <summary>The Windows 11 default accent.</summary>
    public static readonly Color Windows11Accent = Color.FromRgb(0x00, 0x78, 0xD4);

    private readonly ThemeFamily _family;
    private Color? _override;
    private AccentRamp? _ramp;
    private readonly IImmutableSolidColorBrush[] _brushes = new IImmutableSolidColorBrush[7];

    public AccentColors(ThemeFamily family, Color? accent)
    {
        _family = family;
        _override = accent;
    }

    /// <summary>The family default accent.</summary>
    public static Color DefaultFor(ThemeFamily family) => family == ThemeFamily.Fluent ? Windows11Accent : Windows10Accent;

    public Color? Override
    {
        get => _override;
        set
        {
            if (_override != value)
            {
                _override = value;
                _ramp = null;
                RaiseResourcesChanged();
            }
        }
    }

    public AccentRamp Ramp => _ramp ??= Compute(_override ?? DefaultFor(_family));

    public override bool HasResources => true;

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        value = null;
        if (key is not string s)
        {
            return false;
        }

        var brush = false;
        ReadOnlySpan<char> name;
        if (s.StartsWith("SystemAccentColor", StringComparison.Ordinal))
        {
            name = s.AsSpan("SystemAccentColor".Length);
        }
        else if (s.StartsWith("SystemColors.AccentColor", StringComparison.Ordinal))
        {
            name = s.AsSpan("SystemColors.AccentColor".Length);
            if (name.EndsWith("Brush", StringComparison.Ordinal))
            {
                brush = true;
                name = name[..^5];
            }
        }
        else
        {
            return false;
        }

        var index = name switch
        {
            "" => 0,
            "Light1" => 1,
            "Light2" => 2,
            "Light3" => 3,
            "Dark1" => 4,
            "Dark2" => 5,
            "Dark3" => 6,
            _ => -1,
        };
        if (index < 0)
        {
            return false;
        }

        var r = Ramp;
        var color = index switch
        {
            0 => r.Accent,
            1 => r.Light1,
            2 => r.Light2,
            3 => r.Light3,
            4 => r.Dark1,
            5 => r.Dark2,
            _ => r.Dark3,
        };
        if (brush)
        {
            if (_brushes[index] is not { } b || b.Color != color)
            {
                _brushes[index] = b = new ImmutableSolidColorBrush(color);
            }

            value = b;
        }
        else
        {
            value = color;
        }

        return true;
    }

    /// <summary>The accent ramp: fixed HSL lightness steps on either side of the accent, as Avalonia Fluent computes it.</summary>
    public static AccentRamp Compute(Color accent)
    {
        const double dark1step = 28.5 / 255d;
        const double dark2step = 49 / 255d;
        const double dark3step = 74.5 / 255d;
        const double light1step = 39 / 255d;
        const double light2step = 70 / 255d;
        const double light3step = 103 / 255d;

        var hsl = accent.ToHsl();
        Color L(double delta) => new HslColor(hsl.A, hsl.H, hsl.S, Math.Clamp(hsl.L + delta, 0, 1)).ToRgb();
        return new AccentRamp(accent, L(light1step), L(light2step), L(light3step), L(-dark1step), L(-dark2step), L(-dark3step));
    }
}
