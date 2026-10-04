// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonDismissPopupMode.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>Which popups a <see cref="RibbonControlService.DismissPopupEvent"/> closes.</summary>
public enum RibbonDismissPopupMode
{
    /// <summary>Every popup between the source and the Ribbon closes.</summary>
    Always,

    /// <summary>Only the popups the pointer is not over close.</summary>
    MousePhysicallyNotOver,
}
