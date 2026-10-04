using Avalonia.Animation.Easings;

namespace AvaWpf.Animations;

/// <summary>The easing curves of the WPF theme motion as Avalonia <see cref="Easing"/> instances.</summary>
public static class WpfEasing
{
    /// <summary>WPF Fluent's <c>ControlFastOutSlowInKeySpline</c>, <c>KeySpline 0,0,0,1</c>.</summary>
    public static readonly Easing FastOutSlowIn = Bezier(0, 0, 0, 1);

    /// <summary>Linear, as a WPF <c>DoubleAnimation</c> without an easing function.</summary>
    public static readonly Easing Linear = new LinearEasing();

    /// <summary>A cubic ease-out (<c>cubic-bezier(0.215, 0.61, 0.355, 1)</c>), for the Windows 7 window open.</summary>
    public static readonly Easing EaseOut = Bezier(0.215, 0.61, 0.355, 1);

    /// <summary>A cubic ease-in (<c>cubic-bezier(0.55, 0.055, 0.675, 0.19)</c>), for window close.</summary>
    public static readonly Easing EaseIn = Bezier(0.55, 0.055, 0.675, 0.19);

    /// <summary>The Windows 10 window curve, <c>cubic-bezier(0.1, 0.9, 0.2, 1)</c>.</summary>
    public static readonly Easing Decelerate = Bezier(0.1, 0.9, 0.2, 1);

    private static SplineEasing Bezier(double x1, double y1, double x2, double y2) =>
        new() { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };
}
