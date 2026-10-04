// Ported from WPF $W/PresentationFramework/System/Windows/Controls/ToolBar.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Automation.Peers;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Controls;

/// <summary>
/// A bar of commands whose items move to an overflow popup when they do not fit (see
/// <see cref="OverflowModeProperty"/>).
/// </summary>
/// <remarks>
/// Stock <see cref="Button"/>, <see cref="ToggleButton"/>, <see cref="CheckBox"/>, <see cref="RadioButton"/>,
/// <see cref="ComboBox"/>, <see cref="TextBox"/>, <see cref="Menu"/> and <see cref="Separator"/> items get the
/// <c>toolbar</c> style class (WPF's <c>ToolBar.*StyleKey</c>). The items panel is a <see cref="ToolBarPanel"/> and the
/// template's <c>PART_ToolBarOverflowPanel</c> is a <see cref="ToolBarOverflowPanel"/>.
/// </remarks>
[TemplatePart(PartToolBarPanel, typeof(ToolBarPanel))]
[TemplatePart(PartToolBarOverflowPanel, typeof(ToolBarOverflowPanel))]
[TemplatePart(PartOverflowButton, typeof(ToggleButton))]
[TemplatePart(PartGripper, typeof(Thumb))]
[PseudoClasses(PcHorizontal, PcVertical, PcOverflowOpen, PcHasOverflowItems, PcLocked)]
public class ToolBar : HeaderedItemsControl
{
    /// <summary>The name of the items panel, a <see cref="ToolBarPanel"/>.</summary>
    internal const string PartToolBarPanel = "PART_ToolBarPanel";

    /// <summary>The name of the overflow panel in the template.</summary>
    internal const string PartToolBarOverflowPanel = "PART_ToolBarOverflowPanel";

    /// <summary>The name of the overflow toggle button in the template.</summary>
    internal const string PartOverflowButton = "PART_OverflowButton";

    /// <summary>The name of the gripper thumb in the template.</summary>
    internal const string PartGripper = "PART_Gripper";

    /// <summary>The style class given to stock item controls, as WPF's <c>ToolBar.*StyleKey</c> styles.</summary>
    public const string ToolBarItemClass = "toolbar";

    private const string PcHorizontal = ":horizontal";
    private const string PcVertical = ":vertical";
    private const string PcOverflowOpen = ":overflowopen";
    private const string PcHasOverflowItems = ":hasoverflowitems";
    private const string PcLocked = ":locked";

    /// <summary>Defines the <see cref="Orientation"/> property.</summary>
    public static readonly DirectProperty<ToolBar, Orientation> OrientationProperty =
        AvaloniaProperty.RegisterDirect<ToolBar, Orientation>(nameof(Orientation), o => o.Orientation);

    /// <summary>Defines the <see cref="Band"/> property.</summary>
    public static readonly StyledProperty<int> BandProperty =
        AvaloniaProperty.Register<ToolBar, int>(nameof(Band));

    /// <summary>Defines the <see cref="BandIndex"/> property.</summary>
    public static readonly StyledProperty<int> BandIndexProperty =
        AvaloniaProperty.Register<ToolBar, int>(nameof(BandIndex));

    /// <summary>Defines the <see cref="IsOverflowOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsOverflowOpenProperty =
        AvaloniaProperty.Register<ToolBar, bool>(nameof(IsOverflowOpen), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceIsOverflowOpen);

    /// <summary>Defines the <see cref="HasOverflowItems"/> property.</summary>
    public static readonly DirectProperty<ToolBar, bool> HasOverflowItemsProperty =
        AvaloniaProperty.RegisterDirect<ToolBar, bool>(nameof(HasOverflowItems), o => o.HasOverflowItems);

    /// <summary>Defines the <c>ToolBar.OverflowMode</c> attached property: where an item is placed.</summary>
    public static readonly AttachedProperty<OverflowMode> OverflowModeProperty =
        AvaloniaProperty.RegisterAttached<ToolBar, Control, OverflowMode>("OverflowMode");

