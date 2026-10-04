// Ported from WPF $R/Microsoft/Windows/Controls/KeyTipService.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>The state of KeyTip mode, as WPF's <c>KeyTipService.KeyTipState</c>.</summary>
internal enum KeyTipState
{
    /// <summary>KeyTip mode is off.</summary>
    None,

    /// <summary>Alt or F10 is down; the KeyTips show when it is released.</summary>
    Pending,

    /// <summary>The KeyTips of the current scope are showing.</summary>
    Enabled,
}
