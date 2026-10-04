using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace AvaWpf.Animations;

/// <summary>
/// Plays the window open and close motion of a Windows era on the drawn root of a window. The motion shows only on a
/// transparent window.
/// </summary>
public static class WindowAnimations
{
    /// <summary>Plays the open motion: scale up from the era's start scale while fading in.</summary>
    public static Task PlayOpen(Control root, WindowMotion motion)
    {
        if (!Describe(motion, open: true, out var duration, out var scale, out var easing))
        {
            return Task.CompletedTask;
        }

        root.RenderTransformOrigin = RelativePoint.Center;
        return AnimationRunner.Run(
            root,
            AnimationRunner.Opacity(0, 1, 0, duration.TotalMilliseconds, easing),
            AnimationRunner.Transform(AnimationRunner.Scale(scale), AnimationRunner.Identity, 0, duration.TotalMilliseconds, easing));
    }

    /// <summary>Plays the close motion (also used before a minimize): the open motion in reverse.</summary>
    public static Task PlayClose(Control root, WindowMotion motion)
    {
        if (!Describe(motion, open: false, out var duration, out var scale, out var easing))
        {
            return Task.CompletedTask;
        }

        root.RenderTransformOrigin = RelativePoint.Center;
        return AnimationRunner.Run(
            root,
            AnimationRunner.Opacity(1, 0, 0, duration.TotalMilliseconds, easing),
            AnimationRunner.Transform(AnimationRunner.Identity, AnimationRunner.Scale(scale), 0, duration.TotalMilliseconds, easing));
    }

    /// <summary>Clears what <see cref="PlayClose"/> left on the root, so a restored window shows again.</summary>
    public static void Reset(Control root)
    {
        root.ClearValue(Visual.OpacityProperty);
        root.ClearValue(Visual.RenderTransformProperty);
    }

    private static bool Describe(WindowMotion motion, bool open, out TimeSpan duration, out double scale, out Easing easing)
    {
        switch (motion)
        {
            case WindowMotion.Windows7:
                duration = open ? WindowTimings.Windows7Open : WindowTimings.Windows7Close;
                scale = WindowTimings.Windows7Scale;
                easing = open ? WpfEasing.EaseOut : WpfEasing.EaseIn;
                return true;
            case WindowMotion.Windows10:
                duration = open ? WindowTimings.Windows10Open : WindowTimings.Windows10Close;
                scale = WindowTimings.Windows10Scale;
                easing = open ? WpfEasing.Decelerate : WpfEasing.EaseIn;
                return true;
            case WindowMotion.Windows11:
                duration = open ? WindowTimings.Windows11Open : WindowTimings.Windows11Close;
                scale = WindowTimings.Windows11Scale;
                easing = WpfEasing.FastOutSlowIn;
                return true;
            default:
                duration = TimeSpan.Zero;
                scale = 1;
                easing = WpfEasing.Linear;
                return false;
        }
    }
}
