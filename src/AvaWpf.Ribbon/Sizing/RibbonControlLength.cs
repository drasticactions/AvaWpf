// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonControlLength.cs (MIT, see NOTICE.md).
using System;
using System.Globalization;

namespace AvaWpf.Ribbon;

/// <summary>
/// A width in a <see cref="RibbonControlSizeDefinition"/>: <c>Auto</c>, pixels (<c>"40"</c>, <c>"40px"</c>), items
/// (<c>"3items"</c>) or a star weight (<c>"*"</c>, <c>"2*"</c>), like <c>GridLength</c>.
/// </summary>
public readonly struct RibbonControlLength : IEquatable<RibbonControlLength>
{
    private readonly double _value;

    /// <summary>Initializes a length in pixels.</summary>
    /// <param name="pixels">The width in device-independent pixels.</param>
    public RibbonControlLength(double pixels)
        : this(pixels, RibbonControlLengthUnitType.Pixel)
    {
    }

    /// <summary>Initializes a length of a given unit.</summary>
    /// <param name="value">The value.</param>
    /// <param name="type">The unit.</param>
    public RibbonControlLength(double value, RibbonControlLengthUnitType type)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentException("The value cannot be NaN.", nameof(value));
        }

        if (type == RibbonControlLengthUnitType.Star && double.IsInfinity(value))
        {
            throw new ArgumentException("A star length cannot be infinite.", nameof(value));
        }

        _value = type == RibbonControlLengthUnitType.Auto ? 0 : value;
        RibbonControlLengthUnitType = type;
    }

    /// <summary>The automatic length (the size of the content).</summary>
    public static RibbonControlLength Auto { get; } = new(1, RibbonControlLengthUnitType.Auto);

    /// <summary>True for a pixel or item length.</summary>
    public bool IsAbsolute => RibbonControlLengthUnitType is RibbonControlLengthUnitType.Pixel or RibbonControlLengthUnitType.Item;

    /// <summary>True for <see cref="Auto"/>.</summary>
    public bool IsAuto => RibbonControlLengthUnitType == RibbonControlLengthUnitType.Auto;

    /// <summary>True for a star length.</summary>
    public bool IsStar => RibbonControlLengthUnitType == RibbonControlLengthUnitType.Star;

    /// <summary>The value; 1 for <see cref="Auto"/>.</summary>
    public double Value => IsAuto ? 1 : _value;

    /// <summary>The unit.</summary>
    public RibbonControlLengthUnitType RibbonControlLengthUnitType { get; }

    /// <summary>Parses <c>"Auto"</c>, <c>"40"</c>, <c>"40px"</c>, <c>"3items"</c>, <c>"*"</c> or <c>"2*"</c>.</summary>
    /// <param name="s">The text.</param>
    /// <returns>The length.</returns>
    public static RibbonControlLength Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        var text = s.Trim().ToLowerInvariant();
        if (text == "auto")
        {
            return Auto;
        }

        if (text.EndsWith('*'))
        {
            var number = text[..^1];
            return new RibbonControlLength(number.Length == 0 ? 1 : double.Parse(number, CultureInfo.InvariantCulture), RibbonControlLengthUnitType.Star);
        }

        if (text.EndsWith("items", StringComparison.Ordinal))
        {
            return new RibbonControlLength(double.Parse(text[..^5], CultureInfo.InvariantCulture), RibbonControlLengthUnitType.Item);
        }

        if (text.EndsWith("px", StringComparison.Ordinal))
        {
            text = text[..^2];
        }

        return new RibbonControlLength(double.Parse(text, CultureInfo.InvariantCulture));
    }

    /// <summary>Compares two lengths.</summary>
    /// <param name="left">The first length.</param>
    /// <param name="right">The second length.</param>
    /// <returns>True when unit and value are equal.</returns>
    public static bool operator ==(RibbonControlLength left, RibbonControlLength right) => left.Equals(right);

    /// <summary>Compares two lengths.</summary>
    /// <param name="left">The first length.</param>
    /// <param name="right">The second length.</param>
    /// <returns>True when unit or value differ.</returns>
    public static bool operator !=(RibbonControlLength left, RibbonControlLength right) => !left.Equals(right);

    /// <inheritdoc/>
    public bool Equals(RibbonControlLength other) => RibbonControlLengthUnitType == other.RibbonControlLengthUnitType && Value.Equals(other.Value);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RibbonControlLength other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Value, RibbonControlLengthUnitType);

    /// <inheritdoc/>
    public override string ToString() => RibbonControlLengthUnitType switch
    {
        RibbonControlLengthUnitType.Auto => "Auto",
        RibbonControlLengthUnitType.Star => Value == 1 ? "*" : Value.ToString(CultureInfo.InvariantCulture) + "*",
        RibbonControlLengthUnitType.Item => Value.ToString(CultureInfo.InvariantCulture) + "items",
        _ => Value.ToString(CultureInfo.InvariantCulture),
    };
}
