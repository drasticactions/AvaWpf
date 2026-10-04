// Ported from WPF $W/Themes/PresentationFramework.Fluent/Controls/AnimationFactorToValueConverter.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaWpf.Converters;

/// <summary>
/// Multiplies a measured length by a factor, for Fluent animation values that are a fraction of a size.
/// </summary>
public class AnimationFactorToValueConverter : IMultiValueConverter
{
    /// <summary>A shared instance, for <c>{x:Static}</c> use in templates.</summary>
    public static readonly AnimationFactorToValueConverter Instance = new();

    /// <summary>Returns <c>factor * value</c>, or 0 when either input is not a number.</summary>
    /// <param name="values">The complete value and the factor.</param>
    /// <param name="targetType">The type of the target (unused).</param>
    /// <param name="parameter"><c>"negative"</c> to negate the factor.</param>
    /// <param name="culture">The culture (unused).</param>
    /// <returns>The product, as a <see cref="double"/>.</returns>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not double completeValue)
        {
            return 0.0;
        }

        if (values[1] is not double factor || double.IsNaN(factor))
        {
            return 0.0;
        }

        if (parameter is "negative")
        {
            factor = -factor;
        }

        return factor * completeValue;
    }
}
