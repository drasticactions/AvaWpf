// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonApplicationMenuItemLevel.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>The nesting level of an application menu item, which picks its look.</summary>
public enum RibbonApplicationMenuItemLevel
{
    /// <summary>A direct item of the application menu: a large image and a label.</summary>
    Top,

    /// <summary>An item of a top-level item's submenu: a large image, a bold header and a description.</summary>
    Middle,

    /// <summary>A deeper item: a plain menu item.</summary>
    Sub,
}
