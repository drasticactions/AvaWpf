using Avalonia.Interactivity;

namespace AvaWpf.Ribbon;

/// <summary>
/// The data of <see cref="RibbonGallery.PreviewRequestedEvent"/>.
/// </summary>
public class RibbonGalleryPreviewEventArgs : RoutedEventArgs
{
    /// <summary>Initializes the arguments.</summary>
    /// <param name="item">The item to preview, or the item whose preview ends.</param>
    /// <param name="isPreviewing">True when a preview should start, false when it should be reverted.</param>
    public RibbonGalleryPreviewEventArgs(object? item, bool isPreviewing)
        : base(RibbonGallery.PreviewRequestedEvent)
    {
        Item = item;
        IsPreviewing = isPreviewing;
    }

    /// <summary>The item (the data item, or the <see cref="RibbonGalleryItem"/> when it is its own item).</summary>
    public object? Item { get; }

    /// <summary>True when the pointer moved onto <see cref="Item"/>; false when the preview should be reverted.</summary>
    public bool IsPreviewing { get; }
}
