// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonControlSizeDefinition.cs (MIT, see NOTICE.md).
namespace AvaWpf.Ribbon;

/// <summary>
/// One size variant of a Ribbon control: the image size, whether the label shows, and the width. A
/// <see cref="RibbonGroupSizeDefinition"/> lists one per control of its group. Treat an instance as a value: change a
/// control's size by assigning another definition, not by editing one in use.
/// </summary>
public sealed class RibbonControlSizeDefinition
{
    /// <summary>The image size. Default <see cref="RibbonImageSize.Large"/>.</summary>
    public RibbonImageSize ImageSize { get; set; } = RibbonImageSize.Large;

    /// <summary>Whether the label shows. Default true.</summary>
    public bool IsLabelVisible { get; set; } = true;

    /// <summary>Whether the control is hidden in this variant. Default false.</summary>
    public bool IsCollapsed { get; set; }

    /// <summary>The width. Default <see cref="RibbonControlLength.Auto"/>.</summary>
    public RibbonControlLength Width { get; set; } = RibbonControlLength.Auto;

    /// <summary>The minimum width. Default <see cref="RibbonControlLength.Auto"/>.</summary>
    public RibbonControlLength MinWidth { get; set; } = RibbonControlLength.Auto;

    /// <summary>The maximum width. Default <see cref="RibbonControlLength.Auto"/>.</summary>
    public RibbonControlLength MaxWidth { get; set; } = RibbonControlLength.Auto;

    /// <summary>Creates a copy.</summary>
    /// <returns>The copy.</returns>
    public RibbonControlSizeDefinition Clone() => new()
    {
        ImageSize = ImageSize,
        IsLabelVisible = IsLabelVisible,
        IsCollapsed = IsCollapsed,
        Width = Width,
        MinWidth = MinWidth,
        MaxWidth = MaxWidth,
    };

    /// <inheritdoc/>
    public override string ToString() => IsCollapsed ? "Collapsed" : ImageSize + (IsLabelVisible ? "+Label" : string.Empty);
}
