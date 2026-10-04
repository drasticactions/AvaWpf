using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.Animations;

/// <summary>Global switches for all theme, chrome, popup and window motion.</summary>
public static class WpfAnimations
{
    /// <summary>
    /// The resource key that turns theme motion off for a subtree, as Windows' <c>SPI_GETCLIENTAREAANIMATION</c>. It is
    /// false for the Classic high-contrast schemes.
    /// </summary>
    public const string ClientAreaAnimationKey = "SystemParameters.ClientAreaAnimation";

    /// <summary>When false, every animation jumps to its end state. Default true.</summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Multiplies every duration and delay. 1 is real time; 0 jumps to the end state (tests set this). Default 1.
    /// </summary>
    public static double TimeScale { get; set; } = 1;

    /// <summary>True when theme motion may play on <paramref name="visual"/>.</summary>
    public static bool IsMotionEnabled(Visual visual) =>
        IsEnabled && TimeScale > 0 && IsClientAreaAnimationEnabled(visual);

    /// <summary>Reads <see cref="ClientAreaAnimationKey"/> for <paramref name="visual"/>; a missing key counts as true.</summary>
    public static bool IsClientAreaAnimationEnabled(Visual visual)
    {
        if (visual is IResourceHost host && host.TryFindResource(ClientAreaAnimationKey, visual.ActualThemeVariant, out var value) && value is bool b)
        {
            return b;
        }

        return true;
    }
}
