using System;

namespace AvaWpf.Animations;

/// <summary>The durations of WPF Fluent's storyboards, from <c>PresentationFramework.Fluent/Resources/Variables.xaml</c>.</summary>
public static class FluentTimings
{
    /// <summary><c>ControlNormalAnimationDuration</c>: 250 ms.</summary>
    public static readonly TimeSpan Normal = TimeSpan.FromMilliseconds(250);

    /// <summary><c>ControlFastAnimationDuration</c>: 167 ms.</summary>
    public static readonly TimeSpan Fast = TimeSpan.FromMilliseconds(167);

    /// <summary><c>ControlFastAnimationAfterDuration</c>: 168 ms, the begin time of the second stage of a two-stage storyboard.</summary>
    public static readonly TimeSpan FastAfter = TimeSpan.FromMilliseconds(168);

    /// <summary><c>ControlFasterAnimationDuration</c>: 83 ms.</summary>
    public static readonly TimeSpan Faster = TimeSpan.FromMilliseconds(83);
}
