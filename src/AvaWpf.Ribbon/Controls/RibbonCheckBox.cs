// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonCheckBox.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>
/// A Ribbon check box: a check mark (or image) beside a label, sized by its <see cref="ControlSizeDefinition"/>.
/// </summary>
[PseudoClasses(RibbonHelper.Large, RibbonHelper.Small, RibbonHelper.NoImage, RibbonHelper.NoLabel, RibbonHelper.Collapsed, RibbonHelper.InQat, RibbonHelper.InControlGroup)]
public class RibbonCheckBox : CheckBox
{
    /// <summary>Defines the <see cref="Label"/> property.</summary>
    public static readonly AttachedProperty<string?> LabelProperty = RibbonControlService.LabelProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="LargeImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="SmallImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="KeyTip"/> property.</summary>
    public static readonly AttachedProperty<string?> KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="ToolTipTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipTitleProperty = RibbonControlService.ToolTipTitleProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="ToolTipDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipDescriptionProperty = RibbonControlService.ToolTipDescriptionProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="ToolTipImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipImageSourceProperty = RibbonControlService.ToolTipImageSourceProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="ToolTipFooterTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterTitleProperty = RibbonControlService.ToolTipFooterTitleProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="ToolTipFooterDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterDescriptionProperty = RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="ToolTipFooterImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipFooterImageSourceProperty = RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="ControlSizeDefinition"/> property.</summary>
    public static readonly AttachedProperty<RibbonControlSizeDefinition?> ControlSizeDefinitionProperty = RibbonControlService.ControlSizeDefinitionProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="QuickAccessToolBarControlSizeDefinition"/> property.</summary>
    public static readonly AttachedProperty<RibbonControlSizeDefinition?> QuickAccessToolBarControlSizeDefinitionProperty = RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="QuickAccessToolBarId"/> property.</summary>
    public static readonly AttachedProperty<object?> QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="CanAddToQuickAccessToolBarDirectly"/> property.</summary>
    public static readonly AttachedProperty<bool> CanAddToQuickAccessToolBarDirectlyProperty = RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="IsInQuickAccessToolBar"/> property.</summary>
    public static readonly AttachedProperty<bool> IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="IsInControlGroup"/> property.</summary>
    public static readonly AttachedProperty<bool> IsInControlGroupProperty = RibbonControlService.IsInControlGroupProperty.AddOwner<RibbonCheckBox>();

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonCheckBox>();

    static RibbonCheckBox()
    {
        KeyTipService.KeyTipAccessedEvent.AddClassHandler<RibbonCheckBox>((b, e) => b.OnKeyTipAccessed(e));
    }

    /// <summary>Initializes a new instance of the <see cref="RibbonCheckBox"/> class.</summary>
    public RibbonCheckBox()
    {
        RibbonHelper.UpdateSizePseudoClasses(this, PseudoClasses);
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

    /// <summary>Toggles the control when its KeyTip is typed.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
    {
        if (e.Source == this)
        {
            OnClick();
            e.Handled = true;
        }
    }

    /// <summary>Clicks, then closes the Ribbon popups the control is in (a collapsed group, a menu), as WPF does.</summary>
    protected override void OnClick()
    {
        base.OnClick();
        RaiseEvent(new RibbonDismissPopupEventArgs());
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (RibbonHelper.AffectsSize(change.Property))
        {
            RibbonHelper.UpdateSizePseudoClasses(this, PseudoClasses);
        }
        else if (RibbonHelper.AffectsToolTip(change.Property))
        {
            RibbonHelper.UpdateToolTip(this);
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonCheckBoxAutomationPeer(this);
}
