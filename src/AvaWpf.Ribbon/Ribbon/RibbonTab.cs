// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonTab.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>
/// A tab of a <see cref="Ribbon"/>: a header in the tab row and a row of <see cref="RibbonGroup"/>s shown while the
/// tab is selected.
/// </summary>
[PseudoClasses(":selected", ":contextual")]
public class RibbonTab : HeaderedItemsControl
{
    /// <summary>Defines the <see cref="IsSelected"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        SelectingItemsControl.IsSelectedProperty.AddOwner<RibbonTab>();

    /// <summary>Defines the <see cref="GroupSizeReductionOrder"/> property.</summary>
    public static readonly StyledProperty<StringCollection?> GroupSizeReductionOrderProperty =
        AvaloniaProperty.Register<RibbonTab, StringCollection?>(nameof(GroupSizeReductionOrder));

    /// <summary>Defines the <see cref="ContextualTabGroupHeader"/> property.</summary>
    public static readonly StyledProperty<object?> ContextualTabGroupHeaderProperty =
        AvaloniaProperty.Register<RibbonTab, object?>(nameof(ContextualTabGroupHeader));

    /// <summary>Defines the <see cref="ContextualTabGroup"/> property.</summary>
    public static readonly DirectProperty<RibbonTab, RibbonContextualTabGroup?> ContextualTabGroupProperty =
        AvaloniaProperty.RegisterDirect<RibbonTab, RibbonContextualTabGroup?>(nameof(ContextualTabGroup), o => o.ContextualTabGroup);

    /// <summary>Defines the <see cref="IsTabVisible"/> property.</summary>
    public static readonly DirectProperty<RibbonTab, bool> IsTabVisibleProperty =
        AvaloniaProperty.RegisterDirect<RibbonTab, bool>(nameof(IsTabVisible), o => o.IsTabVisible);

    /// <summary>Defines the <see cref="KeyTip"/> property.</summary>
    public static readonly AttachedProperty<string?> KeyTipProperty = KeyTipService.KeyTipProperty.AddOwner<RibbonTab>();

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonTab>();

    private readonly List<int> _automaticResizeOrder = new();
    private readonly List<bool> _groupReductionResizeStatus = new();
    private int? _groupAutoResizeIndex;
    private int _groupReduceOrderLocation = -1;
    private RibbonContextualTabGroup? _contextualTabGroup;
    private bool _isTabVisible = true;

    static RibbonTab()
    {
        KeyTipService.IsKeyTipScopeProperty.OverrideDefaultValue<RibbonTab>(true);
        KeyTipService.ActivatingKeyTipEvent.AddClassHandler<RibbonTab>((t, e) =>
        {
            // The tab's KeyTip shows on its header; the tab itself is only the scope of its groups.
            if (e.Source == t)
            {
                e.KeyTipVisibility = false;
            }
        });
    }

