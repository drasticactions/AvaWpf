// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonQuickAccessToolBar.cs (MIT, see NOTICE.md).
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using AvaWpf.Controls;
using AvaWpf.Ribbon.Primitives;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>
/// The Quick Access Toolbar: small copies of Ribbon controls, with an overflow popup for items that do not fit.
/// It has the <c>:incaption</c> pseudo-class in a <see cref="RibbonWindow"/> caption.
/// </summary>
[TemplatePart("PART_MainPanel", typeof(RibbonQuickAccessToolBarPanel))]
[TemplatePart("PART_OverflowPanel", typeof(ToolBarOverflowPanel))]
[TemplatePart("PART_OverflowPopup", typeof(Popup))]
[TemplatePart("PART_OverflowButton", typeof(ToggleButton))]
[PseudoClasses(":overflow", ":incaption")]
public class RibbonQuickAccessToolBar : ItemsControl
{
    /// <summary>Defines the <see cref="CustomizeMenuButton"/> property.</summary>
    public static readonly StyledProperty<RibbonMenuButton?> CustomizeMenuButtonProperty =
        AvaloniaProperty.Register<RibbonQuickAccessToolBar, RibbonMenuButton?>(nameof(CustomizeMenuButton));

    /// <summary>Defines the <see cref="HasOverflowItems"/> property.</summary>
    public static readonly DirectProperty<RibbonQuickAccessToolBar, bool> HasOverflowItemsProperty =
        AvaloniaProperty.RegisterDirect<RibbonQuickAccessToolBar, bool>(nameof(HasOverflowItems), o => o.HasOverflowItems);

