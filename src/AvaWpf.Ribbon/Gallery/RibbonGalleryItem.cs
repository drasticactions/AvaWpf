// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonGalleryItem.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>An item of a <see cref="RibbonGalleryCategory"/>. Clicking it selects it in the gallery.</summary>
[PseudoClasses(":selected", ":highlighted", ":pressed")]
public class RibbonGalleryItem : ContentControl
{
    /// <summary>Defines the <see cref="IsSelected"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<RibbonGalleryItem, bool>(nameof(IsSelected));

    /// <summary>Defines the <see cref="IsHighlighted"/> property.</summary>
    public static readonly DirectProperty<RibbonGalleryItem, bool> IsHighlightedProperty =
        AvaloniaProperty.RegisterDirect<RibbonGalleryItem, bool>(nameof(IsHighlighted), o => o.IsHighlighted);

    /// <summary>Defines the <see cref="KeyTip"/> property.</summary>
    public static readonly AttachedProperty<string?> KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner<RibbonGalleryItem>();

    /// <summary>Defines the <see cref="ToolTipTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipTitleProperty = RibbonControlService.ToolTipTitleProperty.AddOwner<RibbonGalleryItem>();

    /// <summary>Defines the <see cref="ToolTipDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipDescriptionProperty = RibbonControlService.ToolTipDescriptionProperty.AddOwner<RibbonGalleryItem>();

    /// <summary>Defines the <see cref="ToolTipImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipImageSourceProperty = RibbonControlService.ToolTipImageSourceProperty.AddOwner<RibbonGalleryItem>();

    private bool _isHighlighted;

    static RibbonGalleryItem()
    {
        KeyTipService.KeyTipAccessedEvent.AddClassHandler<RibbonGalleryItem>((i, e) => i.OnKeyTipAccessed(e));
    }

    /// <summary>Whether the item is the gallery's selected item.</summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>Whether the pointer is over the item (the item being previewed).</summary>
    public bool IsHighlighted
    {
        get => _isHighlighted;
        private set => SetAndRaise(IsHighlightedProperty, ref _isHighlighted, value);
    }

    /// <summary>The KeyTip.</summary>
    public string? KeyTip
    {
        get => GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>The title of the rich tool tip.</summary>
    public string? ToolTipTitle
    {
        get => GetValue(ToolTipTitleProperty);
        set => SetValue(ToolTipTitleProperty, value);
    }

    /// <summary>The description of the rich tool tip.</summary>
    public string? ToolTipDescription
    {
        get => GetValue(ToolTipDescriptionProperty);
        set => SetValue(ToolTipDescriptionProperty, value);
    }

    /// <summary>The image of the rich tool tip.</summary>
    public IImage? ToolTipImageSource
    {
        get => GetValue(ToolTipImageSourceProperty);
        set => SetValue(ToolTipImageSourceProperty, value);
    }

    /// <summary>The category the item belongs to.</summary>
    public RibbonGalleryCategory? Category => this.FindLogicalAncestorOfType<RibbonGalleryCategory>();

    /// <summary>The gallery the item belongs to.</summary>
    public RibbonGallery? Gallery => this.FindLogicalAncestorOfType<RibbonGallery>();

    /// <summary>The item this container shows: its data item, or itself.</summary>
    internal object? Item => Category?.ItemFromContainer(this) ?? this;

    /// <summary>Selects the item when its KeyTip is typed.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
    {
        if (e.Source == this)
        {
            Gallery?.SelectFromUser(this);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        IsHighlighted = true;
        Gallery?.OnItemHighlighted(this, true);
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        IsHighlighted = false;
        PseudoClasses.Set(":pressed", false);
        Gallery?.OnItemHighlighted(this, false);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            PseudoClasses.Set(":pressed", true);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (PseudoClasses.Contains(":pressed"))
        {
            PseudoClasses.Set(":pressed", false);
            e.Handled = true;
            Gallery?.SelectFromUser(this);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty)
        {
            PseudoClasses.Set(":selected", change.GetNewValue<bool>());
        }
        else if (change.Property == IsHighlightedProperty)
        {
            PseudoClasses.Set(":highlighted", change.GetNewValue<bool>());
        }
        else if (RibbonHelper.AffectsToolTip(change.Property))
        {
            RibbonHelper.UpdateToolTip(this);
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGalleryItemAutomationPeer(this);
}
