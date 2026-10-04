// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonImageSize.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>The image size a Ribbon control shows in one of its size variants.</summary>
public enum RibbonImageSize
{
    /// <summary>No image is shown.</summary>
    Collapsed,

    /// <summary>The 16 × 16 image (<c>SmallImageSource</c>).</summary>
    Small,

    /// <summary>The 32 × 32 image (<c>LargeImageSource</c>).</summary>
    Large,
}
