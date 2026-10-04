// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonGroup.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaWpf.Ribbon.Primitives;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>
/// A titled group of controls on a <see cref="RibbonTab"/> that shrinks through its <see cref="GroupSizeDefinitions"/>
/// as width runs out, and finally collapses to a drop-down button.
/// </summary>
/// <remarks>
/// Without <see cref="GroupSizeDefinitions"/> it builds WPF's default steps: from the end, each step shrinks three
/// consecutive same-size controls, and the last step collapses the group.
/// </remarks>
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
[TemplatePart("PART_ItemsHost", typeof(Decorator))]
[TemplatePart("PART_PopupItemsHost", typeof(Decorator))]
[TemplatePart("PART_TemplateContentControl", typeof(ContentControl))]
[PseudoClasses(":collapsed", ":templated", ":inqat")]
public class RibbonGroup : HeaderedItemsControl
{
    /// <summary>Defines the <see cref="GroupSizeDefinitions"/> property.</summary>
    public static readonly StyledProperty<RibbonGroupSizeDefinitionBaseCollection?> GroupSizeDefinitionsProperty =
        AvaloniaProperty.Register<RibbonGroup, RibbonGroupSizeDefinitionBaseCollection?>(nameof(GroupSizeDefinitions));

    /// <summary>Defines the <see cref="IsCollapsed"/> property.</summary>
    public static readonly DirectProperty<RibbonGroup, bool> IsCollapsedProperty =
        AvaloniaProperty.RegisterDirect<RibbonGroup, bool>(nameof(IsCollapsed), o => o.IsCollapsed);

    /// <summary>Defines the <see cref="IsDropDownOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<RibbonGroup, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="LargeImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> LargeImageSourceProperty = RibbonControlService.LargeImageSourceProperty.AddOwner<RibbonGroup>();

    /// <summary>Defines the <see cref="SmallImageSource"/> property.</summary>
    public static readonly AttachedProperty<IImage?> SmallImageSourceProperty = RibbonControlService.SmallImageSourceProperty.AddOwner<RibbonGroup>();

    /// <summary>Defines the <see cref="KeyTip"/> property.</summary>
    public static readonly AttachedProperty<string?> KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner<RibbonGroup>();

    /// <summary>Defines the <see cref="ToolTipTitle"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipTitleProperty = RibbonControlService.ToolTipTitleProperty.AddOwner<RibbonGroup>();

    /// <summary>Defines the <see cref="ToolTipDescription"/> property.</summary>
    public static readonly AttachedProperty<string?> ToolTipDescriptionProperty = RibbonControlService.ToolTipDescriptionProperty.AddOwner<RibbonGroup>();

    /// <summary>Defines the <see cref="QuickAccessToolBarId"/> property.</summary>
    public static readonly AttachedProperty<object?> QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner<RibbonGroup>();

    /// <summary>Defines the <see cref="CanAddToQuickAccessToolBarDirectly"/> property.</summary>
    public static readonly AttachedProperty<bool> CanAddToQuickAccessToolBarDirectlyProperty = RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner<RibbonGroup>();

    /// <summary>Defines the <see cref="IsInQuickAccessToolBar"/> property.</summary>
    public static readonly AttachedProperty<bool> IsInQuickAccessToolBarProperty = RibbonControlService.IsInQuickAccessToolBarProperty.AddOwner<RibbonGroup>();

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonGroup>();

    private static readonly RibbonGroupSizeDefinition s_collapsedDefinition = new() { IsCollapsed = true };

    private RibbonGroupSizeDefinitionBaseCollection? _defaultDefinitions;
    private int _sizeDefinitionIndex;
    private bool _isCollapsed;
    private ItemsPresenter? _itemsPresenter;
    private Decorator? _itemsHost;
    private Decorator? _popupItemsHost;
    private ContentControl? _templateContentControl;
    private RibbonToggleButton? _toggleButton;

