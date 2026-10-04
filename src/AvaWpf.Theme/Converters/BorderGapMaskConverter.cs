// Ported from WPF $W/PresentationFramework/System/Windows/Controls/BorderGapMaskConverter.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaWpf.Converters;

/// <summary>
/// Makes the opacity mask that cuts the header gap out of a GroupBox border.
/// </summary>
/// <remarks>
/// The mask is a three-column grid; the middle column, the header wide, covers only the lower half.
/// </remarks>
internal sealed class BorderGapMaskConverter : IMultiValueConverter
{
    /// <summary>Creates the opacity mask brush.</summary>
    /// <param name="values">Header width, border width, border height.</param>
    /// <param name="targetType">The target type (unused).</param>
    /// <param name="parameter">The width of the line left of the header.</param>
    /// <param name="culture">The culture (unused).</param>
    /// <returns>A <see cref="VisualBrush"/>, null for a zero-sized border, or <see cref="AvaloniaProperty.UnsetValue"/> for invalid input.</returns>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is null ||
            values is not { Count: 3 } ||
            values[0] is not double headerWidth ||
            values[1] is not double borderWidth ||
            values[2] is not double borderHeight)
        {
            return AvaloniaProperty.UnsetValue;
        }

        if (parameter is not double && parameter is not string)
        {
            return AvaloniaProperty.UnsetValue;
        }

        if (borderWidth == 0 || borderHeight == 0)
        {
            return null;
        }

        var lineWidth = parameter is string s
            ? double.Parse(s, NumberFormatInfo.InvariantInfo)
            : (double)parameter;

        var grid = new Grid
        {
            Width = borderWidth,
            Height = borderHeight,
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(lineWidth)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(headerWidth)));
        grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(borderHeight / 2)));
        grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));

        var rectColumn1 = new Rectangle { Fill = Brushes.Black };
        var rectColumn2 = new Rectangle { Fill = Brushes.Black };
        var rectColumn3 = new Rectangle { Fill = Brushes.Black };

        Grid.SetRowSpan(rectColumn1, 2);
        Grid.SetRow(rectColumn1, 0);
        Grid.SetColumn(rectColumn1, 0);

        Grid.SetRow(rectColumn2, 1);
        Grid.SetColumn(rectColumn2, 1);

        Grid.SetRowSpan(rectColumn3, 2);
        Grid.SetRow(rectColumn3, 0);
        Grid.SetColumn(rectColumn3, 2);

        grid.Children.Add(rectColumn1);
        grid.Children.Add(rectColumn2);
        grid.Children.Add(rectColumn3);

        return new VisualBrush(grid);
    }
}
