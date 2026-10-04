// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonTabHeaderItemsControl.cs (MIT, see NOTICE.md).
using Avalonia.Controls;

namespace AvaWpf.Ribbon;

/// <summary>The tab row of a <see cref="Ribbon"/>: the Ribbon fills it with one <see cref="RibbonTabHeader"/> per tab.</summary>
public class RibbonTabHeaderItemsControl : ItemsControl
{
    /// <summary>The Ribbon the tab row belongs to: its templated parent.</summary>
    public Ribbon? Ribbon => TemplatedParent as Ribbon;
}