    /// <summary>Defines the <see cref="IsOverflowOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsOverflowOpenProperty =
        AvaloniaProperty.Register<RibbonQuickAccessToolBar, bool>(nameof(IsOverflowOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the IsOverflowItem attached property: true for the items in the overflow popup.</summary>
    public static readonly AttachedProperty<bool> IsOverflowItemProperty =
        AvaloniaProperty.RegisterAttached<RibbonQuickAccessToolBar, Control, bool>("IsOverflowItem");

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonQuickAccessToolBar>();

    /// <summary>
    /// Raised (bubbling) on a control being added to the toolbar; set
    /// <see cref="RibbonQuickAccessToolBarCloneEventArgs.CloneInstance"/> to supply the copy.
    /// </summary>
    public static readonly RoutedEvent<RibbonQuickAccessToolBarCloneEventArgs> CloneEvent =
        RoutedEvent.Register<RibbonQuickAccessToolBar, RibbonQuickAccessToolBarCloneEventArgs>("Clone", RoutingStrategies.Bubble);

    private bool _hasOverflowItems;

    static RibbonQuickAccessToolBar()
    {
        RibbonControlService.DismissPopupEvent.AddClassHandler<RibbonQuickAccessToolBar>((q, e) =>
        {
            if (q.IsOverflowOpen)
            {
                q.IsOverflowOpen = false;
            }
        });
    }

    /// <summary>Initializes a new instance of the <see cref="RibbonQuickAccessToolBar"/> class.</summary>
    public RibbonQuickAccessToolBar()
    {
        // Set (not only defaulted) so the items inherit it.
        SetValue(RibbonControlService.IsInQuickAccessToolBarProperty, true);
    }

    /// <summary>The menu button at the end of the toolbar (customize the toolbar).</summary>
    public RibbonMenuButton? CustomizeMenuButton
    {
        get => GetValue(CustomizeMenuButtonProperty);
        set => SetValue(CustomizeMenuButtonProperty, value);
    }

    /// <summary>Whether some items did not fit and are in the overflow popup.</summary>
    public bool HasOverflowItems
    {
        get => _hasOverflowItems;
        private set => SetAndRaise(HasOverflowItemsProperty, ref _hasOverflowItems, value);
    }

    /// <summary>Whether the overflow popup is open.</summary>
    public bool IsOverflowOpen
    {
        get => GetValue(IsOverflowOpenProperty);
        set => SetValue(IsOverflowOpenProperty, value);
    }

    /// <summary>The Ribbon the toolbar belongs to.</summary>
    public Ribbon? Ribbon => GetValue(RibbonProperty);

    /// <summary>The overflow panel of the template.</summary>
    internal ToolBarOverflowPanel? OverflowPanel { get; private set; }

    /// <summary>Gets whether an item is in the overflow popup.</summary>
    /// <param name="element">The item.</param>
    /// <returns>The value.</returns>
    public static bool GetIsOverflowItem(Control element) => element.GetValue(IsOverflowItemProperty);

    /// <summary>Sets whether an item is in the overflow popup (set by the toolbar's panel).</summary>
    internal static void SetIsOverflowItem(Control element, bool value) => element.SetValue(IsOverflowItemProperty, value);

    /// <summary>Whether an item with the given Quick Access Toolbar id is in the toolbar.</summary>
    /// <param name="id">The id.</param>
    /// <returns>True when found.</returns>
    public bool ContainsId(object id)
    {
        foreach (var item in Items)
        {
            if (item is AvaloniaObject o && Equals(RibbonHelper.QuickAccessToolBarId(o), id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Sets the <c>:incaption</c> pseudo-class (the toolbar is in a window caption).</summary>
    internal void SetIsInCaption(bool value) => PseudoClasses.Set(":incaption", value);

    /// <summary>Records whether items overflowed (from the panel's measure pass).</summary>
    internal void SetHasOverflowItems(bool value)
    {
        HasOverflowItems = value;
        PseudoClasses.Set(":overflow", value);
        if (!value && IsOverflowOpen)
        {
            SetCurrentValue(IsOverflowOpenProperty, false);
        }
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is Control)
        {
            recycleKey = null;
            return false;
        }

        return base.NeedsContainerOverride(item, index, out recycleKey);
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        SetGeneratedKeyTip(container, index);
    }

    /// <inheritdoc/>
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container.GetValue(s_autoKeyTipProperty))
        {
            container.ClearValue(KeyTipService.KeyTipProperty);
            container.ClearValue(s_autoKeyTipProperty);
        }

        Avalonia.Threading.Dispatcher.UIThread.Post(UpdateKeyTips);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        OverflowPanel = e.NameScope.Find<ToolBarOverflowPanel>("PART_OverflowPanel");
    }

    private static readonly AttachedProperty<bool> s_autoKeyTipProperty =
        AvaloniaProperty.RegisterAttached<RibbonQuickAccessToolBar, Control, bool>("AutoKeyTip");

    /// <summary>Gives items without a KeyTip WPF's generated QAT KeyTips: 1 to 9, then 09 to 01, then 0A to 0Z.</summary>
    private void UpdateKeyTips()
    {
        foreach (var container in GetRealizedContainers())
        {
            SetGeneratedKeyTip(container, IndexFromContainer(container));
        }
    }

    private static void SetGeneratedKeyTip(Control container, int index)
    {
        if (index < 0 || (!container.GetValue(s_autoKeyTipProperty) && !string.IsNullOrEmpty(KeyTipService.GetKeyTip(container))))
        {
            return;
        }

        container.SetValue(s_autoKeyTipProperty, true);
        KeyTipService.SetKeyTip(container, GeneratedKeyTip(index + 1));
    }

    /// <summary>The generated KeyTip of the item at a 1-based position.</summary>
    internal static string GeneratedKeyTip(int position) => position switch
    {
        <= 9 => position.ToString(CultureInfo.InvariantCulture),
        <= 18 => "0" + (19 - position).ToString(CultureInfo.InvariantCulture),
        _ => "0" + (char)('A' + Math.Min(25, position - 19)),
    };

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonQuickAccessToolBarAutomationPeer(this);
}