    static RibbonGroup()
    {
        KeyTipService.KeyTipAccessedEvent.AddClassHandler<RibbonGroup>((g, e) => g.OnKeyTipAccessed(e));
        KeyTipService.ActivatingKeyTipEvent.AddClassHandler<RibbonGroup>((g, e) => g.OnActivatingKeyTip(e));
        RibbonControlService.DismissPopupEvent.AddClassHandler<RibbonGroup>((g, e) => g.OnDismissPopup(e));

        // The default steps start from each control's default size, which depends on its images and label: XAML can
        // set them after the control is added to the group. A control group's default size is the union of its items'.
        RibbonControlService.LargeImageSourceProperty.Changed.AddClassHandler<Control>((c, _) => OnDefaultSizeInputChanged(c));
        RibbonControlService.SmallImageSourceProperty.Changed.AddClassHandler<Control>((c, _) => OnDefaultSizeInputChanged(c));
        RibbonControlService.LabelProperty.Changed.AddClassHandler<Control>((c, _) => OnDefaultSizeInputChanged(c));
    }

    private static void OnDefaultSizeInputChanged(Control control)
    {
        var item = control.Parent is RibbonControlGroup { Parent: RibbonGroup } controlGroup ? controlGroup : control;
        if (item.Parent is RibbonGroup { GroupSizeDefinitions: not { Count: > 0 } } group && group != item && group._defaultDefinitions is not null)
        {
            group.InvalidateSizeDefinitions();
        }
    }

    /// <summary>Initializes a new instance of the <see cref="RibbonGroup"/> class.</summary>
    public RibbonGroup()
    {
        Items.CollectionChanged += (_, _) => InvalidateSizeDefinitions();
    }

    /// <summary>
    /// The size steps of the group, largest first. Null (default) uses WPF's default steps (see the remarks).
    /// </summary>
    public RibbonGroupSizeDefinitionBaseCollection? GroupSizeDefinitions
    {
        get => GetValue(GroupSizeDefinitionsProperty);
        set => SetValue(GroupSizeDefinitionsProperty, value);
    }

    /// <summary>Whether the group is collapsed to a single drop-down button.</summary>
    public bool IsCollapsed
    {
        get => _isCollapsed;
        private set => SetAndRaise(IsCollapsedProperty, ref _isCollapsed, value);
    }

    /// <summary>Whether the drop-down of a collapsed group is open.</summary>
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>The 32 × 32 image of the collapsed drop-down button.</summary>
    public IImage? LargeImageSource
    {
        get => GetValue(LargeImageSourceProperty);
        set => SetValue(LargeImageSourceProperty, value);
    }

    /// <summary>The 16 × 16 image of the group in the Quick Access Toolbar.</summary>
    public IImage? SmallImageSource
    {
        get => GetValue(SmallImageSourceProperty);
        set => SetValue(SmallImageSourceProperty, value);
    }

    /// <summary>The KeyTip of the collapsed drop-down button.</summary>
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

    /// <summary>The identity in the Quick Access Toolbar.</summary>
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

    /// <summary>Whether the group is in the Quick Access Toolbar.</summary>
    public bool IsInQuickAccessToolBar => GetValue(IsInQuickAccessToolBarProperty);

    /// <summary>The Ribbon the group is in.</summary>
    public Ribbon? Ribbon => GetValue(RibbonProperty);

    /// <summary>The size steps in effect: <see cref="GroupSizeDefinitions"/>, or the default steps.</summary>
    internal IReadOnlyList<RibbonGroupSizeDefinitionBase> ActualGroupSizeDefinitions =>
        GroupSizeDefinitions is { Count: > 0 } own ? own : (_defaultDefinitions ??= CreateDefaultGroupSizeDefinitions());

    /// <summary>The index of the size step in effect (0 is the largest).</summary>
    internal int SizeDefinitionIndex => _sizeDefinitionIndex;

    /// <summary>The tab the group is on.</summary>
    internal RibbonTab? Tab => this.FindAncestorOfType<RibbonTab>();

    /// <summary>Moves to the next larger step. With <paramref name="update"/> false only reports whether one exists.</summary>
    internal bool IncreaseGroupSize(bool update)
    {
        if (_sizeDefinitionIndex > 0 && ActualGroupSizeDefinitions.Count > 0)
        {
            if (update)
            {
                ApplyGroupSizeDefinition(--_sizeDefinitionIndex);
            }

            return true;
        }

        return false;
    }

