// Ported from WPF $R/Microsoft/Windows/Controls/KeyTipAdorner.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.Ribbon;

/// <summary>
/// The adorner that hosts one <see cref="KeyTipControl"/>. The adorner layer arranges it over its target; it places
/// the KeyTip by the placements of <see cref="ActivatingKeyTipEventArgs"/>, outside the target bounds if needed.
/// </summary>
internal sealed class KeyTipAdorner : Control
{
    private readonly ActivatingKeyTipEventArgs _placement;

    public KeyTipAdorner(KeyTipControl keyTip, ActivatingKeyTipEventArgs placement)
    {
        KeyTip = keyTip;
        _placement = placement;
        IsHitTestVisible = false;
        LogicalChildren.Add(keyTip);
        VisualChildren.Add(keyTip);
    }

    public KeyTipControl KeyTip { get; }

    protected override Size MeasureOverride(Size availableSize)
    {
        KeyTip.Measure(Size.Infinity);
        return default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = KeyTip.DesiredSize;
        var x = _placement.KeyTipHorizontalPlacement switch
        {
            KeyTipHorizontalPlacement.KeyTipLeftAtTargetLeft => 0,
            KeyTipHorizontalPlacement.KeyTipLeftAtTargetCenter => finalSize.Width / 2,
            KeyTipHorizontalPlacement.KeyTipLeftAtTargetRight => finalSize.Width,
            KeyTipHorizontalPlacement.KeyTipCenterAtTargetLeft => -size.Width / 2,
            KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter => (finalSize.Width - size.Width) / 2,
            KeyTipHorizontalPlacement.KeyTipCenterAtTargetRight => finalSize.Width - (size.Width / 2),
            KeyTipHorizontalPlacement.KeyTipRightAtTargetLeft => -size.Width,
            KeyTipHorizontalPlacement.KeyTipRightAtTargetCenter => (finalSize.Width / 2) - size.Width,
            _ => finalSize.Width - size.Width,
        };
        var y = _placement.KeyTipVerticalPlacement switch
        {
            KeyTipVerticalPlacement.KeyTipTopAtTargetTop => 0,
            KeyTipVerticalPlacement.KeyTipTopAtTargetCenter => finalSize.Height / 2,
            KeyTipVerticalPlacement.KeyTipTopAtTargetBottom => finalSize.Height,
            KeyTipVerticalPlacement.KeyTipCenterAtTargetTop => -size.Height / 2,
            KeyTipVerticalPlacement.KeyTipCenterAtTargetCenter => (finalSize.Height - size.Height) / 2,
            KeyTipVerticalPlacement.KeyTipCenterAtTargetBottom => finalSize.Height - (size.Height / 2),
            KeyTipVerticalPlacement.KeyTipBottomAtTargetTop => -size.Height,
            KeyTipVerticalPlacement.KeyTipBottomAtTargetCenter => (finalSize.Height / 2) - size.Height,
            _ => finalSize.Height - size.Height,
        };
        KeyTip.Arrange(new Rect(new Point(x + _placement.KeyTipHorizontalOffset, y + _placement.KeyTipVerticalOffset), size));
        return finalSize;
    }
}
