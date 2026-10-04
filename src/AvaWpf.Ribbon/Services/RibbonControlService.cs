// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonControlService.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvaWpf.Ribbon;

/// <summary>
/// The attached properties shared by Ribbon controls, and <see cref="DismissPopupEvent"/>; set them directly on other
/// controls in a Ribbon.
/// </summary>
public static class RibbonControlService
{
    /// <summary>Defines the Label attached property: the text a control shows.</summary>
    public static readonly AttachedProperty<string?> LabelProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, string?>("Label", typeof(RibbonControlService));

    /// <summary>Defines the LargeImageSource attached property: the 32 × 32 image.</summary>
    public static readonly AttachedProperty<IImage?> LargeImageSourceProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, IImage?>("LargeImageSource", typeof(RibbonControlService));

    /// <summary>Defines the SmallImageSource attached property: the 16 × 16 image.</summary>
    public static readonly AttachedProperty<IImage?> SmallImageSourceProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, IImage?>("SmallImageSource", typeof(RibbonControlService));

    /// <summary>Defines the ToolTipTitle attached property: the bold title of the rich tool tip.</summary>
    public static readonly AttachedProperty<string?> ToolTipTitleProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, string?>("ToolTipTitle", typeof(RibbonControlService));

    /// <summary>Defines the ToolTipDescription attached property: the body of the rich tool tip.</summary>
    public static readonly AttachedProperty<string?> ToolTipDescriptionProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, string?>("ToolTipDescription", typeof(RibbonControlService));

    /// <summary>Defines the ToolTipImageSource attached property: the image beside the tool tip description.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipImageSourceProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, IImage?>("ToolTipImageSource", typeof(RibbonControlService));

    /// <summary>Defines the ToolTipFooterTitle attached property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterTitleProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, string?>("ToolTipFooterTitle", typeof(RibbonControlService));

    /// <summary>Defines the ToolTipFooterDescription attached property.</summary>
    public static readonly AttachedProperty<string?> ToolTipFooterDescriptionProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, string?>("ToolTipFooterDescription", typeof(RibbonControlService));

    /// <summary>Defines the ToolTipFooterImageSource attached property.</summary>
    public static readonly AttachedProperty<IImage?> ToolTipFooterImageSourceProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, IImage?>("ToolTipFooterImageSource", typeof(RibbonControlService));

    /// <summary>
    /// Defines the ControlSizeDefinition attached property: the size variant, set by the owning <see cref="RibbonGroup"/>.
    /// </summary>
    public static readonly AttachedProperty<RibbonControlSizeDefinition?> ControlSizeDefinitionProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, RibbonControlSizeDefinition?>("ControlSizeDefinition", typeof(RibbonControlService));

    /// <summary>
    /// Defines the QuickAccessToolBarControlSizeDefinition attached property: the copy's size variant; null is a small
    /// image without a label.
    /// </summary>
    public static readonly AttachedProperty<RibbonControlSizeDefinition?> QuickAccessToolBarControlSizeDefinitionProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, RibbonControlSizeDefinition?>("QuickAccessToolBarControlSizeDefinition", typeof(RibbonControlService));

    /// <summary>
    /// Defines the QuickAccessToolBarId attached property: a control's toolbar identity, defaulting to its command.
    /// A control without one cannot be added, and an id already in the toolbar is not added twice.
    /// </summary>
    public static readonly AttachedProperty<object?> QuickAccessToolBarIdProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, object?>("QuickAccessToolBarId", typeof(RibbonControlService));

    /// <summary>
    /// Defines the CanAddToQuickAccessToolBarDirectly attached property: whether the context menu offers "Add to Quick
    /// Access Toolbar"; false inside another control's template.
    /// </summary>
    public static readonly AttachedProperty<bool> CanAddToQuickAccessToolBarDirectlyProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("CanAddToQuickAccessToolBarDirectly", typeof(RibbonControlService), true);

    /// <summary>Defines the IsInQuickAccessToolBar attached property: true inside the Quick Access Toolbar (inherited).</summary>
    public static readonly AttachedProperty<bool> IsInQuickAccessToolBarProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("IsInQuickAccessToolBar", typeof(RibbonControlService), inherits: true);

    /// <summary>Defines the IsInControlGroup attached property: true for the items of a <see cref="RibbonControlGroup"/>.</summary>
    public static readonly AttachedProperty<bool> IsInControlGroupProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("IsInControlGroup", typeof(RibbonControlService));

    /// <summary>Defines the Ribbon attached property: the Ribbon a control is in (inherited).</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, Ribbon?>("Ribbon", typeof(RibbonControlService), inherits: true);

    /// <summary>Raised by a control in a Ribbon popup to close the popups above it.</summary>
    public static readonly RoutedEvent<RibbonDismissPopupEventArgs> DismissPopupEvent =
        RoutedEvent.Register<RibbonDismissPopupEventArgs>("DismissPopup", RoutingStrategies.Bubble, typeof(RibbonControlService));

    /// <summary>Gets the label.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static string? GetLabel(AvaloniaObject element) => element.GetValue(LabelProperty);

    /// <summary>Sets the label.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetLabel(AvaloniaObject element, string? value) => element.SetValue(LabelProperty, value);

    /// <summary>Gets the large image.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static IImage? GetLargeImageSource(AvaloniaObject element) => element.GetValue(LargeImageSourceProperty);

    /// <summary>Sets the large image.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetLargeImageSource(AvaloniaObject element, IImage? value) => element.SetValue(LargeImageSourceProperty, value);

