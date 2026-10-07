using System;

namespace AvaWpf.Animations;

/// <summary>
/// The durations and key times of the chrome code animations, copied from WPF's
/// <c>Microsoft.Windows.Themes</c> sources.
/// </summary>
public static class ChromeTimings
{
    /// <summary>Aero chrome: hover fade-in, also the defaulted border and ScrollChrome pressed/enabled fades (0.3 s).</summary>
    public static readonly TimeSpan HoverIn = TimeSpan.FromSeconds(0.3);

    /// <summary>Aero chrome: hover fade-out, also the defaulted border and ScrollChrome disabled fades (0.2 s).</summary>
    public static readonly TimeSpan HoverOut = TimeSpan.FromSeconds(0.2);

    /// <summary>Aero <c>ButtonChrome</c>: the pressed look fades in and out over 0.1 s.</summary>
    public static readonly TimeSpan Press = TimeSpan.FromSeconds(0.1);

    /// <summary>Aero <c>BulletChrome</c>: the pressed look fades in and out over 0.3 s (<c>OnRenderPressedChanged</c>).</summary>
    public static readonly TimeSpan BulletPress = TimeSpan.FromSeconds(0.3);

    /// <summary>
    /// Aero <c>BulletChrome</c>: the check mark, the dot and the indeterminate look fade over 0.3 s
    /// (<c>OnIsCheckedChanged</c>, <c>AnimateToIndeterminate</c>).
    /// </summary>
    public static readonly TimeSpan BulletCheck = TimeSpan.FromSeconds(0.3);

    /// <summary>WPF's <c>Duration.Automatic</c> for a From/To animation (1 s), used by one Aero <c>BulletChrome</c> color animation.</summary>
    public static readonly TimeSpan Automatic = TimeSpan.FromSeconds(1.0);

    /// <summary>Aero <c>ButtonChrome</c> default pulse: the overlay reaches full opacity at 0.5 s.</summary>
    public static readonly TimeSpan DefaultPulseUp = TimeSpan.FromSeconds(0.5);

    /// <summary>Aero <c>ButtonChrome</c> default pulse: the overlay holds until 0.75 s.</summary>
    public static readonly TimeSpan DefaultPulseHold = TimeSpan.FromSeconds(0.75);

    /// <summary>Aero <c>ButtonChrome</c> default pulse: the overlay is gone at 2 s, then the pulse repeats.</summary>
    public static readonly TimeSpan DefaultPulsePeriod = TimeSpan.FromSeconds(2.0);

    /// <summary>Aero <c>ButtonChrome</c> resumed default pulse: the hold after the overlay is back at full opacity.</summary>
    public static readonly TimeSpan DefaultPulseResumeHold = TimeSpan.FromSeconds(0.25);

    /// <summary>Aero <c>ButtonChrome</c> resumed default pulse: the fade-out after the overlay is back at full opacity.</summary>
    public static readonly TimeSpan DefaultPulseResumeFall = TimeSpan.FromSeconds(1.5);

    /// <summary>Aero <c>ButtonChrome</c> default pulse: the <c>DesiredFrameRate</c> of WPF's pulse storyboard.</summary>
    public const int DefaultPulseFrameRate = 10;
}
