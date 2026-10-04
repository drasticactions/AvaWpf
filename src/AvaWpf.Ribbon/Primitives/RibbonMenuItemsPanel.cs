// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/Primitives/RibbonMenuItemsPanel.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.Ribbon.Primitives;

/// <summary>
/// Stacks the items of a <see cref="RibbonMenuButton"/> drop-down. A <see cref="RibbonGallery"/> gets the height the
/// other items leave, so in a drop-down cut to the screen or to <see cref="RibbonMenuButton.DropDownHeight"/> it
/// scrolls instead of growing past it.
/// </summary>
public class RibbonMenuItemsPanel : Panel
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var fixedHeight = 0.0;
        var galleries = 0;
        foreach (var child in Children)
        {
            if (child is RibbonGallery && child.IsVisible)
            {
                galleries++;
                continue;
            }

            child.Measure(availableSize.WithHeight(double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            fixedHeight += child.DesiredSize.Height;
        }

        if (galleries == 0)
        {
            return new Size(width, fixedHeight);
        }

        var share = double.IsInfinity(availableSize.Height)
            ? double.PositiveInfinity
            : Math.Max(0, availableSize.Height - fixedHeight) / galleries;
        var height = fixedHeight;
        foreach (var child in Children)
        {
            if (child is RibbonGallery && child.IsVisible)
            {
                child.Measure(availableSize.WithHeight(share));
                width = Math.Max(width, child.DesiredSize.Width);
                height += child.DesiredSize.Height;
            }
        }

        return new Size(width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var desired = 0.0;
        var galleries = 0;
        foreach (var child in Children)
        {
            desired += child.DesiredSize.Height;
            if (child is RibbonGallery && child.IsVisible)
            {
                galleries++;
            }
        }

        // Height beyond what the items asked for goes to the galleries.
        var surplus = galleries > 0 ? Math.Max(0, finalSize.Height - desired) / galleries : 0;
        var y = 0.0;
        foreach (var child in Children)
        {
            var height = child.DesiredSize.Height + (child is RibbonGallery && child.IsVisible ? surplus : 0);
            child.Arrange(new Rect(0, y, finalSize.Width, height));
            y += height;
        }

        return finalSize;
    }
}
