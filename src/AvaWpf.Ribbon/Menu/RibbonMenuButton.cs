// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonMenuButton.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Platform;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;
using AvaWpf.Ribbon.Primitives;

namespace AvaWpf.Ribbon;

/// <summary>
/// A Ribbon drop-down button: clicking it opens a menu of <see cref="RibbonMenuItem"/>s, galleries and separators.
/// </summary>
[PseudoClasses(RibbonHelper.Large, RibbonHelper.Small, RibbonHelper.NoImage, RibbonHelper.NoLabel, RibbonHelper.Collapsed, RibbonHelper.InQat, RibbonHelper.InControlGroup)]
public class RibbonMenuButton : MenuBase
{
    /// <summary>Defines the <see cref="Label"/> property.</summary>
    public static readonly AttachedProperty<string?> LabelProperty = RibbonControlService.LabelProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="LargeImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="SmallImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="KeyTip"/> property.</summary>
    public static readonly AttachedProperty<string?> KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="ToolTipTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipTitleProperty = RibbonControlService.ToolTipTitleProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="ToolTipDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipDescriptionProperty = RibbonControlService.ToolTipDescriptionProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="ToolTipImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipImageSourceProperty = RibbonControlService.ToolTipImageSourceProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="ToolTipFooterTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterTitleProperty = RibbonControlService.ToolTipFooterTitleProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="ToolTipFooterDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterDescriptionProperty = RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="ToolTipFooterImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipFooterImageSourceProperty = RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="ControlSizeDefinition"/> property.</summary>
    public static readonly AttachedProperty<RibbonControlSizeDefinition?> ControlSizeDefinitionProperty = RibbonControlService.ControlSizeDefinitionProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="QuickAccessToolBarControlSizeDefinition"/> property.</summary>
    public static readonly AttachedProperty<RibbonControlSizeDefinition?> QuickAccessToolBarControlSizeDefinitionProperty = RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="QuickAccessToolBarId"/> property.</summary>
    public static readonly AttachedProperty<object?> QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="CanAddToQuickAccessToolBarDirectly"/> property.</summary>
    public static readonly AttachedProperty<bool> CanAddToQuickAccessToolBarDirectlyProperty = RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="IsInQuickAccessToolBar"/> property.</summary>
    public static readonly AttachedProperty<bool> IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="IsInControlGroup"/> property.</summary>
    public static readonly AttachedProperty<bool> IsInControlGroupProperty = RibbonControlService.IsInControlGroupProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonMenuButton>();

    /// <summary>Defines the <see cref="IsDropDownOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<RibbonMenuButton, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="DropDownHeight"/> property.</summary>
    public static readonly StyledProperty<double> DropDownHeightProperty =
        AvaloniaProperty.Register<RibbonMenuButton, double>(nameof(DropDownHeight), double.NaN);

    /// <summary>Defines the <see cref="HasGallery"/> property.</summary>
    public static readonly DirectProperty<RibbonMenuButton, bool> HasGalleryProperty =
        AvaloniaProperty.RegisterDirect<RibbonMenuButton, bool>(nameof(HasGallery), o => o.HasGallery);

    private bool _hasGallery;

    static RibbonMenuButton()
    {
        KeyTipService.KeyTipAccessedEvent.AddClassHandler<RibbonMenuButton>((b, e) => b.OnKeyTipAccessed(e));
        ItemsPanelProperty.OverrideDefaultValue<RibbonMenuButton>(new FuncTemplate<Panel?>(() => new RibbonMenuItemsPanel()));
        KeyTipService.IsKeyTipScopeProperty.OverrideDefaultValue<RibbonMenuButton>(true);
        RibbonControlService.DismissPopupEvent.AddClassHandler<RibbonMenuButton>((b, e) => b.OnDismissPopup(e));
    }

    /// <summary>Initializes a new instance of the <see cref="RibbonMenuButton"/> class.</summary>
    public RibbonMenuButton()
        : base(new DefaultMenuInteractionHandler(true))
    {
        RibbonHelper.UpdateSizePseudoClasses(this, PseudoClasses);
        Items.CollectionChanged += (_, _) => UpdateHasGallery();
    }

    /// <summary>Whether the drop-down is open.</summary>
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>The height of the drop-down. NaN sizes it to the items.</summary>
    public double DropDownHeight
    {
        get => GetValue(DropDownHeightProperty);
        set => SetValue(DropDownHeightProperty, value);
    }

