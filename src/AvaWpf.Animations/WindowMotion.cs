namespace AvaWpf.Animations;

/// <summary>The window open and close motion of a Windows era.</summary>
public enum WindowMotion
{
    /// <summary>No window motion (Windows 9x/2000 Classic and XP).</summary>
    None,

    /// <summary>Windows 7 DWM: scale 0.85 → 1 with a fade.</summary>
    Windows7,

    /// <summary>Windows 8 and 10: scale 0.95 → 1 with a fade.</summary>
    Windows10,

    /// <summary>Windows 11: scale 0.96 → 1 with a fade on the fast-out-slow-in curve.</summary>
    Windows11,
}
