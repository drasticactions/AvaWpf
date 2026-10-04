// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/Primitives/RibbonGroupsPanel.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaWpf.Ribbon.Primitives;

/// <summary>
/// The items panel of a <see cref="RibbonTab"/>: lays the groups out left to right and resizes them to the width.
/// </summary>
/// <remarks>
/// <para>
/// All resizing happens in <see cref="MeasureOverride"/> from sizes measured in the same pass. Too wide: the tab
/// shrinks the next group (<see cref="RibbonTab.GroupSizeReductionOrder"/> first, then right to left). With room: it
/// undoes the last reduction if the groups still fit, and remembers the width a failed attempt needed (WPF's
/// <c>_nextGroupIncreaseWidth</c>).
/// </para>
/// </remarks>
public class RibbonGroupsPanel : Panel
{
    private double _nextGroupIncreaseWidth = double.NaN;
    private int _cachedChildCount;

    /// <summary>Forgets the remembered width and measures again (the groups or their steps changed).</summary>
    internal void InvalidateGroupSizes()
    {
        _nextGroupIncreaseWidth = double.NaN;
        InvalidateMeasure();
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = BasicMeasure(availableSize, out var remaining);
        var tab = this.FindAncestorOfType<RibbonTab>();
        if (tab is null || double.IsInfinity(availableSize.Width))
        {
            return desired;
        }

        if (Children.Count != _cachedChildCount)
        {
            _nextGroupIncreaseWidth = double.NaN;
            _cachedChildCount = Children.Count;
        }

        // Grow: undo the last reduction while it fits.
        while (remaining > 0 && (double.IsNaN(_nextGroupIncreaseWidth) || remaining >= _nextGroupIncreaseWidth))
        {
            if (!tab.IncreaseNextGroupSize())
            {
                break;
            }

            var before = remaining;
            desired = BasicMeasure(availableSize, out remaining);
            if (remaining < 0)
            {
                // It does not fit: shrink again and remember how much room this step needs.
                var cost = before - remaining;
                tab.DecreaseNextGroupSize();
                desired = BasicMeasure(availableSize, out remaining);
                _nextGroupIncreaseWidth = cost;
                break;
            }

            _nextGroupIncreaseWidth = double.NaN;
        }

        // Shrink: reduce the next group while the groups are too wide.
        while (remaining < 0)
        {
            if (!tab.DecreaseNextGroupSize())
            {
                break;
            }

            desired = BasicMeasure(availableSize, out remaining);
        }

        return new Size(Math.Min(desired.Width, availableSize.Width), desired.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0.0;
        foreach (var child in Children)
        {
            var width = child.DesiredSize.Width;
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }

    private Size BasicMeasure(Size availableSize, out double remaining)
    {
        var width = 0.0;
        var height = 0.0;
        var childConstraint = new Size(double.PositiveInfinity, availableSize.Height);
        foreach (var child in Children)
        {
            child.Measure(childConstraint);
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        remaining = availableSize.Width - width;
        return new Size(width, height);
    }
}
