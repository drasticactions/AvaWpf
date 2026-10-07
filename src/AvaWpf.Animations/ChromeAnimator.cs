using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaWpf.Animations;

/// <summary>
/// Advances the <see cref="AnimatedDouble"/> and <see cref="AnimatedColor"/> values of code-drawn chrome on the owner's
/// <see cref="TopLevel.RequestAnimationFrame"/>, invalidating the owner on each frame that changes a value.
/// </summary>
/// <remarks>
/// When motion is off (<see cref="WpfAnimations.IsMotionEnabled"/>) or the owner is not in a visual tree, an animation
/// jumps to its last key frame.
/// </remarks>
public sealed class ChromeAnimator
{
    private readonly Visual _owner;
    private readonly List<IChromeChannel> _running = new();
    private bool _frameRequested;

    /// <summary>Initializes an animator for <paramref name="owner"/>.</summary>
    public ChromeAnimator(Visual owner)
    {
        _owner = owner;
    }

    /// <summary>True while any value animates.</summary>
    public bool IsAnimating => _running.Count > 0;

    internal bool CanAnimate => _owner.IsAttachedToVisualTree() && WpfAnimations.IsMotionEnabled(_owner) && TopLevel.GetTopLevel(_owner) is not null;

    /// <summary>Creates an animated double owned by this animator.</summary>
    public AnimatedDouble CreateDouble(double initial) => new(this, initial);

    /// <summary>Creates an animated color owned by this animator.</summary>
    public AnimatedColor CreateColor(Avalonia.Media.Color initial) => new(this, initial);

    internal void Add(IChromeChannel channel)
    {
        if (!_running.Contains(channel))
        {
            _running.Add(channel);
        }

        RequestFrame();
    }

    internal void Remove(IChromeChannel channel) => _running.Remove(channel);

    internal void Invalidate() => _owner.InvalidateVisual();

    private void RequestFrame()
    {
        if (_frameRequested)
        {
            return;
        }

        if (TopLevel.GetTopLevel(_owner) is { } top)
        {
            _frameRequested = true;
            top.RequestAnimationFrame(OnFrame);
        }
    }

    private void OnFrame(TimeSpan now)
    {
        _frameRequested = false;
        var changed = false;
        for (var i = _running.Count - 1; i >= 0; i--)
        {
            if (i >= _running.Count)
            {
                continue;
            }

            var running = _running[i].Tick(now, out var channelChanged);
            changed |= channelChanged;
            if (!running)
            {
                _running.RemoveAt(i);
            }
        }

        // A hold between key frames, or a lowered frame rate, leaves the values as drawn; skip the render.
        if (changed)
        {
            _owner.InvalidateVisual();
        }

        if (_running.Count > 0)
        {
            if (!_owner.IsAttachedToVisualTree())
            {
                // Jump the running values to their end, so a later animation of the same value starts afresh.
                foreach (var channel in _running.ToArray())
                {
                    channel.Finish();
                }

                _running.Clear();
                return;
            }

            RequestFrame();
        }
    }
}
