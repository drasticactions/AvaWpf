// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonControlLengthUnitType.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>The unit of a <see cref="RibbonControlLength"/>.</summary>
public enum RibbonControlLengthUnitType
{
    /// <summary>The size of the content.</summary>
    Auto,

    /// <summary>Device-independent pixels.</summary>
    Pixel,

    /// <summary>A number of items (galleries: columns of items).</summary>
    Item,

    /// <summary>A weighted share of the space left in the RibbonGroupsPanel.</summary>
    Star,
}
