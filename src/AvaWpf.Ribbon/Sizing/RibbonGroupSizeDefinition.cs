// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonGroupSizeDefinition.cs (MIT, see NOTICE.md).
using Avalonia.Metadata;

namespace AvaWpf.Ribbon;

/// <summary>A size step of a <see cref="RibbonGroup"/> that sizes each control of the group.</summary>
public class RibbonGroupSizeDefinition : RibbonGroupSizeDefinitionBase
{
    /// <summary>One definition per control of the group, in item order. Controls past the end keep their default size.</summary>
    [Content]
    public RibbonControlSizeDefinitionCollection ControlSizeDefinitions { get; } = new();

    /// <inheritdoc/>
    public override string ToString() => IsCollapsed ? "Collapsed" : string.Join(" ", ControlSizeDefinitions);
}
