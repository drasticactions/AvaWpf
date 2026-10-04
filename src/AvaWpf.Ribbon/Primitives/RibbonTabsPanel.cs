// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/Primitives/RibbonTabsPanel.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.Ribbon.Primitives;

/// <summary>The items panel of a <see cref="Ribbon"/>: gives the selected <see cref="RibbonTab"/> the whole area and the others none.</summary>
public class RibbonTabsPanel : Panel
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = default(Size);
        foreach (var child in Children)
        {
            if (IsShown(child))
            {
                child.Measure(availableSize);
                desired = new Size(Math.Max(desired.Width, child.DesiredSize.Width), Math.Max(desired.Height, child.DesiredSize.Height));
            }
        }

        return desired;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            child.Arrange(IsShown(child) ? new Rect(finalSize) : default);
        }

        return finalSize;
    }

    // An unselected tab is neither measured nor given room; its template hides its groups.
    private static bool IsShown(Control child) => child is not RibbonTab tab || tab.IsSelected;
}
