using System;

namespace AvaWpf.Animations;

/// <summary>One running animation that a <see cref="ChromeAnimator"/> advances each frame.</summary>
internal interface IChromeChannel
{
    /// <summary>Advances to <paramref name="now"/>. Returns false once the animation has ended.</summary>
    bool Tick(TimeSpan now);

    /// <summary>Ends the animation at its last key frame.</summary>
    void Finish();
}
