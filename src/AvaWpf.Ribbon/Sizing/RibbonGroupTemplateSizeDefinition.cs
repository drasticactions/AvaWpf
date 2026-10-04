// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonGroupTemplateSizeDefinition.cs (MIT, see NOTICE.md).
using Avalonia.Controls.Templates;

namespace AvaWpf.Ribbon;

/// <summary>A size step of a <see cref="RibbonGroup"/> that replaces the group's items with a template.</summary>
public class RibbonGroupTemplateSizeDefinition : RibbonGroupSizeDefinitionBase
{
    /// <summary>The template shown instead of the items. The group's DataContext is its data context.</summary>
    public IDataTemplate? ContentTemplate { get; set; }
}
