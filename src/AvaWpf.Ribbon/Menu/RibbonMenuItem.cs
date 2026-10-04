// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonMenuItem.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;
using AvaWpf.Ribbon.Primitives;

namespace AvaWpf.Ribbon;

/// <summary>
/// An item of a <see cref="RibbonMenuButton"/>, <see cref="RibbonSplitButton"/> or another menu item: an image, a
/// header, and a submenu of further items or a gallery.
/// </summary>
public class RibbonMenuItem : MenuItem
{
    /// <summary>Defines the <see cref="ImageSource"/> property.</summary>
    public static readonly StyledProperty<IImage?> ImageSourceProperty =
        AvaloniaProperty.Register<RibbonMenuItem, IImage?>(nameof(ImageSource));

    /// <summary>Defines the <see cref="QuickAccessToolBarImageSource"/> property.</summary>
    public static readonly StyledProperty<IImage?> QuickAccessToolBarImageSourceProperty =
        AvaloniaProperty.Register<RibbonMenuItem, IImage?>(nameof(QuickAccessToolBarImageSource));

    /// <summary>Defines the <see cref="KeyTip"/> property.</summary>
    public static readonly AttachedProperty<string?> KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="ToolTipTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipTitleProperty = RibbonControlService.ToolTipTitleProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="ToolTipDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipDescriptionProperty = RibbonControlService.ToolTipDescriptionProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="ToolTipImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipImageSourceProperty = RibbonControlService.ToolTipImageSourceProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="ToolTipFooterTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterTitleProperty = RibbonControlService.ToolTipFooterTitleProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="ToolTipFooterDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterDescriptionProperty = RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="ToolTipFooterImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipFooterImageSourceProperty = RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="QuickAccessToolBarId"/> property.</summary>
    public static readonly AttachedProperty<object?> QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="CanAddToQuickAccessToolBarDirectly"/> property.</summary>
    public static readonly AttachedProperty<bool> CanAddToQuickAccessToolBarDirectlyProperty = RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonMenuItem>();

    /// <summary>Defines the <see cref="HasGallery"/> property.</summary>
    public static readonly DirectProperty<RibbonMenuItem, bool> HasGalleryProperty =
        AvaloniaProperty.RegisterDirect<RibbonMenuItem, bool>(nameof(HasGallery), o => o.HasGallery);

    private bool _hasGallery;

    static RibbonMenuItem()
    {
        KeyTipService.KeyTipAccessedEvent.AddClassHandler<RibbonMenuItem>((m, e) => m.OnKeyTipAccessed(e));
        ItemsPanelProperty.OverrideDefaultValue<RibbonMenuItem>(new FuncTemplate<Panel?>(() => new RibbonMenuItemsPanel()));
    }

    /// <summary>Initializes a new instance of the <see cref="RibbonMenuItem"/> class.</summary>
    public RibbonMenuItem()
    {
        Items.CollectionChanged += (_, _) => UpdateHasGallery();
    }

    /// <summary>The 16 × 16 image beside the header.</summary>
    public IImage? ImageSource
    {
        get => GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    /// <summary>The image of the item's copy in the Quick Access Toolbar. Null uses <see cref="ImageSource"/>.</summary>
    public IImage? QuickAccessToolBarImageSource
    {
        get => GetValue(QuickAccessToolBarImageSourceProperty);
        set => SetValue(QuickAccessToolBarImageSourceProperty, value);
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

    /// <summary>The footer title of the rich tool tip.</summary>
    public string? ToolTipFooterTitle
    {
        get => GetValue(ToolTipFooterTitleProperty);
        set => SetValue(ToolTipFooterTitleProperty, value);
    }

    /// <summary>The footer description of the rich tool tip.</summary>
    public string? ToolTipFooterDescription
    {
        get => GetValue(ToolTipFooterDescriptionProperty);
        set => SetValue(ToolTipFooterDescriptionProperty, value);
    }

    /// <summary>The footer image of the rich tool tip.</summary>
    public IImage? ToolTipFooterImageSource
    {
        get => GetValue(ToolTipFooterImageSourceProperty);
        set => SetValue(ToolTipFooterImageSourceProperty, value);
    }

    /// <summary>The identity in the Quick Access Toolbar. Null uses the command.</summary>
    public object? QuickAccessToolBarId
    {
        get => GetValue(QuickAccessToolBarIdProperty);
        set => SetValue(QuickAccessToolBarIdProperty, value);
    }

    /// <summary>Whether the context menu offers "Add to Quick Access Toolbar". Default true.</summary>
    public bool CanAddToQuickAccessToolBarDirectly
    {
        get => GetValue(CanAddToQuickAccessToolBarDirectlyProperty);
        set => SetValue(CanAddToQuickAccessToolBarDirectlyProperty, value);
    }

    /// <summary>The Ribbon the item is in.</summary>
    public Ribbon? Ribbon => GetValue(RibbonProperty);

    /// <summary>Whether one of the items is a <see cref="RibbonGallery"/>.</summary>
    public bool HasGallery
    {
        get => _hasGallery;
        private set => SetAndRaise(HasGalleryProperty, ref _hasGallery, value);
    }

    /// <summary>
    /// Opens the submenu when its KeyTip is typed (its items' KeyTips show next), or clicks a leaf item and closes the
    /// menus.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
    {
        if (e.Source != this)
        {
            return;
        }

        if (HasSubMenu)
        {
            IsSubMenuOpen = true;
            e.TargetKeyTipScope = this;
        }
        else
        {
            if (ToggleType == MenuItemToggleType.CheckBox)
            {
                IsChecked = !IsChecked;
            }
            else if (ToggleType == MenuItemToggleType.Radio)
            {
                IsChecked = true;
            }

            RaiseEvent(new RoutedEventArgs(ClickEvent));
            RaiseEvent(new RibbonDismissPopupEventArgs());
        }

        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is Control)
        {
            recycleKey = null;
            return false;
        }

        return NeedsContainer<RibbonMenuItem>(item, out recycleKey);
    }

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new RibbonMenuItem();

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (RibbonHelper.AffectsToolTip(change.Property))
        {
            RibbonHelper.UpdateToolTip(this);
        }
    }

    private void UpdateHasGallery()
    {
        var has = false;
        foreach (var item in Items)
        {
            if (item is RibbonGallery)
            {
                has = true;
                break;
            }
        }

        HasGallery = has;
        KeyTipService.SetIsKeyTipScope(this, Items.Count > 0);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonMenuItemAutomationPeer(this);
}
