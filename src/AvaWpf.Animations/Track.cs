using System;
using Avalonia;
using Avalonia.Animation.Easings;

namespace AvaWpf.Animations;

/// <summary>
/// One animated property on one control: from → to over a duration after a delay.
/// </summary>
internal readonly record struct Track(
    AvaloniaProperty Property,
    object From,
    object To,
    TimeSpan Delay,
    TimeSpan Duration,
    Easing Easing);