    /// <summary>
    /// Defines the <c>ToolBar.IsOverflowItem</c> attached property, which the <see cref="ToolBarPanel"/> sets while an item
    /// is in the overflow popup.
    /// </summary>
    public static readonly AttachedProperty<bool> IsOverflowItemProperty =
        AvaloniaProperty.RegisterAttached<ToolBar, Control, bool>("IsOverflowItem");

    private static readonly FuncTemplate<Panel?> s_defaultPanel = new(() => new ToolBarPanel { Name = PartToolBarPanel });

    private Orientation _orientation;
    private bool _hasOverflowItems;
    private ToolBarOverflowPanel? _overflowPanel;
    private IDisposable? _toolTipSuppression;
    private bool _openOnLoad;

    static ToolBar()
    {
        ItemsPanelProperty.OverrideDefaultValue<ToolBar>(s_defaultPanel);
        FocusableProperty.OverrideDefaultValue<ToolBar>(false);
        KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<ToolBar>(KeyboardNavigationMode.Cycle);
        OverflowModeProperty.Changed.AddClassHandler<Control>(OnOverflowModeChanged);
        Button.ClickEvent.AddClassHandler<ToolBar>((t, e) => t.OnItemClick(e));
    }

    /// <summary>Initializes a new instance of the <see cref="ToolBar"/> class.</summary>
    public ToolBar()
    {
        UpdatePseudoClasses();
    }

    /// <summary>The orientation of the bar, taken from its <see cref="ToolBarTray"/>; horizontal outside a tray.</summary>
    public Orientation Orientation
    {
        get => _orientation;
        private set => SetAndRaise(OrientationProperty, ref _orientation, value);
    }

    /// <summary>The band (row of a horizontal tray, column of a vertical one) the bar is placed in.</summary>
    public int Band
    {
        get => GetValue(BandProperty);
        set => SetValue(BandProperty, value);
    }

    /// <summary>The position of the bar within its <see cref="Band"/>.</summary>
    public int BandIndex
    {
        get => GetValue(BandIndexProperty);
        set => SetValue(BandIndexProperty, value);
    }

    /// <summary>Whether the overflow popup is open; set before load, it opens once the bar is loaded.</summary>
    public bool IsOverflowOpen
    {
        get => GetValue(IsOverflowOpenProperty);
        set => SetValue(IsOverflowOpenProperty, value);
    }

    /// <summary>Whether any item is in the overflow popup.</summary>
    public bool HasOverflowItems
    {
        get => _hasOverflowItems;
        internal set => SetAndRaise(HasOverflowItemsProperty, ref _hasOverflowItems, value);
    }

    /// <summary>The smallest length the bar can be laid out in: every item that cannot overflow, plus the chrome.</summary>
    internal double MinLength { get; private set; }

    /// <summary>The length the bar needs with no item in the overflow popup.</summary>
    internal double MaxLength { get; private set; }

    /// <summary>The items panel, or null before the template is applied.</summary>
    internal ToolBarPanel? ToolBarPanel => Presenter?.Panel as ToolBarPanel;

    /// <summary>The overflow panel of the template, or null.</summary>
    internal ToolBarOverflowPanel? ToolBarOverflowPanel => _overflowPanel;

    /// <summary>Gets the <c>ToolBar.OverflowMode</c> of an item.</summary>
    /// <param name="element">The item.</param>
    /// <returns>The overflow mode.</returns>
    public static OverflowMode GetOverflowMode(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(OverflowModeProperty);
    }

