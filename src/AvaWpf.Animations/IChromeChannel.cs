using System;

namespace AvaWpf.Animations;

/// <summary>One running animation that a <see cref="ChromeAnimator"/> advances each frame.</summary>
internal interface IChromeChannel
{
    /// <summary>
    /// Advances to <paramref name="now"/>. Returns false once the animation has ended. <paramref name="changed"/> is
    /// true when the value moved, so the owner needs a new render.
    /// </summary>
    bool Tick(TimeSpan now, out bool changed);

    /// <summary>Ends the animation at its last key frame.</summary>
    void Finish();
}
