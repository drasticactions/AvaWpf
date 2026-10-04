// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonTabHeader.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Media;

namespace AvaWpf.Ribbon;

/// <summary>
/// The header of a <see cref="RibbonTab"/> in the Ribbon's tab row. Clicking it selects the tab; double-clicking the
/// selected tab minimizes or restores the Ribbon. A contextual tab's header is tinted with its group's color.
/// </summary>
[PseudoClasses(":selected", ":contextual", ":minimized", ":dropdownopen")]
public class RibbonTabHeader : ContentControl
{
    /// <summary>Defines the <see cref="RibbonTab"/> property.</summary>
    public static readonly DirectProperty<RibbonTabHeader, RibbonTab?> RibbonTabProperty =
        AvaloniaProperty.RegisterDirect<RibbonTabHeader, RibbonTab?>(nameof(RibbonTab), o => o.RibbonTab);

    /// <summary>Defines the <see cref="IsRibbonTabSelected"/> property.</summary>
    public static readonly StyledProperty<bool> IsRibbonTabSelectedProperty =
        AvaloniaProperty.Register<RibbonTabHeader, bool>(nameof(IsRibbonTabSelected));

    /// <summary>Defines the <see cref="ContextualTabGroup"/> property.</summary>
    public static readonly StyledProperty<RibbonContextualTabGroup?> ContextualTabGroupProperty =
        AvaloniaProperty.Register<RibbonTabHeader, RibbonContextualTabGroup?>(nameof(ContextualTabGroup));

    /// <summary>Defines the <see cref="ContextualTabGroupBackground"/> property.</summary>
    public static readonly StyledProperty<IBrush?> ContextualTabGroupBackgroundProperty =
        AvaloniaProperty.Register<RibbonTabHeader, IBrush?>(nameof(ContextualTabGroupBackground));

    /// <summary>Defines the <see cref="KeyTip"/> property.</summary>
    public static readonly AttachedProperty<string?> KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner<RibbonTabHeader>();

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonTabHeader>();

    private RibbonTab? _ribbonTab;

    static RibbonTabHeader()
    {
        KeyTipService.KeyTipAccessedEvent.AddClassHandler<RibbonTabHeader>((h, e) => h.OnKeyTipAccessed(e));
        FocusableProperty.OverrideDefaultValue<RibbonTabHeader>(true);
    }

    /// <summary>The tab the header belongs to.</summary>
    public RibbonTab? RibbonTab
    {
        get => _ribbonTab;
        internal set => SetAndRaise(RibbonTabProperty, ref _ribbonTab, value);
    }

    /// <summary>Whether the header's tab is selected.</summary>
    public bool IsRibbonTabSelected
    {
        get => GetValue(IsRibbonTabSelectedProperty);
        set => SetValue(IsRibbonTabSelectedProperty, value);
    }

    /// <summary>The contextual tab group of the header's tab.</summary>
    public RibbonContextualTabGroup? ContextualTabGroup
    {
        get => GetValue(ContextualTabGroupProperty);
        set => SetValue(ContextualTabGroupProperty, value);
    }

    /// <summary>The tint of a contextual tab: its group's background.</summary>
    public IBrush? ContextualTabGroupBackground
    {
        get => GetValue(ContextualTabGroupBackgroundProperty);
        set => SetValue(ContextualTabGroupBackgroundProperty, value);
    }

    /// <summary>The tab's KeyTip.</summary>
    public string? KeyTip
    {
        get => GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>The Ribbon the header is in.</summary>
    public Ribbon? Ribbon => GetValue(RibbonProperty);

    /// <summary>The index of the header's tab in the Ribbon.</summary>
    internal int Index { get; set; } = -1;

    /// <summary>Selects the tab when its KeyTip is typed; the tab's KeyTips show next.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
    {
        if (e.Source != this || Ribbon is not { } ribbon)
        {
            return;
        }

        ribbon.SelectedIndex = Index;
        if (ribbon.IsMinimized)
        {
            ribbon.IsDropDownOpen = true;
        }

        e.TargetKeyTipScope = RibbonTab;
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && Ribbon is { } ribbon)
        {
            ribbon.NotifyMouseClickedOnTabHeader(this, e.ClickCount);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsRibbonTabSelectedProperty)
        {
            PseudoClasses.Set(":selected", change.GetNewValue<bool>());
        }
        else if (change.Property == ContextualTabGroupProperty)
        {
            PseudoClasses.Set(":contextual", change.NewValue is not null);
        }
    }

    /// <summary>Updates the pseudo-classes that follow the Ribbon state.</summary>
    internal void UpdateRibbonState(bool isMinimized, bool isDropDownOpen)
    {
        PseudoClasses.Set(":minimized", isMinimized);
        PseudoClasses.Set(":dropdownopen", isDropDownOpen);
    }
}
