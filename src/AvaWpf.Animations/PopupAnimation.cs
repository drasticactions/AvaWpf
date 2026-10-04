using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace AvaWpf.Animations;

/// <summary>
/// WPF's <c>Popup.PopupAnimation</c> as an attached property, set on a <see cref="Popup"/> or on popup content such as
/// a <see cref="ContextMenu"/>, <see cref="ToolTip"/> or flyout presenter.
/// </summary>
/// <remarks>
/// Matches WPF's <c>PopupRoot.SetupFadeAnimation</c> and <c>SetupTranslateAnimations</c>: linear, <see cref="Duration"/>
/// long. Slide translates by the content height away from the target; Scroll also translates by the width, from the
/// right when the flow direction differs from the target's.
/// </remarks>
public static class PopupAnimation
{
    /// <summary>WPF's <c>Popup.AnimationDelay</c>: every popup animation lasts 150 ms.</summary>
    public static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(150);

    /// <summary>Defines the <c>Kind</c> attached property.</summary>
    public static readonly AttachedProperty<PopupAnimationKind> KindProperty =
        AvaloniaProperty.RegisterAttached<Control, PopupAnimationKind>("Kind", typeof(PopupAnimation));

    private static readonly AttachedProperty<CancellationTokenSource?> s_runProperty =
        AvaloniaProperty.RegisterAttached<Control, CancellationTokenSource?>("PopupAnimationRun", typeof(PopupAnimation));

    static PopupAnimation()
    {
        KindProperty.Changed.AddClassHandler<Control>(OnKindChanged);
    }

    /// <summary>Gets the popup animation of a control.</summary>
    public static PopupAnimationKind GetKind(Control control) => control.GetValue(KindProperty);

    /// <summary>Sets the popup animation of a control.</summary>
    public static void SetKind(Control control, PopupAnimationKind value) => control.SetValue(KindProperty, value);

    private static void OnKindChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (control is Popup popup)
        {
            popup.Opened -= OnPopupOpened;
            if (e.GetNewValue<PopupAnimationKind>() != PopupAnimationKind.None)
            {
                popup.Opened += OnPopupOpened;
            }
        }
        else
        {
            control.AttachedToVisualTree -= OnShown;
            if (e.GetNewValue<PopupAnimationKind>() != PopupAnimationKind.None)
            {
                control.AttachedToVisualTree += OnShown;
            }
        }
    }

    private static void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is Popup { Child: Control child } popup)
        {
            Play(child, GetKind(popup), popup.PlacementTarget ?? popup.GetVisualParent() as Control, OpensAbove(popup.Placement, popup.PlacementGravity));
        }
    }

    private static void OnShown(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is Control control)
        {
            var popup = control.FindLogicalAncestorOfType<Popup>();
            var above = control switch
            {
                ContextMenu menu => OpensAbove(menu.Placement, menu.PlacementGravity),
                _ when popup is not null => OpensAbove(popup.Placement, popup.PlacementGravity),
                _ => false,
            };
            Play(control, GetKind(control), popup?.PlacementTarget, above);
        }
    }

    /// <summary>
    /// True when a popup with this placement opens above its target (WPF's <c>AnimateFromBottom</c>). Uses the placement,
    /// not screen positions, because Wayland does not report window positions.
    /// </summary>
    public static bool OpensAbove(PlacementMode placement, PopupGravity gravity) => placement switch
    {
        PlacementMode.Top or PlacementMode.TopEdgeAlignedLeft or PlacementMode.TopEdgeAlignedRight => true,
        PlacementMode.AnchorAndGravity => gravity is PopupGravity.Top or PopupGravity.TopLeft or PopupGravity.TopRight,
        _ => false,
    };

    /// <summary>
    /// Plays the opening motion of <paramref name="kind"/> on popup <paramref name="content"/> shown next to
    /// <paramref name="target"/>. A second call cancels a running one.
    /// </summary>
    public static void Play(Control content, PopupAnimationKind kind, Control? target, bool opensAbove = false)
    {
        content.GetValue(s_runProperty)?.Cancel();
        if (kind == PopupAnimationKind.None || !WpfAnimations.IsMotionEnabled(content))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        content.SetValue(s_runProperty, cts);

        if (kind == PopupAnimationKind.Fade)
        {
            _ = AnimationRunner.Run(content, [AnimationRunner.Opacity(0, 1, 0, Duration.TotalMilliseconds, WpfEasing.Linear)], cts.Token);
            return;
        }

        // Hide the content until it is laid out, so the first frame is already offset.
        content.Opacity = 0;
        Dispatcher.UIThread.Post(() =>
        {
            content.ClearValue(Visual.OpacityProperty);
            if (cts.IsCancellationRequested)
            {
                return;
            }

            var size = content.Bounds.Size;
            var fromBottom = opensAbove;
            var y = fromBottom ? size.Height : -size.Height;
            var x = 0.0;
            if (kind == PopupAnimationKind.Scroll)
            {
                var fromRight = target is not null && target.FlowDirection != content.FlowDirection;
                x = fromRight ? size.Width : -size.Width;
            }

            _ = AnimationRunner.Run(
                content,
                [AnimationRunner.Transform(AnimationRunner.Translate(x, y), AnimationRunner.Identity, 0, Duration.TotalMilliseconds, WpfEasing.Linear)],
                cts.Token);
        }, DispatcherPriority.Render);
    }
}
