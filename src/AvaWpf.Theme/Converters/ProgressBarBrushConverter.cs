// Ported from WPF $W/Themes/Shared/Microsoft/Windows/Themes/ProgressBarBrushConverter.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaWpf.Converters;

/// <summary>
/// Builds the block-style progress indicator brush of the Luna, Royale and Classic families.
/// </summary>
/// <remarks>
/// An indeterminate bar draws blocks over 30 % of the width and scrolls them one block every 100 ms.
/// </remarks>
public sealed class ProgressBarBrushConverter : IMultiValueConverter
{
    /// <summary>The width of one block, in DIPs.</summary>
    private const double BlockWidth = 6.0;

    /// <summary>The gap between blocks, in DIPs.</summary>
    private const double BlockGap = 2.0;

    /// <summary>Creates the progress indicator brush.</summary>
    /// <param name="values">Foreground brush, IsIndeterminate, indicator width, indicator height, track width.</param>
    /// <param name="targetType">The target type (unused).</param>
    /// <param name="parameter">The converter parameter (unused).</param>
    /// <param name="culture">The culture (unused).</param>
    /// <returns>A <see cref="DrawingBrush"/>, or null for invalid input or a non-positive size.</returns>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not { Count: 5 } ||
            values[0] is not IBrush brush ||
            values[1] is not bool isIndeterminate ||
            values[2] is not double width ||
            values[3] is not double height ||
            values[4] is not double trackWidth)
        {
            return null;
        }

        if (width <= 0.0 || double.IsInfinity(width) || double.IsNaN(width) ||
            height <= 0.0 || double.IsInfinity(height) || double.IsNaN(height))
        {
            return null;
        }

        var drawing = new DrawingGroup();
        var viewport = new Rect(0, 0, width, height);
        var marqueeBlocks = 0;

        const double blockTotal = BlockWidth + BlockGap;
        var drawnWidth = 0.0;

        using (var dc = drawing.Open())
        {
            if (isIndeterminate)
            {
                var blocks = (int)Math.Ceiling(width / blockTotal);

                var left = -blocks * blockTotal;

                var indeterminateWidth = width * .3;

                // The brush is wider than the rectangle so the marquee wraps.
                //                +-------------+
                // [] [] [] __ __ |[] [] [] __ _|
                //                +-------------+
                // Translate brush =>>
                viewport = new Rect(left, 0, indeterminateWidth - left, height);

                marqueeBlocks = blocks;

                // Blocks left of the brush that the animation translates into view.
                while (drawnWidth + BlockWidth < indeterminateWidth)
                {
                    dc.DrawRectangle(brush, null, new Rect(left + drawnWidth, 0, BlockWidth, height));
                    drawnWidth += blockTotal;
                }

                width = indeterminateWidth;
                drawnWidth = 0.0;
            }

            while (drawnWidth + BlockWidth < width)
            {
                dc.DrawRectangle(brush, null, new Rect(drawnWidth, 0, BlockWidth, height));
                drawnWidth += blockTotal;
            }

            // A partial last block only when the indicator fills the track.
            var remainder = width - drawnWidth;
            if (!isIndeterminate && remainder > 0.0 && Math.Abs(width - trackWidth) < 1.0e-5)
            {
                dc.DrawRectangle(brush, null, new Rect(drawnWidth, 0, remainder, height));
            }
        }

        // Viewport and viewbox are both the progress region in absolute units, with no stretch and no tiling.
        var rect = new RelativeRect(viewport, RelativeUnit.Absolute);
        var result = new DrawingBrush(drawing)
        {
            SourceRect = rect,
            DestinationRect = rect,
            TileMode = TileMode.None,
            Stretch = Stretch.None,
        };
        if (isIndeterminate)
        {
            ProgressBarMarquee.Start(result, marqueeBlocks, blockTotal);
        }

        return result;
    }
}
