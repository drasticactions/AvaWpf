// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonContextMenu.cs (MIT, see NOTICE.md).
using Avalonia.Controls;

namespace AvaWpf.Ribbon;

/// <summary>
/// The Ribbon's context menu: "Add to Quick Access Toolbar", "Show Quick Access Toolbar below the Ribbon", "Minimize
/// the Ribbon". The Ribbon opens it on right-click over its controls; its items are <see cref="RibbonMenuItem"/>s.
/// </summary>
public class RibbonContextMenu : ContextMenu
{
    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is Control)
        {
            recycleKey = null;
            return false;
        }

        return NeedsContainer<RibbonMenuItem>(item, out recycleKey);
    }

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new RibbonMenuItem();
}
