// Ported from WPF $R/Microsoft/Windows/Controls/KeyTipHorizontalPlacement.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>Where a KeyTip sits horizontally relative to its target.</summary>
public enum KeyTipHorizontalPlacement
{
    /// <summary>The KeyTip's left edge at the target's left edge.</summary>
    KeyTipLeftAtTargetLeft,

    /// <summary>The KeyTip's left edge at the target's center.</summary>
    KeyTipLeftAtTargetCenter,

    /// <summary>The KeyTip's left edge at the target's right edge.</summary>
    KeyTipLeftAtTargetRight,

    /// <summary>The KeyTip's center at the target's left edge.</summary>
    KeyTipCenterAtTargetLeft,

    /// <summary>The KeyTip's center at the target's center.</summary>
    KeyTipCenterAtTargetCenter,

    /// <summary>The KeyTip's center at the target's right edge.</summary>
    KeyTipCenterAtTargetRight,

    /// <summary>The KeyTip's right edge at the target's left edge.</summary>
    KeyTipRightAtTargetLeft,

    /// <summary>The KeyTip's right edge at the target's center.</summary>
    KeyTipRightAtTargetCenter,

    /// <summary>The KeyTip's right edge at the target's right edge.</summary>
    KeyTipRightAtTargetRight,
}
