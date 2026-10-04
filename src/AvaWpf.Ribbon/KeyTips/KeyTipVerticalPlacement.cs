// Ported from WPF $R/Microsoft/Windows/Controls/KeyTipVerticalPlacement.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>Where a KeyTip sits vertically relative to its target.</summary>
public enum KeyTipVerticalPlacement
{
    /// <summary>The KeyTip's top edge at the target's top edge.</summary>
    KeyTipTopAtTargetTop,

    /// <summary>The KeyTip's top edge at the target's center.</summary>
    KeyTipTopAtTargetCenter,

    /// <summary>The KeyTip's top edge at the target's bottom edge.</summary>
    KeyTipTopAtTargetBottom,

    /// <summary>The KeyTip's center at the target's top edge.</summary>
    KeyTipCenterAtTargetTop,

    /// <summary>The KeyTip's center at the target's center.</summary>
    KeyTipCenterAtTargetCenter,

    /// <summary>The KeyTip's center at the target's bottom edge.</summary>
    KeyTipCenterAtTargetBottom,

    /// <summary>The KeyTip's bottom edge at the target's top edge.</summary>
    KeyTipBottomAtTargetTop,

    /// <summary>The KeyTip's bottom edge at the target's center.</summary>
    KeyTipBottomAtTargetCenter,

    /// <summary>The KeyTip's bottom edge at the target's bottom edge.</summary>
    KeyTipBottomAtTargetBottom,
}
