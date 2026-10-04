// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/Primitives/RibbonGalleryItemsPanel.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaWpf.Ribbon.Primitives;

/// <summary>
/// Lays out the items of a <see cref="RibbonGalleryCategory"/> in uniform cells, as many columns as fit, within the
/// category's (or the gallery's) <c>MinColumnCount</c> and <c>MaxColumnCount</c>.
/// </summary>
public class RibbonGalleryItemsPanel : Panel
{
    private Size _cell;
    private int _columns = 1;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var cell = default(Size);
        var count = 0;
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            if (!child.IsVisible)
            {
                continue;
            }

            count++;
            cell = new Size(Math.Max(cell.Width, child.DesiredSize.Width), Math.Max(cell.Height, child.DesiredSize.Height));
        }

        _cell = cell;
        if (count == 0 || cell.Width <= 0)
        {
            _columns = 1;
            return default;
        }

        var (min, max) = ColumnLimits();
        var fit = double.IsInfinity(availableSize.Width) ? count : (int)Math.Floor(availableSize.Width / cell.Width);
        _columns = Math.Max(1, Math.Clamp(Math.Min(fit, count), min, Math.Max(min, max)));
        var rows = (int)Math.Ceiling(count / (double)_columns);
        return new Size(_columns * cell.Width, rows * cell.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var index = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            var column = index % _columns;
            var row = index / _columns;
            child.Arrange(new Rect(new Point(column * _cell.Width, row * _cell.Height), _cell));
            index++;
        }

        return finalSize;
    }

    private (int Min, int Max) ColumnLimits()
    {
        // In the row of an InRibbonGallery (not in its drop-down, which is another visual tree) the InRibbonGallery's
        // column counts apply, as WPF's in-ribbon mode.
        if (this.FindAncestorOfType<InRibbonGallery>() is { } inRibbon)
        {
            var inMin = Math.Max(1, inRibbon.MinColumnCount);
            return (inMin, Math.Max(inMin, inRibbon.MaxColumnCount));
        }

        var category = this.FindAncestorOfType<RibbonGalleryCategory>();
        var gallery = category?.Gallery;
        var min = Math.Max(category?.MinColumnCount ?? 1, gallery?.MinColumnCount ?? 1);
        var max = Math.Min(category?.MaxColumnCount ?? int.MaxValue, gallery?.MaxColumnCount ?? int.MaxValue);
        return (Math.Max(1, min), Math.Max(1, max));
    }
}
