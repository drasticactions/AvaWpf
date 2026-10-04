// Ported from WPF $W/PresentationFramework/System/Windows/Controls/MenuScrollingVisibilityConverter.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;

namespace AvaWpf.Converters;

/// <summary>
/// Shows a menu scroll button while the menu scrolls and the offset is not at the button's end of the range.
/// </summary>
/// <remarks>
/// The parameter is the button's end of the range in percent: 0 for the up button, 100 for the down button.
/// </remarks>
public sealed class MenuScrollingVisibilityConverter : IMultiValueConverter
{
    /// <summary>The shared instance.</summary>
    public static readonly MenuScrollingVisibilityConverter Instance = new();

    /// <summary>Decides whether the scroll button is visible.</summary>
    /// <param name="values">Scroll bar visibility, vertical offset, extent height, viewport height.</param>
    /// <param name="targetType">The target type (unused).</param>
    /// <param name="parameter">The end of the range in percent, a <see cref="double"/> or a string.</param>
    /// <param name="culture">The culture (unused; the parameter is parsed with the invariant culture).</param>
    /// <returns><see langword="true"/> when the button is shown.</returns>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is null ||
            values.Count != 4 ||
            values[1] is not double verticalOffset ||
            values[2] is not double extentHeight ||
            values[3] is not double viewportHeight)
        {
            return AvaloniaProperty.UnsetValue;
        }

        double target;
        if (parameter is double d)
        {
            target = d;
        }
        else if (parameter is string s && double.TryParse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out var parsed))
        {
            target = parsed;
        }
        else
        {
            return AvaloniaProperty.UnsetValue;
        }

        // Avalonia exposes only the requested visibility, so Auto is computed here: visible when the content does not fit.
        bool visible;
        switch (values[0])
        {
            case bool computed:
                visible = computed;
                break;
            case ScrollBarVisibility.Visible:
                visible = true;
                break;
            case ScrollBarVisibility.Auto:
                visible = extentHeight > viewportHeight && !AreClose(extentHeight, viewportHeight);
                break;
            case ScrollBarVisibility:
                visible = false;
                break;
            default:
                return AvaloniaProperty.UnsetValue;
        }

        if (!visible)
        {
            return false;
        }

        if (extentHeight != viewportHeight)
        {
            // The check above avoids dividing by 0.
            var percent = Math.Min(100.0, Math.Max(0.0, verticalOffset * 100.0 / (extentHeight - viewportHeight)));
            if (AreClose(percent, target))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>WPF's <c>DoubleUtil.AreClose</c>: equal within a relative epsilon.</summary>
    private static bool AreClose(double value1, double value2)
    {
        if (value1 == value2)
        {
            return true;
        }

        var eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * 2.2204460492503131e-016;
        var delta = value1 - value2;
        return -eps < delta && eps > delta;
    }
}
