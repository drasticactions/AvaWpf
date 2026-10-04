// Ported from WPF $W/PresentationFramework/System/Windows/Controls/Primitives/ToolBarPanel.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace AvaWpf.Controls;

/// <summary>
/// The items panel of a <see cref="ToolBar"/>; it moves the items that do not fit to the bar's
/// <see cref="ToolBarOverflowPanel"/>.
/// </summary>
/// <remarks>
/// <see cref="Panel.Children"/> holds every container in item order, so the <see cref="ItemsControl"/> keeps its
/// indices, but only main-bar containers are visual children. Outside a <see cref="ToolBar"/> it acts as a
/// <see cref="StackPanel"/>.
/// </remarks>
public class ToolBarPanel : StackPanel
{
    private IDisposable? _orientationBinding;

    /// <summary>The length of the items that can never overflow, from the last measure.</summary>
    internal double MinLength { get; private set; }

    /// <summary>The length of every item, from the last measure.</summary>
    internal double MaxLength { get; private set; }

    /// <summary>The bar whose items panel this is.</summary>
    internal ToolBar? ToolBar => IsItemsHost ? TemplatedParent as ToolBar : null;

    private ToolBarOverflowPanel? OverflowPanel => ToolBar?.ToolBarOverflowPanel;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TemplatedParentProperty)
        {
            // Follow the bar's orientation unless set otherwise.
            _orientationBinding?.Dispose();
            _orientationBinding = null;
            if (change.NewValue is ToolBar tb)
            {
                _orientationBinding = Bind(OrientationProperty, tb.GetObservable(ToolBar.OrientationProperty), BindingPriority.Template);
            }
        }
    }

    /// <summary>Keeps the visual children to the main-bar containers; new containers start in the main bar.</summary>
    /// <param name="sender">The children collection.</param>
    /// <param name="e">The change.</param>
    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!IsItemsHost)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    LogicalChildren.InsertRange(e.NewStartingIndex, e.NewItems!.OfType<Control>().ToList());
                    break;
                case NotifyCollectionChangedAction.Move:
                    LogicalChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    LogicalChildren.RemoveAll(e.OldItems!.OfType<Control>().ToList());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    for (var i = 0; i < e.OldItems!.Count; ++i)
                    {
                        LogicalChildren[i + e.OldStartingIndex] = (Control)e.NewItems![i]!;
                    }

                    break;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (var old in e.OldItems.OfType<Control>())
            {
                if (!Children.Contains(old))
                {
                    VisualChildren.Remove(old);
                    OverflowPanel?.ReleaseChild(old);
                }
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var added in e.NewItems.OfType<Control>())
            {
                // Reset the overflow decision; it is made again on the next measure.
                ToolBar.SetIsOverflowItem(added, false);
            }
        }

        SyncVisualChildren();
        InvalidateMeasure();
        OverflowPanel?.InvalidateMeasure();
    }

    /// <summary>
    /// Measures the fixed items first, then the <see cref="OverflowMode.AsNeeded"/> items in order; from the first that
    /// does not fit, they all overflow.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (ToolBar is not { } toolBar)
        {
            return base.MeasureOverride(availableSize);
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var slot = horizontal ? availableSize.WithWidth(double.PositiveInfinity) : availableSize.WithHeight(double.PositiveInfinity);
        var maxExtent = horizontal ? availableSize.Width : availableSize.Height;
        var desired = default(Size);

        var hasAlways = MeasureGeneratedItems(false, slot, horizontal, maxExtent, ref desired, out _);
        MinLength = horizontal ? desired.Width : desired.Height;
        var hasAsNeeded = MeasureGeneratedItems(true, slot, horizontal, maxExtent, ref desired, out var overflowExtent);
        MaxLength = (horizontal ? desired.Width : desired.Height) + overflowExtent;

        SyncVisualChildren();
        toolBar.HasOverflowItems = hasAlways || hasAsNeeded;
        return desired;
    }

    /// <summary>Arranges the main-bar items side by side, each stretched across the bar.</summary>
    /// <param name="finalSize">The final size.</param>
    /// <returns>The size used.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (ToolBar is null)
        {
            return base.ArrangeOverride(finalSize);
        }

        var horizontal = Orientation == Orientation.Horizontal;
        double offset = 0;
        foreach (var child in Children)
        {
            if (ToolBar.GetIsOverflowItem(child))
            {
                continue;
            }

            var size = child.DesiredSize;
            var rect = horizontal
                ? new Rect(offset, 0, size.Width, Math.Max(finalSize.Height, size.Height))
                : new Rect(0, offset, Math.Max(finalSize.Width, size.Width), size.Height);
            child.Arrange(rect);
            offset += horizontal ? size.Width : size.Height;
        }

        return finalSize;
    }

    /// <summary>Releases a container that the overflow panel is about to host.</summary>
    /// <param name="child">The container.</param>
    internal void ReleaseChild(Control child) => VisualChildren.Remove(child);

    private bool MeasureGeneratedItems(bool asNeededPass, Size constraint, bool horizontal, double maxExtent, ref Size panelDesiredSize, out double overflowExtent)
    {
        var sendToOverflow = false; // Becomes true when the first AsNeeded item does not fit.
        var hasOverflowItems = false;
        overflowExtent = 0.0;

        foreach (var child in Children)
        {
            var mode = ToolBar.GetOverflowMode(child);
            var asNeeded = mode == OverflowMode.AsNeeded;
            if (asNeeded != asNeededPass)
            {
                continue;
            }

            if (mode != OverflowMode.Always && !sendToOverflow)
            {
                // Measure with the main-bar look: the item may size differently in the overflow.
                ToolBar.SetIsOverflowItem(child, false);
                child.Measure(constraint);
                var childSize = child.DesiredSize;
                if (asNeeded)
                {
                    var newExtent = horizontal ? childSize.Width + panelDesiredSize.Width : childSize.Height + panelDesiredSize.Height;
                    if (GreaterThan(newExtent, maxExtent))
                    {
                        sendToOverflow = true;
                    }
                }

                if (!sendToOverflow)
                {
                    panelDesiredSize = horizontal
                        ? new Size(panelDesiredSize.Width + childSize.Width, Math.Max(panelDesiredSize.Height, childSize.Height))
                        : new Size(Math.Max(panelDesiredSize.Width, childSize.Width), panelDesiredSize.Height + childSize.Height);
                }
            }

            if (mode == OverflowMode.Always || sendToOverflow)
            {
                hasOverflowItems = true;

                // Keep the thickness of the bar and the length saved by overflowing, for MaxLength.
                if (!child.IsMeasureValid)
                {
                    ToolBar.SetIsOverflowItem(child, false);
                    child.Measure(constraint);
                }

                var childSize = child.DesiredSize;
                if (horizontal)
                {
                    overflowExtent += childSize.Width;
                    panelDesiredSize = panelDesiredSize.WithHeight(Math.Max(panelDesiredSize.Height, childSize.Height));
                }
                else
                {
                    overflowExtent += childSize.Height;
                    panelDesiredSize = panelDesiredSize.WithWidth(Math.Max(panelDesiredSize.Width, childSize.Width));
                }

                ToolBar.SetIsOverflowItem(child, true);
            }
        }

        return hasOverflowItems;
    }

    /// <summary>Makes the visual children exactly the main-bar containers, in order.</summary>
    private void SyncVisualChildren()
    {
        var overflow = OverflowPanel;
        var changed = false;
        var vi = 0;
        foreach (var child in Children)
        {
            if (ToolBar is not null && ToolBar.GetIsOverflowItem(child))
            {
                continue;
            }

            if (vi < VisualChildren.Count && ReferenceEquals(VisualChildren[vi], child))
            {
                vi++;
                continue;
            }

            var existing = VisualChildren.IndexOf(child);
            if (existing >= 0)
            {
                VisualChildren.Move(existing, vi);
            }
            else
            {
                overflow?.ReleaseChild(child);
                VisualChildren.Insert(vi, child);
            }

            changed = true;
            vi++;
        }

        while (VisualChildren.Count > vi)
        {
            VisualChildren.RemoveAt(VisualChildren.Count - 1);
            changed = true;
        }

        if (changed)
        {
            overflow?.InvalidateMeasure();
        }
    }

    private static bool GreaterThan(double a, double b) => a > b && !AreClose(a, b);

    private static bool AreClose(double a, double b)
    {
        if (a == b)
        {
            return true;
        }

        var eps = (Math.Abs(a) + Math.Abs(b) + 10.0) * 2.220446049250313E-16;
        var delta = a - b;
        return -eps < delta && eps > delta;
    }
}