    /// <summary>Gets the small image.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static IImage? GetSmallImageSource(AvaloniaObject element) => element.GetValue(SmallImageSourceProperty);

    /// <summary>Sets the small image.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetSmallImageSource(AvaloniaObject element, IImage? value) => element.SetValue(SmallImageSourceProperty, value);

    /// <summary>Gets the tool tip title.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static string? GetToolTipTitle(AvaloniaObject element) => element.GetValue(ToolTipTitleProperty);

    /// <summary>Sets the tool tip title.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetToolTipTitle(AvaloniaObject element, string? value) => element.SetValue(ToolTipTitleProperty, value);

    /// <summary>Gets the tool tip description.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static string? GetToolTipDescription(AvaloniaObject element) => element.GetValue(ToolTipDescriptionProperty);

    /// <summary>Sets the tool tip description.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetToolTipDescription(AvaloniaObject element, string? value) => element.SetValue(ToolTipDescriptionProperty, value);

    /// <summary>Gets the tool tip image.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static IImage? GetToolTipImageSource(AvaloniaObject element) => element.GetValue(ToolTipImageSourceProperty);

    /// <summary>Sets the tool tip image.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetToolTipImageSource(AvaloniaObject element, IImage? value) => element.SetValue(ToolTipImageSourceProperty, value);

    /// <summary>Gets the tool tip footer title.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static string? GetToolTipFooterTitle(AvaloniaObject element) => element.GetValue(ToolTipFooterTitleProperty);

    /// <summary>Sets the tool tip footer title.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetToolTipFooterTitle(AvaloniaObject element, string? value) => element.SetValue(ToolTipFooterTitleProperty, value);

    /// <summary>Gets the tool tip footer description.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static string? GetToolTipFooterDescription(AvaloniaObject element) => element.GetValue(ToolTipFooterDescriptionProperty);

    /// <summary>Sets the tool tip footer description.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetToolTipFooterDescription(AvaloniaObject element, string? value) => element.SetValue(ToolTipFooterDescriptionProperty, value);

    /// <summary>Gets the tool tip footer image.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static IImage? GetToolTipFooterImageSource(AvaloniaObject element) => element.GetValue(ToolTipFooterImageSourceProperty);

    /// <summary>Sets the tool tip footer image.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetToolTipFooterImageSource(AvaloniaObject element, IImage? value) => element.SetValue(ToolTipFooterImageSourceProperty, value);

    /// <summary>Gets the control size definition.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static RibbonControlSizeDefinition? GetControlSizeDefinition(AvaloniaObject element) => element.GetValue(ControlSizeDefinitionProperty);

    /// <summary>Sets the control size definition.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetControlSizeDefinition(AvaloniaObject element, RibbonControlSizeDefinition? value) => element.SetValue(ControlSizeDefinitionProperty, value);

    /// <summary>Gets the Quick Access Toolbar size definition.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static RibbonControlSizeDefinition? GetQuickAccessToolBarControlSizeDefinition(AvaloniaObject element) => element.GetValue(QuickAccessToolBarControlSizeDefinitionProperty);

    /// <summary>Sets the Quick Access Toolbar size definition.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetQuickAccessToolBarControlSizeDefinition(AvaloniaObject element, RibbonControlSizeDefinition? value) => element.SetValue(QuickAccessToolBarControlSizeDefinitionProperty, value);

    /// <summary>Gets the Quick Access Toolbar id.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static object? GetQuickAccessToolBarId(AvaloniaObject element) => element.GetValue(QuickAccessToolBarIdProperty);

    /// <summary>Sets the Quick Access Toolbar id.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetQuickAccessToolBarId(AvaloniaObject element, object? value) => element.SetValue(QuickAccessToolBarIdProperty, value);

    /// <summary>Gets whether the element can be added to the Quick Access Toolbar from its context menu.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static bool GetCanAddToQuickAccessToolBarDirectly(AvaloniaObject element) => element.GetValue(CanAddToQuickAccessToolBarDirectlyProperty);

    /// <summary>Sets whether the element can be added to the Quick Access Toolbar from its context menu.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetCanAddToQuickAccessToolBarDirectly(AvaloniaObject element, bool value) => element.SetValue(CanAddToQuickAccessToolBarDirectlyProperty, value);

    /// <summary>Gets whether the element is inside the Quick Access Toolbar.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static bool GetIsInQuickAccessToolBar(AvaloniaObject element) => element.GetValue(IsInQuickAccessToolBarProperty);

    /// <summary>Gets whether the element is an item of a <see cref="RibbonControlGroup"/>.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static bool GetIsInControlGroup(AvaloniaObject element) => element.GetValue(IsInControlGroupProperty);

    /// <summary>Gets the Ribbon the element is in.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static Ribbon? GetRibbon(AvaloniaObject element) => element.GetValue(RibbonProperty);

    /// <summary>Adds a handler for <see cref="DismissPopupEvent"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="handler">The handler.</param>
    public static void AddDismissPopupHandler(Interactive element, System.EventHandler<RibbonDismissPopupEventArgs> handler) =>
        element.AddHandler(DismissPopupEvent, handler);

    /// <summary>Removes a handler for <see cref="DismissPopupEvent"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="handler">The handler.</param>
    public static void RemoveDismissPopupHandler(Interactive element, System.EventHandler<RibbonDismissPopupEventArgs> handler) =>
        element.RemoveHandler(DismissPopupEvent, handler);
}
