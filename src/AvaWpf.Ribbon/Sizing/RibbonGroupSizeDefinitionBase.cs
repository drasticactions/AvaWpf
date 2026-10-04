// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonGroupSizeDefinitionBase.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>One size step of a <see cref="RibbonGroup"/> (see <see cref="RibbonGroup.GroupSizeDefinitions"/>).</summary>
public abstract class RibbonGroupSizeDefinitionBase
{
    /// <summary>Whether the group shows as a single drop-down button in this step.</summary>
    public bool IsCollapsed { get; set; }
}
