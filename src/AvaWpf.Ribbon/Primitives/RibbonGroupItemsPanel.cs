// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/Primitives/RibbonGroupItemsPanel.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.Ribbon.Primitives;

/// <summary>
/// The items panel of a <see cref="RibbonGroup"/>: fills columns top to bottom and starts a new column when the next
/// control does not fit the height.
/// </summary>
public class RibbonGroupItemsPanel : Panel
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        double desiredWidth = 0, desiredHeight = 0, columnWidth = 0, columnHeight = 0;
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            var size = child.DesiredSize;
            if (columnHeight + size.Height > availableSize.Height + 0.01)
            {
                desiredHeight = Math.Min(Math.Max(desiredHeight, columnHeight), availableSize.Height);
                desiredWidth += columnWidth;
                columnHeight = size.Height;
                columnWidth = size.Width;
            }
            else
            {
                columnHeight += size.Height;
                columnWidth = Math.Max(columnWidth, size.Width);
            }
        }

        desiredWidth += columnWidth;
        desiredHeight = Math.Min(Math.Max(desiredHeight, columnHeight), availableSize.Height);
        return new Size(desiredWidth, desiredHeight);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var remaining = finalSize.Height;
        double columnWidth = 0, x = 0;
        foreach (var child in Children)
        {
            var size = child.DesiredSize;
            if (size.Height > remaining + 0.01)
            {
                x += columnWidth;
                columnWidth = size.Width;
                child.Arrange(new Rect(x, 0, columnWidth, size.Height));
                remaining = Math.Max(0, finalSize.Height - size.Height);
            }
            else
            {
                columnWidth = Math.Max(columnWidth, size.Width);
                child.Arrange(new Rect(x, finalSize.Height - remaining, size.Width, size.Height));
                remaining -= size.Height;
            }
        }

        return finalSize;
    }
}
