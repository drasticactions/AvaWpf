using Avalonia.Media;

namespace AvaWpf.Animations;

/// <summary>An animated <see cref="Color"/>, typically a gradient stop, interpolated per channel as WPF's <c>ColorAnimation</c> does.</summary>
public sealed class AnimatedColor : AnimatedValue<Color>
{
    internal AnimatedColor(ChromeAnimator animator, Color initial) : base(animator, initial)
    {
    }

    /// <inheritdoc/>
    protected override Color Lerp(Color from, Color to, double progress) => Color.FromArgb(
        Channel(from.A, to.A, progress),
        Channel(from.R, to.R, progress),
        Channel(from.G, to.G, progress),
        Channel(from.B, to.B, progress));

    private static byte Channel(byte from, byte to, double progress) =>
        (byte)System.Math.Clamp(System.Math.Round(from + ((to - from) * progress)), 0, 255);
}