    /// <summary>Moves to the next smaller step, if there is one.</summary>
    internal bool DecreaseGroupSize()
    {
        var definitions = ActualGroupSizeDefinitions;
        if (_sizeDefinitionIndex >= 0 && _sizeDefinitionIndex < definitions.Count - 1)
        {
            ApplyGroupSizeDefinition(++_sizeDefinitionIndex);
            return true;
        }

        return false;
    }

    /// <summary>Applies the current step again (after the items or the definitions changed).</summary>
    internal void ReapplyGroupSizeDefinition()
    {
        var count = ActualGroupSizeDefinitions.Count;
        if (count == 0)
        {
            return;
        }

        _sizeDefinitionIndex = System.Math.Clamp(_sizeDefinitionIndex, 0, count - 1);
        ApplyGroupSizeDefinition(_sizeDefinitionIndex);
    }

    /// <summary>Returns the group to its largest step.</summary>
    internal void ResetGroupSize()
    {
        _sizeDefinitionIndex = 0;
        ReapplyGroupSizeDefinition();
    }

    /// <summary>Opens the drop-down of a collapsed group when its KeyTip is typed; its controls' KeyTips show next.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnKeyTipAccessed(KeyTipAccessedEventArgs e)
    {
        if (e.Source == this && IsCollapsed)
        {
            IsDropDownOpen = true;
            e.TargetKeyTipScope = this;
            e.Handled = true;
        }
    }

    /// <summary>Hides the group's KeyTip unless the group is collapsed.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnActivatingKeyTip(ActivatingKeyTipEventArgs e)
    {
        if (e.Source == this)
        {
            e.KeyTipVisibility = IsCollapsed;
        }
    }

