using System;

namespace AvaWpf.Animations;

/// <summary>The window motion of each era. The values are estimates; the DWM does not publish them.</summary>
public static class WindowTimings
{
    /// <summary>Windows 7 open: about 250 ms.</summary>
    public static readonly TimeSpan Windows7Open = TimeSpan.FromMilliseconds(250);

    /// <summary>Windows 7 close: about 200 ms.</summary>
    public static readonly TimeSpan Windows7Close = TimeSpan.FromMilliseconds(200);

    /// <summary>Windows 7 start scale: 0.85.</summary>
    public const double Windows7Scale = 0.85;

    /// <summary>Windows 8/10 open: about 167 ms.</summary>
    public static readonly TimeSpan Windows10Open = TimeSpan.FromMilliseconds(167);

    /// <summary>Windows 8/10 close: about 133 ms.</summary>
    public static readonly TimeSpan Windows10Close = TimeSpan.FromMilliseconds(133);

    /// <summary>Windows 8/10 start scale: 0.95.</summary>
    public const double Windows10Scale = 0.95;

    /// <summary>Windows 11 open: about 200 ms.</summary>
    public static readonly TimeSpan Windows11Open = TimeSpan.FromMilliseconds(200);

    /// <summary>Windows 11 close: about 150 ms.</summary>
    public static readonly TimeSpan Windows11Close = TimeSpan.FromMilliseconds(150);

    /// <summary>Windows 11 start scale: 0.96.</summary>
    public const double Windows11Scale = 0.96;
}