    /// <summary>Whether one of the items is a <see cref="RibbonGallery"/>.</summary>
    public bool HasGallery
    {
        get => _hasGallery;
        private set => SetAndRaise(HasGalleryProperty, ref _hasGallery, value);
    }

    /// <summary>Opens the drop-down.</summary>
    public override void Open() => IsDropDownOpen = true;

    /// <summary>Closes the drop-down.</summary>
    public override void Close() => IsDropDownOpen = false;

    /// <summary>Closes the drop-down when an item asks the Ribbon popups to close.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnDismissPopup(RibbonDismissPopupEventArgs e)
    {
        // A click on the button's own toggle part opens the drop-down; it must not close it or the popups above.
        if (e.Source is Control { Name: "PART_ToggleButton", TemplatedParent: { } parent } && parent == this)
        {
            e.Handled = true;
            return;
        }

        if (e.DismissMode == RibbonDismissPopupMode.Always || !IsPointerOver)
        {
            IsDropDownOpen = false;
        }
        else
        {
            e.Handled = true;
        }
    }

    /// <summary>Called when the drop-down opened or closed.</summary>
    /// <param name="isOpen">The new state.</param>
    protected virtual void OnIsDropDownOpenChanged(bool isOpen)
    {
        IsOpen = isOpen;
        if (isOpen)
        {
            RaiseEvent(new RoutedEventArgs(OpenedEvent));
        }
        else
        {
            SelectedItem = null;
            foreach (var container in GetRealizedContainers())
            {
                if (container is MenuItem { IsSubMenuOpen: true } item)
                {
                    item.Close();
                }
            }

            RaiseEvent(new RoutedEventArgs(ClosedEvent));
        }
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is MenuItem or RibbonGallery or Separator)
        {
            recycleKey = null;
            return false;
        }

        if (item is Control)
        {
            recycleKey = null;
            return false;
        }

        return NeedsContainer<RibbonMenuItem>(item, out recycleKey);
    }

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new RibbonMenuItem();

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
    }

    /// <summary>The label.</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>The 32 × 32 image.</summary>
    public IImage? LargeImageSource
    {
        get => GetValue(LargeImageSourceProperty);
        set => SetValue(LargeImageSourceProperty, value);
    }

    /// <summary>The 16 × 16 image.</summary>
    public IImage? SmallImageSource
    {
        get => GetValue(SmallImageSourceProperty);
        set => SetValue(SmallImageSourceProperty, value);
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

    /// <summary>The size variant, set by the owning <see cref="RibbonGroup"/>. Null is the default size.</summary>
    public RibbonControlSizeDefinition? ControlSizeDefinition
    {
        get => GetValue(ControlSizeDefinitionProperty);
        set => SetValue(ControlSizeDefinitionProperty, value);
    }

    /// <summary>The size variant in the Quick Access Toolbar. Null is a small image without a label.</summary>
    public RibbonControlSizeDefinition? QuickAccessToolBarControlSizeDefinition
    {
        get => GetValue(QuickAccessToolBarControlSizeDefinitionProperty);
        set => SetValue(QuickAccessToolBarControlSizeDefinitionProperty, value);
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

    /// <summary>Whether the control is in the Quick Access Toolbar.</summary>
    public bool IsInQuickAccessToolBar => GetValue(IsInQuickAccessToolBarProperty);

    /// <summary>Whether the control is an item of a <see cref="RibbonControlGroup"/>.</summary>
    public bool IsInControlGroup => GetValue(IsInControlGroupProperty);

    /// <summary>The Ribbon the control is in.</summary>
    public Ribbon? Ribbon => GetValue(RibbonProperty);

    /// <summary>Opens the drop-down when its KeyTip is typed; its items' KeyTips show next.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
    {
        if (e.Source == this)
        {
            IsDropDownOpen = true;
            e.TargetKeyTipScope = this;
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsDropDownOpenProperty)
        {
            OnIsDropDownOpenChanged(change.GetNewValue<bool>());
        }
        else if (RibbonHelper.AffectsSize(change.Property))
        {
            RibbonHelper.UpdateSizePseudoClasses(this, PseudoClasses);
        }
        else if (RibbonHelper.AffectsToolTip(change.Property))
        {
            RibbonHelper.UpdateToolTip(this);
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonMenuButtonAutomationPeer(this);
}
