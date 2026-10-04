// Ported from WPF $R/Microsoft/Windows/Controls/ActivatingKeyTipEventArgs.cs (MIT, see NOTICE.md).
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaWpf.Ribbon;

/// <summary>
/// The data of <see cref="KeyTipService.ActivatingKeyTipEvent"/>, raised on an element just before its KeyTip shows.
/// Handlers move or hide the KeyTip.
/// </summary>
public class ActivatingKeyTipEventArgs : RoutedEventArgs
{
    /// <summary>Initializes the arguments: the KeyTip is centered below the target's center.</summary>
    public ActivatingKeyTipEventArgs()
        : base(KeyTipService.ActivatingKeyTipEvent)
    {
    }

    /// <summary>The horizontal placement. Default <see cref="KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter"/>.</summary>
    public KeyTipHorizontalPlacement KeyTipHorizontalPlacement { get; set; } = KeyTipHorizontalPlacement.KeyTipCenterAtTargetCenter;

    /// <summary>The vertical placement. Default <see cref="KeyTipVerticalPlacement.KeyTipTopAtTargetBottom"/>.</summary>
    public KeyTipVerticalPlacement KeyTipVerticalPlacement { get; set; } = KeyTipVerticalPlacement.KeyTipTopAtTargetBottom;

    /// <summary>An extra horizontal offset in pixels.</summary>
    public double KeyTipHorizontalOffset { get; set; }

    /// <summary>An extra vertical offset in pixels.</summary>
    public double KeyTipVerticalOffset { get; set; }

    /// <summary>Whether the KeyTip shows. Default true.</summary>
    public bool KeyTipVisibility { get; set; } = true;

    /// <summary>The element the KeyTip is placed against. Null is the element itself.</summary>
    public Control? PlacementTarget { get; set; }
}
