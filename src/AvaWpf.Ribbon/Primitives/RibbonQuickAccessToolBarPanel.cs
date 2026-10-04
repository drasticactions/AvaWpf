// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/Primitives/RibbonQuickAccessToolBarPanel.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using AvaWpf.Controls;

namespace AvaWpf.Ribbon.Primitives;

/// <summary>
/// The items panel of a <see cref="RibbonQuickAccessToolBar"/>: lays the items out in a row and moves those from the
/// first that does not fit to the <see cref="ToolBarOverflowPanel"/>, deciding in <see cref="MeasureOverride"/>.
/// </summary>
public class RibbonQuickAccessToolBarPanel : Panel
{
    /// <inheritdoc/>
    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // The children's visuals are placed by MeasureOverride: here or in the overflow panel.
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is Control control)
                {
                    Detach(control);
                    if (!IsItemsHost)
                    {
                        LogicalChildren.Remove(control);
                    }
                }
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var visual in new List<Visual>(VisualChildren))
            {
                if (visual is Control control && !Children.Contains(control))
                {
                    VisualChildren.Remove(control);
                }
            }

            if (OverflowPanel is { } overflow)
            {
                foreach (var item in new List<Control>(overflow.HostedItems))
                {
                    if (!Children.Contains(item))
                    {
                        overflow.RemoveHostedItem(item);
                    }
                }
            }
        }

        if (!IsItemsHost && e.NewItems is not null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is Control control && !LogicalChildren.Contains(control))
                {
                    LogicalChildren.Add(control);
                }
            }
        }

        InvalidateMeasure();
    }

    /// <summary>The overflow panel of the toolbar.</summary>
    private ToolBarOverflowPanel? OverflowPanel => this.FindAncestorOfType<RibbonQuickAccessToolBar>()?.OverflowPanel;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var overflow = OverflowPanel;
        var width = 0.0;
        var height = 0.0;
        var main = new List<Control>();
        var overflowed = new List<Control>();
        var sendToOverflow = false;
        foreach (var child in Children)
        {
            if (!sendToOverflow)
            {
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                var size = child.DesiredSize;
                if (width + size.Width > availableSize.Width + 0.01 && overflow is not null)
                {
                    sendToOverflow = true;
                }
                else
                {
                    width += size.Width;
                    height = Math.Max(height, size.Height);
                    main.Add(child);
                    continue;
                }
            }

            overflowed.Add(child);
        }

        // Move the visuals only where the split changed.
        for (var i = 0; i < main.Count; i++)
        {
            var child = main[i];
            if (i < VisualChildren.Count && VisualChildren[i] == child)
            {
                continue;
            }

            Detach(child);
            VisualChildren.Insert(Math.Min(i, VisualChildren.Count), child);
            RibbonQuickAccessToolBar.SetIsOverflowItem(child, false);
        }

        while (VisualChildren.Count > main.Count)
        {
            VisualChildren.RemoveAt(VisualChildren.Count - 1);
        }

        if (overflow is not null)
        {
            for (var i = 0; i < overflowed.Count; i++)
            {
                var child = overflowed[i];
                if (i < overflow.HostedItems.Count && overflow.HostedItems[i] == child)
                {
                    continue;
                }

                Detach(child);
                overflow.InsertHostedItem(Math.Min(i, overflow.HostedItems.Count), child);
                RibbonQuickAccessToolBar.SetIsOverflowItem(child, true);
            }

            while (overflow.HostedItems.Count > overflowed.Count)
            {
                overflow.RemoveHostedItem(overflow.HostedItems[^1]);
            }
        }

        this.FindAncestorOfType<RibbonQuickAccessToolBar>()?.SetHasOverflowItems(overflowed.Count > 0);
        return new Size(width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0.0;
        foreach (var visual in VisualChildren)
        {
            if (visual is Control child)
            {
                child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
                x += child.DesiredSize.Width;
            }
        }

        return finalSize;
    }

    private void Detach(Control child)
    {
        if (child.GetVisualParent() == this)
        {
            VisualChildren.Remove(child);
        }
        else if (child.GetVisualParent() is ToolBarOverflowPanel overflow)
        {
            overflow.RemoveHostedItem(child);
        }
    }
}
