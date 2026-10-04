// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonSplitButtonLabelPosition.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>Which half of a large <see cref="RibbonSplitButton"/> shows the label.</summary>
public enum RibbonSplitButtonLabelPosition
{
    /// <summary>The label is on the header (upper) half.</summary>
    Header,

    /// <summary>The label is on the drop-down (lower) half.</summary>
    DropDown,
}
