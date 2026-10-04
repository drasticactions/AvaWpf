using System;

namespace AvaWpf.Animations;

/// <summary>
/// One key frame of a chrome animation: the value at <see cref="KeyTime"/>, reached linearly from the previous frame,
/// or held from the previous frame and then jumped to when <see cref="IsDiscrete"/> is set (WPF's
/// <c>LinearDoubleKeyFrame</c> and <c>DiscreteDoubleKeyFrame</c>).
/// </summary>
/// <typeparam name="T">The animated type.</typeparam>
/// <param name="Value">The value at <see cref="KeyTime"/>.</param>
/// <param name="KeyTime">The time of the frame from the start of the animation.</param>
/// <param name="IsDiscrete">True for a discrete frame.</param>
public readonly record struct ChromeKeyFrame<T>(T Value, TimeSpan KeyTime, bool IsDiscrete = false);