    /// <summary>Initializes a new instance of the <see cref="RibbonTab"/> class.</summary>
    public RibbonTab()
    {
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    /// <summary>Whether the tab is selected.</summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// The group names in the order they shrink, such as <c>"Clipboard,Font,Clipboard"</c>; then groups shrink right to
    /// left, cyclically.
    /// </summary>
    public StringCollection? GroupSizeReductionOrder
    {
        get => GetValue(GroupSizeReductionOrderProperty);
        set => SetValue(GroupSizeReductionOrderProperty, value);
    }

    /// <summary>The <see cref="RibbonContextualTabGroup.Header"/> of the contextual tab group the tab belongs to.</summary>
    public object? ContextualTabGroupHeader
    {
        get => GetValue(ContextualTabGroupHeaderProperty);
        set => SetValue(ContextualTabGroupHeaderProperty, value);
    }

    /// <summary>The contextual tab group the tab belongs to.</summary>
    public RibbonContextualTabGroup? ContextualTabGroup
    {
        get => _contextualTabGroup;
        internal set
        {
            SetAndRaise(ContextualTabGroupProperty, ref _contextualTabGroup, value);
            PseudoClasses.Set(":contextual", value is not null);
            UpdateIsTabVisible();
        }
    }

    /// <summary>Whether the tab and, for a contextual tab, its group are visible; hidden tabs have no header.</summary>
    public bool IsTabVisible
    {
        get => _isTabVisible;
        private set => SetAndRaise(IsTabVisibleProperty, ref _isTabVisible, value);
    }

    /// <summary>The KeyTip, shown on the tab header.</summary>
    public string? KeyTip
    {
        get => GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>The Ribbon the tab is in.</summary>
    public Ribbon? Ribbon => GetValue(RibbonProperty) ?? this.FindLogicalAncestorOfType<Ribbon>();

    /// <summary>The groups (the realized group containers), in order.</summary>
    internal IEnumerable<RibbonGroup> Groups
    {
        get
        {
            for (var i = 0; i < Items.Count; i++)
            {
                if ((ContainerFromIndex(i) ?? Items[i] as Control) is RibbonGroup group)
                {
                    yield return group;
                }
            }
        }
    }

    /// <summary>Recomputes <see cref="IsTabVisible"/>.</summary>
    internal void UpdateIsTabVisible() => IsTabVisible = IsVisible && (_contextualTabGroup?.IsVisible ?? true);

    /// <summary>Undoes the last reduction, in reverse order of shrinking.</summary>
    /// <returns>True when a group grew.</returns>
    internal bool IncreaseNextGroupSize()
    {
        var resized = false;
        var count = _automaticResizeOrder.Count;
        while (count > 0 && !resized)
        {
            var nextIndex = _automaticResizeOrder[count - 1];
            if (GroupAt(nextIndex) is { } group)
            {
                resized = group.IncreaseGroupSize(true);
            }

            _automaticResizeOrder.RemoveAt(count - 1);
            _groupAutoResizeIndex = nextIndex;
            count--;
        }

        if (!resized && GroupSizeReductionOrder is { } order && _groupReduceOrderLocation >= 0)
        {
            var location = _groupReduceOrderLocation;
            var statusCount = _groupReductionResizeStatus.Count;
            while (location >= 0 && !resized && statusCount > 0)
            {
                var wasResized = _groupReductionResizeStatus[statusCount - 1];
                _groupReductionResizeStatus.RemoveAt(statusCount - 1);
                statusCount--;
                if (!wasResized)
                {
                    location--;
                    continue;
                }

                var group = FindGroup(order[location--]);
                resized = group is not null && group.IncreaseGroupSize(true);
            }

            _groupReduceOrderLocation = location;
        }

        return resized;
    }

    /// <summary>Shrinks the next group from <see cref="GroupSizeReductionOrder"/>, then right to left, cyclically.</summary>
    /// <returns>True when a group shrank.</returns>
    internal bool DecreaseNextGroupSize()
    {
        var resized = false;
        if (GroupSizeReductionOrder is { } order)
        {
            while (_groupReduceOrderLocation < order.Count - 1 && !resized)
            {
                var group = FindGroup(order[++_groupReduceOrderLocation]);
                resized = group is not null && group.DecreaseGroupSize();
                _groupReductionResizeStatus.Add(resized);
            }
        }

        if (!resized)
        {
            resized = DefaultCyclicalReduceGroup();
        }

        return resized;
    }

    /// <summary>Returns every group to its largest step and forgets the reductions.</summary>
    internal void ResetGroupSizes()
    {
        _automaticResizeOrder.Clear();
        _groupReductionResizeStatus.Clear();
        _groupAutoResizeIndex = null;
        _groupReduceOrderLocation = -1;
        foreach (var group in Groups)
        {
            group.ResetGroupSize();
        }
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<RibbonGroup>(item, out recycleKey);

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new RibbonGroup();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is RibbonGroup group && container != item)
        {
            group.Header = item;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty)
        {
            PseudoClasses.Set(":selected", change.GetNewValue<bool>());
            (this.GetVisualParent() as Avalonia.Layout.Layoutable)?.InvalidateMeasure();
        }
        else if (change.Property == IsVisibleProperty)
        {
            UpdateIsTabVisible();
        }
        else if (change.Property == ContextualTabGroupHeaderProperty)
        {
            Ribbon?.UpdateContextualTabGroups();
        }
        else if (change.Property == GroupSizeReductionOrderProperty)
        {
            ResetGroupSizes();
            this.FindDescendantOfType<Primitives.RibbonGroupsPanel>()?.InvalidateGroupSizes();
        }
    }

    private RibbonGroup? GroupAt(int index) =>
        index >= 0 && index < Items.Count ? (ContainerFromIndex(index) ?? Items[index] as Control) as RibbonGroup : null;

    private RibbonGroup? FindGroup(string name)
    {
        // Indexed, not through the Groups iterator, to avoid allocating in measure.
        for (var i = 0; i < Items.Count; i++)
        {
            if (GroupAt(i) is { } group && group.Name == name)
            {
                return group;
            }
        }

        return null;
    }

    /// <summary>From right to left, finds the next group that can shrink, wrapping around to the right end.</summary>
    private bool DefaultCyclicalReduceGroup()
    {
        if (Items.Count == 0)
        {
            return false;
        }

        _groupAutoResizeIndex ??= Items.Count - 1;
        var resized = false;
        var remain = true;
        while (remain && !resized)
        {
            var attempts = 0;
            do
            {
                attempts++;
                var index = _groupAutoResizeIndex.Value;
                _groupAutoResizeIndex = index - 1;
                if (GroupAt(index) is { } group)
                {
                    resized = group.DecreaseGroupSize();
                }

                if (resized)
                {
                    _automaticResizeOrder.Add(index);
                }

                if (_groupAutoResizeIndex < 0)
                {
                    _groupAutoResizeIndex = Items.Count - 1;
                    break;
                }
            }
            while (!resized);

            if (attempts >= Items.Count || (!resized && attempts > 0 && AllAtSmallest()))
            {
                remain = false;
            }
        }

        return resized;
    }

    private bool AllAtSmallest()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (GroupAt(i) is { } group && group.SizeDefinitionIndex < group.ActualGroupSizeDefinitions.Count - 1)
            {
                return false;
            }
        }

        return true;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Reset:
                _groupAutoResizeIndex = null;
                _groupReduceOrderLocation = -1;
                _automaticResizeOrder.Clear();
                _groupReductionResizeStatus.Clear();
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                for (var i = 0; i < e.OldItems.Count; i++)
                {
                    var deleted = i + e.OldStartingIndex;
                    for (var j = 0; j < _automaticResizeOrder.Count; j++)
                    {
                        if (deleted == _automaticResizeOrder[j])
                        {
                            _automaticResizeOrder.RemoveAt(j--);
                        }
                        else if (deleted < _automaticResizeOrder[j])
                        {
                            _automaticResizeOrder[j]--;
                        }
                    }

                    if (_groupAutoResizeIndex > deleted)
                    {
                        _groupAutoResizeIndex--;
                        if (_groupAutoResizeIndex < 0)
                        {
                            _groupAutoResizeIndex = Items.Count - 1;
                        }
                    }
                }

                break;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTabAutomationPeer(this);
}
