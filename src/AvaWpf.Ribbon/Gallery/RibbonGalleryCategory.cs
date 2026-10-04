// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonGalleryCategory.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace AvaWpf.Ribbon;

/// <summary>
/// A titled group of <see cref="RibbonGalleryItem"/>s in a <see cref="RibbonGallery"/>; in an
/// <see cref="InRibbonGallery"/> row it gets <c>:inribbon</c> and hides its title.
/// </summary>
[Avalonia.Controls.Metadata.PseudoClasses(":inribbon")]
public class RibbonGalleryCategory : HeaderedItemsControl
{
    /// <summary>Defines the <see cref="IsHeaderVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsHeaderVisibleProperty =
        AvaloniaProperty.Register<RibbonGalleryCategory, bool>(nameof(IsHeaderVisible), true);

    /// <summary>Defines the <see cref="MinColumnCount"/> property.</summary>
    public static readonly StyledProperty<int> MinColumnCountProperty =
        AvaloniaProperty.Register<RibbonGalleryCategory, int>(nameof(MinColumnCount), 1);

    /// <summary>Defines the <see cref="MaxColumnCount"/> property.</summary>
    public static readonly StyledProperty<int> MaxColumnCountProperty =
        AvaloniaProperty.Register<RibbonGalleryCategory, int>(nameof(MaxColumnCount), int.MaxValue);

    /// <summary>Defines the <see cref="HorizontalContentAlignment"/> property.</summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<RibbonGalleryCategory>();

    /// <summary>Defines the <see cref="VerticalContentAlignment"/> property.</summary>
    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<RibbonGalleryCategory>();

    /// <summary>Defines the <see cref="IsFilteredOut"/> property.</summary>
    public static readonly DirectProperty<RibbonGalleryCategory, bool> IsFilteredOutProperty =
        AvaloniaProperty.RegisterDirect<RibbonGalleryCategory, bool>(nameof(IsFilteredOut), o => o.IsFilteredOut);

    private bool _isFilteredOut;

    /// <summary>Whether the category title shows. Default true.</summary>
    public bool IsHeaderVisible
    {
        get => GetValue(IsHeaderVisibleProperty);
        set => SetValue(IsHeaderVisibleProperty, value);
    }

    /// <summary>The fewest item columns. Default 1.</summary>
    public int MinColumnCount
    {
        get => GetValue(MinColumnCountProperty);
        set => SetValue(MinColumnCountProperty, value);
    }

    /// <summary>The most item columns. Default unlimited.</summary>
    public int MaxColumnCount
    {
        get => GetValue(MaxColumnCountProperty);
        set => SetValue(MaxColumnCountProperty, value);
    }

    /// <summary>The horizontal alignment of the item content; the gallery's unless set.</summary>
    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    /// <summary>The vertical alignment of the item content; the gallery's unless set.</summary>
    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    /// <summary>Whether the gallery's filter hides the category.</summary>
    public bool IsFilteredOut
    {
        get => _isFilteredOut;
        internal set
        {
            SetAndRaise(IsFilteredOutProperty, ref _isFilteredOut, value);
            IsVisible = !value;
        }
    }

    /// <summary>The gallery the category belongs to.</summary>
    public RibbonGallery? Gallery => this.FindLogicalAncestorOfType<RibbonGallery>();

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<RibbonGalleryItem>(item, out recycleKey);

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new RibbonGalleryItem();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is RibbonGalleryItem galleryItem && Gallery is { } gallery)
        {
            if (container != item && ItemTemplate is null && gallery.GalleryItemTemplate is { } template)
            {
                galleryItem.ContentTemplate = template;
            }

            galleryItem.IsSelected = Equals(gallery.SelectedItem, container == item ? container : item);
        }

        if (container is RibbonGalleryItem contentItem)
        {
            PassContentAlignment(contentItem);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HorizontalContentAlignmentProperty || change.Property == VerticalContentAlignmentProperty)
        {
            foreach (var container in GetRealizedContainers())
            {
                if (container is RibbonGalleryItem item)
                {
                    PassContentAlignment(item);
                }
            }
        }
    }

    private void PassContentAlignment(RibbonGalleryItem item)
    {
        item.SetValue(ContentControl.HorizontalContentAlignmentProperty, HorizontalContentAlignment, BindingPriority.Template);
        item.SetValue(ContentControl.VerticalContentAlignmentProperty, VerticalContentAlignment, BindingPriority.Template);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The drop-down of an InRibbonGallery is another visual tree, so only the in-ribbon row finds the gallery.
        PseudoClasses.Set(":inribbon", this.FindAncestorOfType<InRibbonGallery>() is not null);
    }
}
