// Ported from WPF $W/PresentationFramework/System/Windows/Controls/Primitives/ToolBarOverflowPanel.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaWpf.Controls;

/// <summary>
/// The panel in a <see cref="ToolBar"/> overflow popup; it wraps the overflow items into lines no wider than
/// <see cref="WrapWidth"/>.
/// </summary>
/// <remarks>
/// Overflow containers are visual children here but stay logical children of the bar. Other bar-like controls host
/// items through <see cref="InsertHostedItem"/> and <see cref="RemoveHostedItem"/>; otherwise the panel wraps its own
/// <see cref="Panel.Children"/>.
/// </remarks>
public class ToolBarOverflowPanel : Panel
{
    /// <summary>Defines the <see cref="WrapWidth"/> property.</summary>
    public static readonly StyledProperty<double> WrapWidthProperty =
        AvaloniaProperty.Register<ToolBarOverflowPanel, double>(nameof(WrapWidth), double.NaN, validate: IsWrapWidthValid);

    private List<Control>? _hostedItems; // Items placed here by a host other than a ToolBar, in order.
    private double _wrapWidth; // Calculated in MeasureOverride and used in ArrangeOverride.
    private Size _panelSize;

    static ToolBarOverflowPanel()
    {
        AffectsMeasure<ToolBarOverflowPanel>(WrapWidthProperty);
    }

    /// <summary>
    /// The width at which items wrap; <see cref="double.NaN"/> (default) wraps at the available width.
    /// </summary>
    public double WrapWidth
    {
        get => GetValue(WrapWidthProperty);
        set => SetValue(WrapWidthProperty, value);
    }

    private ToolBar? ToolBar => TemplatedParent as ToolBar;

    private ToolBarPanel? ToolBarPanel => ToolBar?.ToolBarPanel;

    /// <summary>Takes the overflow items of the bar, then measures and wraps them.</summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        var curLineSize = default(Size);
        _panelSize = default;
        _wrapWidth = double.IsNaN(WrapWidth) ? availableSize.Width : WrapWidth;

        SyncOverflowItems();
        var children = LayoutChildren();

        // Measure all children to determine if the wrap width must grow.
        foreach (var child in children)
        {
            child.Measure(availableSize);
            if (GreaterThan(child.DesiredSize.Width, _wrapWidth))
            {
                _wrapWidth = child.DesiredSize.Width;
            }
        }

        // The wrap width is never bigger than the available width.
        _wrapWidth = Math.Min(_wrapWidth, availableSize.Width);

        foreach (var child in children)
        {
            var sz = child.DesiredSize;
            if (GreaterThan(curLineSize.Width + sz.Width, _wrapWidth))
            {
                // Switch to another line.
                _panelSize = new Size(Math.Max(curLineSize.Width, _panelSize.Width), _panelSize.Height + curLineSize.Height);
                curLineSize = sz;
                if (GreaterThan(sz.Width, _wrapWidth))
                {
                    // The item is wider than the wrap width: give it a line of its own.
                    _panelSize = new Size(Math.Max(sz.Width, _panelSize.Width), _panelSize.Height + sz.Height);
                    curLineSize = default;
                }
            }
            else
            {
                curLineSize = new Size(curLineSize.Width + sz.Width, Math.Max(sz.Height, curLineSize.Height));
            }
        }

        _panelSize = new Size(Math.Max(curLineSize.Width, _panelSize.Width), _panelSize.Height + curLineSize.Height);
        return _panelSize;
    }

    /// <summary>Arranges the items line by line, each line as tall as its tallest item.</summary>
    /// <param name="finalSize">The final size.</param>
    /// <returns>The size used.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = LayoutChildren();
        var firstInLine = 0;
        var curLineSize = default(Size);
        var accumulatedHeight = 0.0;
        _wrapWidth = Math.Min(_wrapWidth, finalSize.Width);

        for (var i = 0; i < children.Count; i++)
        {
            var sz = children[i].DesiredSize;
            if (GreaterThan(curLineSize.Width + sz.Width, _wrapWidth))
            {
                // Arrange the current line without this item, which starts the next line.
                ArrangeLine(children, accumulatedHeight, curLineSize.Height, firstInLine, i);
                accumulatedHeight += curLineSize.Height;
                firstInLine = i;
                curLineSize = sz;
            }
            else
            {
                curLineSize = new Size(curLineSize.Width + sz.Width, Math.Max(sz.Height, curLineSize.Height));
            }
        }

        ArrangeLine(children, accumulatedHeight, curLineSize.Height, firstInLine, children.Count);
        return _panelSize;
    }

    /// <summary>The items a host other than a <see cref="ToolBar"/> placed in the panel, in order.</summary>
    internal IReadOnlyList<Control> HostedItems => (IReadOnlyList<Control>?)_hostedItems ?? Array.Empty<Control>();

    /// <summary>
    /// Hosts the visual of an overflowed item for a control other than a <see cref="ToolBar"/>; the caller removes it
    /// from its previous visual parent first.
    /// </summary>
    /// <param name="index">The position among the hosted items.</param>
    /// <param name="item">The item.</param>
    internal void InsertHostedItem(int index, Control item)
    {
        _hostedItems ??= new List<Control>();
        _hostedItems.Insert(index, item);
        VisualChildren.Insert(index, item);
        InvalidateMeasure();
    }

    /// <summary>Gives back the visual of an item hosted by <see cref="InsertHostedItem"/>.</summary>
    /// <param name="item">The item.</param>
    internal void RemoveHostedItem(Control item)
    {
        if (_hostedItems is not null && _hostedItems.Remove(item))
        {
            VisualChildren.Remove(item);
            InvalidateMeasure();
        }
    }

    /// <summary>Releases a container that the main bar is about to host again.</summary>
    /// <param name="child">The container.</param>
    internal void ReleaseChild(Control child)
    {
        if (ToolBar is not null)
        {
            VisualChildren.Remove(child);
        }
    }

    private static void ArrangeLine(List<Control> children, double y, double lineHeight, int start, int end)
    {
        double x = 0;
        for (var i = start; i < end; i++)
        {
            var child = children[i];
            child.Arrange(new Rect(x, y, child.DesiredSize.Width, lineHeight));
            x += child.DesiredSize.Width;
        }
    }

    private static bool IsWrapWidthValid(double v) => double.IsNaN(v) || (v >= 0 && !double.IsPositiveInfinity(v));

    private static bool GreaterThan(double a, double b) => a > b && Math.Abs(a - b) > 1e-9 * Math.Max(1.0, Math.Abs(a) + Math.Abs(b));

    /// <summary>Makes the visual children the bar's overflow containers (not separators), in item order.</summary>
    private void SyncOverflowItems()
    {
        if (ToolBarPanel is not { } panel)
        {
            return;
        }

        var vi = 0;
        foreach (var child in panel.Children)
        {
            if (!ToolBar.GetIsOverflowItem(child) || child is Separator)
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
                panel.ReleaseChild(child);
                if (child.GetVisualParent() is not null)
                {
                    continue;
                }

                VisualChildren.Insert(vi, child);
            }

            vi++;
        }

        while (VisualChildren.Count > vi)
        {
            VisualChildren.RemoveAt(VisualChildren.Count - 1);
        }
    }

    private List<Control> LayoutChildren()
    {
        var list = new List<Control>();
        if (_hostedItems is not null)
        {
            list.AddRange(_hostedItems);
        }
        else if (ToolBar is not null)
        {
            foreach (var v in VisualChildren)
            {
                if (v is Control c)
                {
                    list.Add(c);
                }
            }
        }
        else
        {
            list.AddRange(Children);
        }

        return list;
    }
}
