using System;
using System.Collections.Generic;

namespace AvaWpf.Animations;

/// <summary>
/// A value that a <see cref="ChromeAnimator"/> animates. Chrome reads <see cref="Value"/> in its render pass. Starting
/// an animation supersedes the running one: the new animation starts from the value on screen, so rapid changes never
/// stack or replay.
/// </summary>
/// <typeparam name="T">The animated type.</typeparam>
public abstract class AnimatedValue<T> : IChromeChannel
{
    private readonly ChromeAnimator _animator;
    private ChromeKeyFrame<T>[] _frames = [];
    private T _start;
    private TimeSpan _beginTime;
    private bool _repeat;
    private TimeSpan? _startedAt;

    /// <summary>Initializes the value.</summary>
    protected AnimatedValue(ChromeAnimator animator, T initial)
    {
        _animator = animator;
        Value = initial;
        _start = initial;
    }

    /// <summary>The current value.</summary>
    public T Value { get; private set; }

    /// <summary>True while an animation runs.</summary>
    public bool IsAnimating { get; private set; }

    /// <summary>Stops any animation and sets the value.</summary>
    public void Set(T value)
    {
        Stop();
        Value = value;
        _animator.Invalidate();
    }

    /// <summary>Stops any animation and leaves the current value.</summary>
    public void Stop()
    {
        if (IsAnimating)
        {
            IsAnimating = false;
            _animator.Remove(this);
        }
    }

    /// <summary>Animates from the current value to <paramref name="to"/> over <paramref name="duration"/>, linearly.</summary>
    public void AnimateTo(T to, TimeSpan duration) => Animate([new ChromeKeyFrame<T>(to, duration)]);

    /// <summary>
    /// Runs key frames from the current value. <paramref name="beginTime"/> starts the timeline at that offset: a
    /// negative value starts it part way through, as WPF's <c>BeginTime</c> does. With <paramref name="repeatForever"/>,
    /// the timeline restarts from its first frame at the end of the last one.
    /// </summary>
    public void Animate(IReadOnlyList<ChromeKeyFrame<T>> frames, TimeSpan beginTime = default, bool repeatForever = false)
    {
        if (frames.Count == 0)
        {
            Stop();
            return;
        }

        _frames = new ChromeKeyFrame<T>[frames.Count];
        for (var i = 0; i < frames.Count; i++)
        {
            _frames[i] = frames[i];
        }

        _start = Value;
        _beginTime = beginTime;
        _repeat = repeatForever;
        _startedAt = null;

        if (!_animator.CanAnimate)
        {
            Stop();
            Value = _frames[^1].Value;
            _animator.Invalidate();
            return;
        }

        if (!IsAnimating)
        {
            IsAnimating = true;
            _animator.Add(this);
        }
    }

    /// <summary>Interpolates between two values.</summary>
    protected abstract T Lerp(T from, T to, double progress);

    void IChromeChannel.Finish()
    {
        if (_frames.Length > 0 && !_repeat)
        {
            Value = _frames[^1].Value;
        }

        IsAnimating = false;
    }

    bool IChromeChannel.Tick(TimeSpan now)
    {
        _startedAt ??= now;
        var scale = WpfAnimations.TimeScale <= 0 ? 1 : WpfAnimations.TimeScale;
        var elapsed = TimeSpan.FromTicks((long)((now - _startedAt.Value).Ticks / scale)) + _beginTime;
        if (elapsed < TimeSpan.Zero)
        {
            return true;
        }

        var total = _frames[^1].KeyTime;
        if (_repeat && total > TimeSpan.Zero)
        {
            elapsed = TimeSpan.FromTicks(elapsed.Ticks % total.Ticks);
        }
        else if (elapsed >= total)
        {
            Value = _frames[^1].Value;
            IsAnimating = false;
            return false;
        }

        var previousValue = _start;
        var previousTime = TimeSpan.Zero;
        foreach (var frame in _frames)
        {
            if (elapsed < frame.KeyTime)
            {
                if (frame.IsDiscrete)
                {
                    Value = previousValue;
                }
                else
                {
                    var span = (frame.KeyTime - previousTime).TotalMilliseconds;
                    var p = span <= 0 ? 1 : (elapsed - previousTime).TotalMilliseconds / span;
                    Value = Lerp(previousValue, frame.Value, Math.Clamp(p, 0, 1));
                }

                return true;
            }

            previousValue = frame.Value;
            previousTime = frame.KeyTime;
        }

        Value = _frames[^1].Value;
        return true;
    }
}
