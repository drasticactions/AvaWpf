// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonTextBox.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Media;
using System.Windows.Input;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>
/// A Ribbon text box: an optional small image and label beside the text field. Enter runs <see cref="Command"/>.
/// </summary>
[PseudoClasses(RibbonHelper.Large, RibbonHelper.Small, RibbonHelper.NoImage, RibbonHelper.NoLabel, RibbonHelper.Collapsed, RibbonHelper.InQat, RibbonHelper.InControlGroup)]
public class RibbonTextBox : TextBox
{
    /// <summary>Defines the <see cref="Label"/> property.</summary>
    public static readonly AttachedProperty<string?> LabelProperty = RibbonControlService.LabelProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="LargeImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="SmallImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="KeyTip"/> property.</summary>
    public static readonly AttachedProperty<string?> KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="ToolTipTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipTitleProperty = RibbonControlService.ToolTipTitleProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="ToolTipDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipDescriptionProperty = RibbonControlService.ToolTipDescriptionProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="ToolTipImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipImageSourceProperty = RibbonControlService.ToolTipImageSourceProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="ToolTipFooterTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterTitleProperty = RibbonControlService.ToolTipFooterTitleProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="ToolTipFooterDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterDescriptionProperty = RibbonControlService.ToolTipFooterDescriptionProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="ToolTipFooterImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipFooterImageSourceProperty = RibbonControlService.ToolTipFooterImageSourceProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="ControlSizeDefinition"/> property.</summary>
    public static readonly AttachedProperty<RibbonControlSizeDefinition?> ControlSizeDefinitionProperty = RibbonControlService.ControlSizeDefinitionProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="QuickAccessToolBarControlSizeDefinition"/> property.</summary>
    public static readonly AttachedProperty<RibbonControlSizeDefinition?> QuickAccessToolBarControlSizeDefinitionProperty = RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="QuickAccessToolBarId"/> property.</summary>
    public static readonly AttachedProperty<object?> QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="CanAddToQuickAccessToolBarDirectly"/> property.</summary>
    public static readonly AttachedProperty<bool> CanAddToQuickAccessToolBarDirectlyProperty = RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="IsInQuickAccessToolBar"/> property.</summary>
    public static readonly AttachedProperty<bool> IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="IsInControlGroup"/> property.</summary>
    public static readonly AttachedProperty<bool> IsInControlGroupProperty = RibbonControlService.IsInControlGroupProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonTextBox>();

    /// <summary>Defines the <see cref="Command"/> property.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<RibbonTextBox, ICommand?>(nameof(Command));

    /// <summary>Defines the <see cref="CommandParameter"/> property.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<RibbonTextBox, object?>(nameof(CommandParameter));

    /// <summary>Defines the <see cref="TextBoxWidth"/> property.</summary>
    public static readonly StyledProperty<double> TextBoxWidthProperty =
        AvaloniaProperty.Register<RibbonTextBox, double>(nameof(TextBoxWidth), double.NaN);

    static RibbonTextBox()
    {
        KeyTipService.KeyTipAccessedEvent.AddClassHandler<RibbonTextBox>((b, e) => b.OnKeyTipAccessed(e));
    }

    /// <summary>Initializes a new instance of the <see cref="RibbonTextBox"/> class.</summary>
    public RibbonTextBox()
    {
        RibbonHelper.UpdateSizePseudoClasses(this, PseudoClasses);
    }

    /// <summary>The command run when Enter is pressed. Its parameter is <see cref="CommandParameter"/>, or the text when that is null.</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The parameter of <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>The width of the text field (the label and image are outside it). NaN sizes to the content.</summary>
    public double TextBoxWidth
    {
        get => GetValue(TextBoxWidthProperty);
        set => SetValue(TextBoxWidthProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Command is { } command && !AcceptsReturn)
        {
            var parameter = CommandParameter ?? Text;
            if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }

            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
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

    /// <summary>Focuses the text box when its KeyTip is typed.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
    {
        if (e.Source == this)
        {
            Focus();
            SelectAll();
            e.Handled = true;
        }
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
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTextBoxAutomationPeer(this);
}
