// Ported from WPF $W/Themes/Shared/Microsoft/Windows/Themes/PlatformCulture.cs (MIT, see NOTICE.md).
using System.Globalization;
using Avalonia.Media;

namespace AvaWpf.Chrome;

/// <summary>
/// Properties of the platform culture, as WPF's <c>PlatformCulture</c>. Uses <see cref="CultureInfo.CurrentUICulture"/>
/// at first use and caches it for the process.
/// </summary>
internal static class PlatformCulture
{
    private static CultureInfo? s_platformCulture;

    /// <summary>
    /// <see cref="FlowDirection.RightToLeft"/> when the platform culture writes right to left, otherwise
    /// <see cref="FlowDirection.LeftToRight"/>.
    /// </summary>
    public static FlowDirection FlowDirection
    {
        get
        {
            s_platformCulture ??= CultureInfo.CurrentUICulture;
            return s_platformCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
    }
}