    /// <summary>Sets the <c>ToolBar.OverflowMode</c> of an item.</summary>
    /// <param name="element">The item.</param>
    /// <param name="mode">The overflow mode.</param>
    public static void SetOverflowMode(Control element, OverflowMode mode)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(OverflowModeProperty, mode);
    }

    /// <summary>Gets whether an item is in the overflow popup.</summary>
    /// <param name="element">The item.</param>
    /// <returns>True while the item is in the overflow popup.</returns>
    public static bool GetIsOverflowItem(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(IsOverflowItemProperty);
    }

    internal static void SetIsOverflowItem(Control element, bool value) => element.SetValue(IsOverflowItemProperty, value);

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _overflowPanel = e.NameScope.Find<ToolBarOverflowPanel>(PartToolBarOverflowPanel);
        InvalidateLayout();
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (IsStockItemType(container.GetType()))
        {
            container.Classes.Add(ToolBarItemClass);
        }
    }

    /// <inheritdoc/>
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (IsStockItemType(container.GetType()))
        {
            container.Classes.Remove(ToolBarItemClass);
        }

        SetIsOverflowItem(container, false);
    }

    /// <summary>
    /// Measures the template and records <see cref="MinLength"/> and <see cref="MaxLength"/>, the panel lengths plus the
    /// chrome.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = base.MeasureOverride(availableSize);
        if (ToolBarPanel is { } panel)
        {
            var margin = panel.Margin;
            double extra = panel.Orientation == Orientation.Horizontal
                ? Math.Max(0.0, desired.Width - panel.DesiredSize.Width + margin.Left + margin.Right)
                : Math.Max(0.0, desired.Height - panel.DesiredSize.Height + margin.Top + margin.Bottom);
            MinLength = panel.MinLength + extra;
            MaxLength = panel.MaxLength + extra;
        }

        return desired;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsOverflowOpenProperty)
        {
            OnOverflowOpenChanged(change.GetNewValue<bool>());
            UpdatePseudoClasses();
        }
        else if (change.Property == OrientationProperty || change.Property == HasOverflowItemsProperty || change.Property == ToolBarTray.IsLockedProperty)
        {
            UpdatePseudoClasses();
        }
        else if (change.Property == BandProperty || change.Property == BandIndexProperty)
        {
            (Parent as ToolBarTray)?.InvalidateMeasure();
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdateOrientation();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        UpdateOrientation();
    }

    /// <inheritdoc/>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (_openOnLoad)
        {
            // Open the overflow after the bar has rendered.
            _openOnLoad = false;
            Dispatcher.UIThread.Post(() => CoerceValue(IsOverflowOpenProperty), DispatcherPriority.Input);
        }
    }

    /// <summary>Closes the overflow popup for every unhandled press inside the bar.</summary>
    /// <param name="e">The event data.</param>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.Handled && IsOverflowOpen)
        {
            Close();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Moves the focus with Home, End and the arrow keys within the focused panel; Escape closes the overflow popup.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && FocusedItem(e.Source) is { } current)
        {
            var host = current.GetVisualParent() as Panel;
            var items = host is not null ? FocusableItems(host) : new List<Control>();
            var index = items.IndexOf(current);
            Control? next = null;
            switch (e.Key)
            {
                case Key.Home:
                    next = items.Count > 0 ? items[0] : null;
                    break;
                case Key.End:
                    next = items.Count > 0 ? items[^1] : null;
                    break;
                case Key.Left:
                case Key.Up:
                    next = index >= 0 && items.Count > 0 ? items[(index - 1 + items.Count) % items.Count] : null;
                    break;
                case Key.Right:
                case Key.Down:
                    next = index >= 0 && items.Count > 0 ? items[(index + 1) % items.Count] : null;
                    break;
                case Key.Escape:
                    if (_overflowPanel is { } overflow && overflow.IsKeyboardFocusWithin)
                    {
                        this.FindDescendantOfType<ToggleButton>()?.Focus(NavigationMethod.Directional);
                    }

                    Close();
                    e.Handled = true;
                    break;
            }

            if (next is not null && next.Focus(NavigationMethod.Directional, e.KeyModifiers))
            {
                e.Handled = true;
            }
        }

        if (!e.Handled)
        {
            base.OnKeyDown(e);
        }
    }

    /// <summary>Re-evaluates which items fit: resets the lengths and remeasures the bar and its panel.</summary>
    internal void InvalidateLayout()
    {
        MinLength = 0.0;
        MaxLength = 0.0;
        InvalidateMeasure();
        ToolBarPanel?.InvalidateMeasure();
    }

    /// <summary>Takes the orientation of the tray that holds the bar.</summary>
    internal void UpdateOrientation() => Orientation = (Parent as ToolBarTray)?.Orientation ?? Orientation.Horizontal;

    /// <summary>The focusable item containers of a panel, in order.</summary>
    internal static List<Control> FocusableItems(Panel host)
    {
        var list = new List<Control>();
        foreach (var v in host.GetVisualChildren())
        {
            if (v is Control c && c.Focusable && c.IsEffectivelyEnabled && c.IsEffectivelyVisible)
            {
                list.Add(c);
            }
        }

        return list;
    }

    private static bool IsStockItemType(Type t) =>
        t == typeof(Button) || t == typeof(ToggleButton) || t == typeof(Separator) || t == typeof(CheckBox) ||
        t == typeof(RadioButton) || t == typeof(ComboBox) || t == typeof(TextBox) || t == typeof(Menu);

    private static bool CoerceIsOverflowOpen(AvaloniaObject d, bool value)
    {
        if (value && d is ToolBar { IsLoaded: false } tb)
        {
            tb._openOnLoad = true;
            return false;
        }

        return value;
    }

    private static void OnOverflowModeChanged(Control element, AvaloniaPropertyChangedEventArgs e)
    {
        // When the mode of an item changes, the item may move between the main bar and the overflow.
        if (ItemsControl.ItemsControlFromItemContainer(element) is ToolBar toolBar)
        {
            toolBar.InvalidateLayout();
        }
    }

    private Control? FocusedItem(object? source)
    {
        var v = source as Visual;
        while (v is not null)
        {
            if (v is Control c && (v.GetVisualParent() is Controls.ToolBarPanel or Controls.ToolBarOverflowPanel) && IndexFromContainer(c) >= 0)
            {
                return c;
            }

            v = v.GetVisualParent();
        }

        return null;
    }

    private void OnOverflowOpenChanged(bool open)
    {
        if (open)
        {
            // Tooltips are off while the overflow is open, as in WPF.
            _toolTipSuppression ??= this.SetValue(ToolTip.ServiceEnabledProperty, false, BindingPriority.Animation);
            Dispatcher.UIThread.Post(() =>
            {
                if (_overflowPanel is { } overflow && FocusableItems(overflow) is { Count: > 0 } items)
                {
                    items[0].Focus(NavigationMethod.Directional);
                }
            }, DispatcherPriority.Input);
        }
        else
        {
            _toolTipSuppression?.Dispose();
            _toolTipSuppression = null;
            if (_overflowPanel is { IsKeyboardFocusWithin: true })
            {
                TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
            }
        }
    }

    private void OnItemClick(RoutedEventArgs e)
    {
        // A click on an item in the overflow closes the overflow.
        if (IsOverflowOpen && e.Source is Button b && b.Parent == this)
        {
            Close();
        }
    }

    private void Close() => SetCurrentValue(IsOverflowOpenProperty, false);

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(PcHorizontal, Orientation == Orientation.Horizontal);
        PseudoClasses.Set(PcVertical, Orientation == Orientation.Vertical);
        PseudoClasses.Set(PcOverflowOpen, IsOverflowOpen);
        PseudoClasses.Set(PcHasOverflowItems, HasOverflowItems);
        PseudoClasses.Set(PcLocked, ToolBarTray.GetIsLocked(this));
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new ToolBarAutomationPeer(this);
}