    /// <summary>Closes the drop-down when a control in it is used.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnDismissPopup(RibbonDismissPopupEventArgs e)
    {
        // The collapsed drop-down button is a template part; its click opens the popup.
        if (e.Source is Control { Name: "PART_ToggleButton", TemplatedParent: { } parent } && parent == this)
        {
            e.Handled = true;
            return;
        }

        if (IsDropDownOpen && (e.DismissMode == RibbonDismissPopupMode.Always || !IsPointerOver))
        {
            IsDropDownOpen = false;
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
        ApplyControlSize(container, index);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _itemsPresenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
        _itemsHost = e.NameScope.Find<Decorator>("PART_ItemsHost");
        _popupItemsHost = e.NameScope.Find<Decorator>("PART_PopupItemsHost");
        _templateContentControl = e.NameScope.Find<ContentControl>("PART_TemplateContentControl");
        _toggleButton = e.NameScope.Find<RibbonToggleButton>("PART_ToggleButton");
        UpdateToggleLabel();
        MovePresenter();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == GroupSizeDefinitionsProperty)
        {
            InvalidateSizeDefinitions();
        }
        else if (change.Property == HeaderProperty)
        {
            UpdateToggleLabel();
        }
        else if (change.Property == IsCollapsedProperty)
        {
            var collapsed = change.GetNewValue<bool>();
            PseudoClasses.Set(":collapsed", collapsed);
            KeyTipService.SetIsKeyTipScope(this, collapsed);
            if (!collapsed)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }

            MovePresenter();
        }
        else if (change.Property == IsDropDownOpenProperty)
        {
            MovePresenter();
        }
        else if (change.Property == IsInQuickAccessToolBarProperty)
        {
            PseudoClasses.Set(":inqat", change.GetNewValue<bool>());
        }
        else if (RibbonHelper.AffectsToolTip(change.Property))
        {
            RibbonHelper.UpdateToolTip(this);
        }
    }

    /// <summary>Shows the header as the label of the collapsed drop-down button.</summary>
    private void UpdateToggleLabel()
    {
        if (_toggleButton is not null)
        {
            _toggleButton.Label = Header?.ToString();
        }
    }

    /// <summary>Drops the default steps (they depend on the items) and applies the current step again.</summary>
    private void InvalidateSizeDefinitions()
    {
        _defaultDefinitions = null;
        ReapplyGroupSizeDefinition();
        this.FindAncestorOfType<RibbonGroupsPanel>()?.InvalidateGroupSizes();
    }

    private void ApplyGroupSizeDefinition(int index)
    {
        var definitions = ActualGroupSizeDefinitions;
        if (index < 0 || index >= definitions.Count)
        {
            return;
        }

        var definition = definitions[index];
        IsCollapsed = definition.IsCollapsed;
        PseudoClasses.Set(":templated", definition is RibbonGroupTemplateSizeDefinition);
        if (definition is RibbonGroupTemplateSizeDefinition templated)
        {
            if (_templateContentControl is not null)
            {
                _templateContentControl.ContentTemplate = templated.ContentTemplate ?? LastTemplate(index);
                _templateContentControl.Content = DataContext;
            }
        }
        else
        {
            for (var i = 0; i < Items.Count; i++)
            {
                if ((ContainerFromIndex(i) ?? Items[i] as Control) is { } container)
                {
                    ApplyControlSize(container, i);
                }
            }
        }

        // Re-measure the whole group in the panel's current measure pass: a size change deep inside (a label that
        // moves to one line) only reaches its ancestors after the layout manager measures it, which would be too late
        // for the groups panel that is deciding which group to shrink.
        RibbonHelper.InvalidateMeasureDeep(this);
    }

    private Avalonia.Controls.Templates.IDataTemplate? LastTemplate(int index)
    {
        var definitions = ActualGroupSizeDefinitions;
        for (var i = index; i >= 0; i--)
        {
            if (definitions[i] is RibbonGroupTemplateSizeDefinition { ContentTemplate: { } template })
            {
                return template;
            }
        }

        return null;
    }

    /// <summary>Sets the size variant of one control from the current step and re-measures it up to the group.</summary>
    private void ApplyControlSize(Control container, int index)
    {
        if (index < 0)
        {
            return;
        }

        var controlDefinitions = ControlDefinitionsForStep(_sizeDefinitionIndex);
        if (controlDefinitions is not null && index < controlDefinitions.Count)
        {
            container.SetValue(RibbonControlService.ControlSizeDefinitionProperty, controlDefinitions[index]);
        }
        else
        {
            container.ClearValue(RibbonControlService.ControlSizeDefinitionProperty);
        }

        RibbonHelper.InvalidateMeasureToAncestor<RibbonGroup>(container);
    }

    /// <summary>
    /// The control sizes of a step. A collapsed step with no sizes of its own shows the controls (in the drop-down) at
    /// the sizes of the last uncollapsed step that has some, as WPF's <c>GetControlDefinitionsForCollapsedGroup</c>.
    /// </summary>
    private RibbonControlSizeDefinitionCollection? ControlDefinitionsForStep(int index)
    {
        var definitions = ActualGroupSizeDefinitions;
        if (index < 0 || index >= definitions.Count)
        {
            return null;
        }

        if (definitions[index] is RibbonGroupSizeDefinition { ControlSizeDefinitions.Count: > 0 } own)
        {
            return own.ControlSizeDefinitions;
        }

        if (definitions[index].IsCollapsed)
        {
            for (var i = 0; i < definitions.Count; i++)
            {
                if (definitions[i] is RibbonGroupSizeDefinition { IsCollapsed: false, ControlSizeDefinitions.Count: > 0 } open)
                {
                    return open.ControlSizeDefinitions;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Puts the items in the drop-down while the drop-down of a collapsed group is open, and in the group otherwise. A
    /// collapsed group keeps them (hidden) in the group until the drop-down opens: moving them re-creates the visuals of
    /// every control in the group, and a resize collapses and restores groups at every step.
    /// </summary>
    private void MovePresenter()
    {
        if (_itemsPresenter is null || _itemsHost is null || _popupItemsHost is null)
        {
            return;
        }

        var inPopup = IsCollapsed && IsDropDownOpen;
        var target = inPopup ? _popupItemsHost : _itemsHost;
        var other = inPopup ? _itemsHost : _popupItemsHost;
        if (target.Child == _itemsPresenter)
        {
            return;
        }

        other.Child = null;
        target.Child = _itemsPresenter;
    }

    /// <summary>WPF's default steps (<c>RibbonGroup.GroupSizeDefinitionsInternal</c>).</summary>
    private RibbonGroupSizeDefinitionBaseCollection CreateDefaultGroupSizeDefinitions()
    {
        var result = new RibbonGroupSizeDefinitionBaseCollection();
        if (Items.Count == 0)
        {
            return result;
        }

        var large = new RibbonGroupSizeDefinition();
        for (var i = 0; i < Items.Count; i++)
        {
            var control = ContainerFromIndex(i) ?? Items[i] as Control;
            large.ControlSizeDefinitions.Add(control is null ? new RibbonControlSizeDefinition() : RibbonHelper.DefaultControlSizeDefinition(control).Clone());
        }

        result.Add(large);
        var sizes = large.ControlSizeDefinitions;
        if (Items.Count > 3)
        {
            // Shrink groups of 3 consecutive controls of the same size, looping backwards from the end:
            //   L L L L -> L M M M -> L S S S -> Collapsed
            //   L L L L L L -> L L L M M M -> M M M M M M -> M M M S S S -> S S S S S S -> Collapsed
            var last = large;
            var repeatStart = sizes.Count - 1;
            while (ReduceGroupSizeDefinition(last, ref repeatStart) is { } reduced)
            {
                result.Add(reduced);
                last = reduced;
                if (repeatStart < 3)
                {
                    repeatStart = last.ControlSizeDefinitions.Count - 1;
                }
            }
        }
        else if (Items.Count is 2 or 3)
        {
            // L L -> M M -> Collapsed and L L L -> M M M -> Collapsed (M M M does not reduce to S S S).
            var allLarge = true;
            foreach (var size in sizes)
            {
                allLarge &= size.ImageSize == RibbonImageSize.Large && size.IsLabelVisible;
            }

            if (allLarge)
            {
                var medium = new RibbonGroupSizeDefinition();
                for (var i = 0; i < sizes.Count; i++)
                {
                    medium.ControlSizeDefinitions.Add(new RibbonControlSizeDefinition { ImageSize = RibbonImageSize.Small, IsLabelVisible = true });
                }

                result.Add(medium);
            }
        }

        result.Add(s_collapsedDefinition);
        return result;
    }

    /// <summary>
    /// A smaller step: shrinks the 3 consecutive same-size controls found searching backwards from
    /// <paramref name="repeatStart"/> (WPF's <c>ReduceGroupSizeDefinition</c>).
    /// </summary>
    private static RibbonGroupSizeDefinition? ReduceGroupSizeDefinition(RibbonGroupSizeDefinition definition, ref int repeatStart)
    {
        var sizes = definition.ControlSizeDefinitions;
        var lastSize = sizes[repeatStart];
        var sameSizeCount = 1;
        for (var i = repeatStart - 1; i >= 0; i--)
        {
            var size = sizes[i];
            if (size.ImageSize != RibbonImageSize.Collapsed &&
                (size.IsLabelVisible || size.ImageSize == RibbonImageSize.Large) &&
                size.ImageSize == lastSize.ImageSize &&
                size.IsLabelVisible == lastSize.IsLabelVisible)
            {
                if (++sameSizeCount == 3)
                {
                    repeatStart = i;
                    break;
                }
            }
            else
            {
                sameSizeCount = 1;
            }

            lastSize = size;
        }

        if (sameSizeCount != 3)
        {
            return null;
        }

        var reduced = new RibbonGroupSizeDefinition();
        for (var i = 0; i < repeatStart; i++)
        {
            reduced.ControlSizeDefinitions.Add(sizes[i].Clone());
        }

        var repeated = sizes[repeatStart];
        var newLabelVisible = repeated.ImageSize == RibbonImageSize.Large && repeated.IsLabelVisible;
        for (var i = 0; i < 3; i++)
        {
            reduced.ControlSizeDefinitions.Add(new RibbonControlSizeDefinition { ImageSize = RibbonImageSize.Small, IsLabelVisible = newLabelVisible });
        }

        for (var i = repeatStart + 3; i < sizes.Count; i++)
        {
            reduced.ControlSizeDefinitions.Add(sizes[i].Clone());
        }

        return reduced;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGroupAutomationPeer(this);
}
