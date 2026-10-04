// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonToolTip.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;

namespace AvaWpf.Ribbon;

/// <summary>
/// The rich Ribbon tool tip with a title, description, image and footer, built from a control's ToolTip* properties.
/// </summary>
[PseudoClasses(":hasheader", ":hasfooter", ":hasimage")]
public class RibbonToolTip : ToolTip
{
    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<RibbonToolTip, string?>(nameof(Title));

    /// <summary>Defines the <see cref="Description"/> property.</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<RibbonToolTip, string?>(nameof(Description));

    /// <summary>Defines the <see cref="ImageSource"/> property.</summary>
    public static readonly StyledProperty<IImage?> ImageSourceProperty =
        AvaloniaProperty.Register<RibbonToolTip, IImage?>(nameof(ImageSource));

    /// <summary>Defines the <see cref="FooterTitle"/> property.</summary>
    public static readonly StyledProperty<string?> FooterTitleProperty =
        AvaloniaProperty.Register<RibbonToolTip, string?>(nameof(FooterTitle));

    /// <summary>Defines the <see cref="FooterDescription"/> property.</summary>
    public static readonly StyledProperty<string?> FooterDescriptionProperty =
        AvaloniaProperty.Register<RibbonToolTip, string?>(nameof(FooterDescription));

    /// <summary>Defines the <see cref="FooterImageSource"/> property.</summary>
    public static readonly StyledProperty<IImage?> FooterImageSourceProperty =
        AvaloniaProperty.Register<RibbonToolTip, IImage?>(nameof(FooterImageSource));

    /// <summary>The bold title.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The description.</summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>The image beside the description.</summary>
    public IImage? ImageSource
    {
        get => GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    /// <summary>The footer title.</summary>
    public string? FooterTitle
    {
        get => GetValue(FooterTitleProperty);
        set => SetValue(FooterTitleProperty, value);
    }

    /// <summary>The footer description.</summary>
    public string? FooterDescription
    {
        get => GetValue(FooterDescriptionProperty);
        set => SetValue(FooterDescriptionProperty, value);
    }

    /// <summary>The footer image.</summary>
    public IImage? FooterImageSource
    {
        get => GetValue(FooterImageSourceProperty);
        set => SetValue(FooterImageSourceProperty, value);
    }

    /// <summary>True for a tool tip a Ribbon control built from its own properties (replaced when they change).</summary>
    internal bool IsGenerated { get; init; }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty || change.Property == FooterTitleProperty || change.Property == FooterDescriptionProperty ||
            change.Property == FooterImageSourceProperty || change.Property == ImageSourceProperty)
        {
            PseudoClasses.Set(":hasheader", !string.IsNullOrEmpty(Title));
            PseudoClasses.Set(":hasfooter", !string.IsNullOrEmpty(FooterTitle) || !string.IsNullOrEmpty(FooterDescription) || FooterImageSource is not null);
            PseudoClasses.Set(":hasimage", ImageSource is not null);
        }
    }
}
