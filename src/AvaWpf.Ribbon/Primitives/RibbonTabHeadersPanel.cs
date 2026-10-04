// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/Primitives/RibbonTabHeadersPanel.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.Ribbon.Primitives;

/// <summary>
/// Lays out the tab headers in a row, widening a contextual group's headers until the group's title fits over them.
/// The title's ideal width comes from the previous measure pass of <see cref="RibbonContextualTabGroupsPanel"/>.
/// </summary>
public class RibbonTabHeadersPanel : Panel
{
    private readonly Dictionary<RibbonContextualTabGroup, (double Width, int Count)> _groups = new();

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _groups.Clear();
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
            if (child is RibbonTabHeader { ContextualTabGroup: { } group } && child.IsVisible)
            {
                _groups.TryGetValue(group, out var g);
                _groups[group] = (g.Width + child.DesiredSize.Width, g.Count + 1);
            }
        }

        foreach (var (group, g) in _groups)
        {
            width += Math.Max(0, group.IdealWidth - g.Width);
        }

        return new Size(width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var x = 0.0;
        foreach (var child in Children)
        {
            var w = child.IsVisible ? child.DesiredSize.Width + Padding(child) : 0;
            child.Arrange(new Rect(x, 0, w, finalSize.Height));
            x += w;
        }

        return finalSize;
    }

    /// <summary>The extra width of a contextual tab header: its share of what its group's title lacks.</summary>
    private double Padding(Control child) =>
        child is RibbonTabHeader { ContextualTabGroup: { } group } && _groups.TryGetValue(group, out var g) && g.Count > 0
            ? Math.Max(0, group.IdealWidth - g.Width) / g.Count
            : 0;
}
