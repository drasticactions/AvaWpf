// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/Primitives/RibbonContextualTabGroupsPanel.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.Ribbon.Primitives;

/// <summary>
/// Places each <see cref="RibbonContextualTabGroup"/> title over its tab headers, using positions from the tab row's
/// previous layout pass, and records each title's ideal width for <see cref="RibbonTabHeadersPanel"/>.
/// </summary>
public class RibbonContextualTabGroupsPanel : Panel
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;
        var right = 0.0;
        var idealChanged = false;
        Ribbon? ribbon = null;
        foreach (var child in Children)
        {
            if (child is RibbonContextualTabGroup ideal && ideal.IsVisible)
            {
                child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                if (Math.Abs(child.DesiredSize.Width - ideal.IdealWidth) > 0.5)
                {
                    ideal.IdealWidth = child.DesiredSize.Width;
                    idealChanged = true;
                    ribbon ??= ideal.Ribbon;
                }
            }

            if (child is RibbonContextualTabGroup group && group.TabsWidth > 0)
            {
                child.Measure(new Size(group.TabsWidth, availableSize.Height));
                height = Math.Max(height, child.DesiredSize.Height);
                right = Math.Max(right, group.TabsLeft + group.TabsWidth);
            }
            else
            {
                child.Measure(default);
            }
        }

        if (idealChanged)
        {
            ribbon?.InvalidateTabHeaders();
        }

        return new Size(right, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            if (child is RibbonContextualTabGroup group && group.TabsWidth > 0)
            {
                child.Arrange(new Rect(group.TabsLeft, 0, group.TabsWidth, finalSize.Height));
            }
            else
            {
                child.Arrange(default);
            }
        }

        return finalSize;
    }
}
