namespace AvaWpf.Animations;

/// <summary>An animated <see cref="double"/>, typically a brush opacity.</summary>
public sealed class AnimatedDouble : AnimatedValue<double>
{
    internal AnimatedDouble(ChromeAnimator animator, double initial) : base(animator, initial)
    {
    }

    /// <inheritdoc/>
    protected override double Lerp(double from, double to, double progress) => from + ((to - from) * progress);
}
