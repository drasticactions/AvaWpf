// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/Ribbon.cs (MIT, see NOTICE.md).
using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>
/// The Ribbon: a row of tab headers with the application menu and Quick Access Toolbar, and the groups of the
/// selected <see cref="RibbonTab"/> below.
/// </summary>
/// <remarks>
/// Commands are plain <see cref="System.Windows.Input.ICommand"/>s; the context menu actions are methods such as
/// <see cref="AddToQuickAccessToolBar"/>.
/// </remarks>
[TemplatePart("PART_TabHeaderItemsControl", typeof(RibbonTabHeaderItemsControl))]
[TemplatePart("PART_ContextualTabGroupItemsControl", typeof(RibbonContextualTabGroupItemsControl))]
[TemplatePart("PART_ItemsPresenterPopup", typeof(Popup))]
[TemplatePart("PART_MainItemsPresenterHost", typeof(Decorator))]
[TemplatePart("PART_PopupItemsPresenterHost", typeof(Decorator))]
[TemplatePart("PART_GroupsBorder", typeof(Control))]
[TemplatePart("PART_QatTopHost", typeof(Decorator))]
[TemplatePart("PART_QatBottomHost", typeof(Decorator))]
[PseudoClasses(":minimized", ":dropdownopen", ":collapsed", ":qatbelow", ":hasqat", ":hosted")]
public class Ribbon : SelectingItemsControl
{
    /// <summary>Defines the <see cref="ApplicationMenu"/> property.</summary>
    public static readonly StyledProperty<RibbonApplicationMenu?> ApplicationMenuProperty =
        AvaloniaProperty.Register<Ribbon, RibbonApplicationMenu?>(nameof(ApplicationMenu));

    /// <summary>Defines the <see cref="QuickAccessToolBar"/> property.</summary>
    public static readonly StyledProperty<RibbonQuickAccessToolBar?> QuickAccessToolBarProperty =
        AvaloniaProperty.Register<Ribbon, RibbonQuickAccessToolBar?>(nameof(QuickAccessToolBar));

    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<Ribbon, object?>(nameof(Title));

    /// <summary>Defines the <see cref="TitleTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty =
        AvaloniaProperty.Register<Ribbon, IDataTemplate?>(nameof(TitleTemplate));

    /// <summary>Defines the <see cref="IsMinimized"/> property.</summary>
    public static readonly StyledProperty<bool> IsMinimizedProperty =
        AvaloniaProperty.Register<Ribbon, bool>(nameof(IsMinimized), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="IsDropDownOpen"/> property.</summary>
    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<Ribbon, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="IsCollapsed"/> property.</summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<Ribbon, bool>(nameof(IsCollapsed));

    /// <summary>Defines the <see cref="ShowQuickAccessToolBarOnTop"/> property.</summary>
    public static readonly StyledProperty<bool> ShowQuickAccessToolBarOnTopProperty =
        AvaloniaProperty.Register<Ribbon, bool>(nameof(ShowQuickAccessToolBarOnTop), true);

    /// <summary>Defines the <see cref="HelpPaneContent"/> property.</summary>
    public static readonly StyledProperty<object?> HelpPaneContentProperty =
        AvaloniaProperty.Register<Ribbon, object?>(nameof(HelpPaneContent));

    /// <summary>Defines the <see cref="HelpPaneContentTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> HelpPaneContentTemplateProperty =
        AvaloniaProperty.Register<Ribbon, IDataTemplate?>(nameof(HelpPaneContentTemplate));

    /// <summary>Defines the <see cref="ContextualTabGroupsSource"/> property.</summary>
    public static readonly StyledProperty<IEnumerable?> ContextualTabGroupsSourceProperty =
        AvaloniaProperty.Register<Ribbon, IEnumerable?>(nameof(ContextualTabGroupsSource));

    /// <summary>Defines the <see cref="CollapseWidth"/> property.</summary>
    public static readonly StyledProperty<double> CollapseWidthProperty =
        AvaloniaProperty.Register<Ribbon, double>(nameof(CollapseWidth), 300);

    /// <summary>Defines the <see cref="IsHostedInRibbonWindow"/> property.</summary>
    public static readonly DirectProperty<Ribbon, bool> IsHostedInRibbonWindowProperty =
        AvaloniaProperty.RegisterDirect<Ribbon, bool>(nameof(IsHostedInRibbonWindow), o => o.IsHostedInRibbonWindow);

    /// <summary>Raised when the Ribbon is minimized.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CollapsedEvent =
        RoutedEvent.Register<Ribbon, RoutedEventArgs>(nameof(Collapsed), RoutingStrategies.Bubble);

    /// <summary>Raised when the Ribbon is restored from minimized.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ExpandedEvent =
        RoutedEvent.Register<Ribbon, RoutedEventArgs>(nameof(Expanded), RoutingStrategies.Bubble);

    private readonly AvaloniaList<RibbonTabHeader> _tabHeaders = new();
    private readonly RibbonContextMenu _contextMenu = new();
    private RibbonTabHeaderItemsControl? _tabHeaderItemsControl;
    private RibbonContextualTabGroupItemsControl? _templateContextualTabGroupItemsControl;
    private RibbonCaption? _caption;
    private bool _isHostedInRibbonWindow;
    private Decorator? _mainHost;
    private Decorator? _popupHost;
    private Control? _groupsBorder;
    private Decorator? _qatTopHost;
    private Decorator? _qatBottomHost;
    private TopLevel? _topLevel;
    private bool _selectedTabClicked;
    private bool _autoCollapsed;

    static Ribbon()
    {
        KeyTipService.EnsureRegistered();
        RibbonControlService.DismissPopupEvent.AddClassHandler<Ribbon>((r, e) => r.OnDismissPopup(e));
        InputElement.ContextRequestedEvent.AddClassHandler<Ribbon>((r, e) => r.OnContextRequested(e));
    }

    /// <summary>Initializes a new instance of the <see cref="Ribbon"/> class.</summary>
    public Ribbon()
    {
        SetValue(RibbonControlService.RibbonProperty, this);
        SelectionMode = SelectionMode.Single | SelectionMode.AlwaysSelected;
        Items.CollectionChanged += (_, _) => RebuildTabHeaders();
        ContextualTabGroups.CollectionChanged += (_, _) => UpdateContextualTabGroups();
        LayoutUpdated += OnLayoutUpdated;
    }

    /// <summary>Raised when the Ribbon is minimized.</summary>
    public event EventHandler<RoutedEventArgs>? Collapsed
    {
        add => AddHandler(CollapsedEvent, value);
        remove => RemoveHandler(CollapsedEvent, value);
    }

    /// <summary>Raised when the Ribbon is restored from minimized.</summary>
    public event EventHandler<RoutedEventArgs>? Expanded
    {
        add => AddHandler(ExpandedEvent, value);
        remove => RemoveHandler(ExpandedEvent, value);
    }

    /// <summary>The application menu at the left of the tab row.</summary>
    public RibbonApplicationMenu? ApplicationMenu
    {
        get => GetValue(ApplicationMenuProperty);
        set => SetValue(ApplicationMenuProperty, value);
    }

    /// <summary>The Quick Access Toolbar.</summary>
    public RibbonQuickAccessToolBar? QuickAccessToolBar
    {
        get => GetValue(QuickAccessToolBarProperty);
        set => SetValue(QuickAccessToolBarProperty, value);
    }

    /// <summary>The title shown in the title row.</summary>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The template of <see cref="Title"/>.</summary>
    public IDataTemplate? TitleTemplate
    {
        get => GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }

    /// <summary>Whether the Ribbon shows only its tab row.</summary>
    public bool IsMinimized
    {
        get => GetValue(IsMinimizedProperty);
        set => SetValue(IsMinimizedProperty, value);
    }

    /// <summary>Whether the groups of a minimized Ribbon are open in a popup.</summary>
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Whether the Ribbon is hidden; it collapses itself while narrower than <see cref="CollapseWidth"/>.</summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>Whether the Quick Access Toolbar is above the tabs (default) or below the groups.</summary>
    public bool ShowQuickAccessToolBarOnTop
    {
        get => GetValue(ShowQuickAccessToolBarOnTopProperty);
        set => SetValue(ShowQuickAccessToolBarOnTopProperty, value);
    }

    /// <summary>The content at the right end of the tab row (a help button).</summary>
    public object? HelpPaneContent
    {
        get => GetValue(HelpPaneContentProperty);
        set => SetValue(HelpPaneContentProperty, value);
    }

    /// <summary>The template of <see cref="HelpPaneContent"/>.</summary>
    public IDataTemplate? HelpPaneContentTemplate
    {
        get => GetValue(HelpPaneContentTemplateProperty);
        set => SetValue(HelpPaneContentTemplateProperty, value);
    }

    /// <summary>The contextual tab groups.</summary>
    public AvaloniaList<RibbonContextualTabGroup> ContextualTabGroups { get; } = new();

    /// <summary>A source of contextual tab groups; null uses <see cref="ContextualTabGroups"/>.</summary>
    public IEnumerable? ContextualTabGroupsSource
    {
        get => GetValue(ContextualTabGroupsSourceProperty);
        set => SetValue(ContextualTabGroupsSourceProperty, value);
    }

    /// <summary>The width below which the Ribbon collapses (default 300, as in WPF).</summary>
    public double CollapseWidth
    {
        get => GetValue(CollapseWidthProperty);
        set => SetValue(CollapseWidthProperty, value);
    }

    /// <summary>
    /// Whether the Ribbon's toolbar and group headers are in a <see cref="RibbonWindow"/> caption; sets <c>:hosted</c>,
    /// which hides the title row.
    /// </summary>
    public bool IsHostedInRibbonWindow
    {
        get => _isHostedInRibbonWindow;
        private set => SetAndRaise(IsHostedInRibbonWindowProperty, ref _isHostedInRibbonWindow, value);
    }

    /// <summary>The row of contextual tab group headers in use: the caption's while hosted, else the template's.</summary>
    private RibbonContextualTabGroupItemsControl? ActiveContextualTabGroupItemsControl =>
        _caption?.ContextualTabGroups ?? _templateContextualTabGroupItemsControl;

    /// <summary>The tab headers, one per tab, in tab order.</summary>
    internal IReadOnlyList<RibbonTabHeader> TabHeaders => _tabHeaders;

    /// <summary>The context menu shown on right-click.</summary>
    internal RibbonContextMenu ContextMenuForTests => _contextMenu;

    /// <summary>The groups area of the template (below the tabs, or in the popup while minimized).</summary>
    internal Control? GroupsBorder => _groupsBorder;

    /// <summary>Adds a copy of a control to the Quick Access Toolbar, raising <see cref="RibbonQuickAccessToolBar.CloneEvent"/>.</summary>
    /// <param name="element">The control (or an element inside it).</param>
    /// <returns>True when a copy was added.</returns>
    public bool AddToQuickAccessToolBar(Control element)
    {
        if (QuickAccessToolBar is not { } qat || FindElementThatCanBeAddedToQuickAccessToolBar(element) is not { } target)
        {
            return false;
        }

        var id = RibbonHelper.QuickAccessToolBarId(target);
        if (id is null || qat.ContainsId(id))
        {
            return false;
        }

        var args = new RibbonQuickAccessToolBarCloneEventArgs(target);
        target.RaiseEvent(args);
        var clone = args.CloneInstance ?? RibbonClone.Create(target);
        if (clone is null)
        {
            return false;
        }

        qat.Items.Add(clone);
        return true;
    }

    /// <summary>Removes a control (or the item with its Quick Access Toolbar id) from the Quick Access Toolbar.</summary>
    /// <param name="element">The toolbar item, or the original control.</param>
    /// <returns>True when an item was removed.</returns>
    public bool RemoveFromQuickAccessToolBar(Control element)
    {
        if (QuickAccessToolBar is not { } qat)
        {
            return false;
        }

        if (qat.Items.Contains(element))
        {
            qat.Items.Remove(element);
            return true;
        }

        var id = RibbonHelper.QuickAccessToolBarId(element);
        foreach (var item in qat.Items)
        {
            if (id is not null && item is AvaloniaObject o && Equals(RibbonHelper.QuickAccessToolBarId(o), id))
            {
                qat.Items.Remove(item);
                return true;
            }
        }

        return false;
    }

    /// <summary>The click logic of a tab header (WPF's <c>NotifyMouseClickedOnTabHeader</c>).</summary>
    internal void NotifyMouseClickedOnTabHeader(RibbonTabHeader header, int clickCount)
    {
        var index = header.Index;
        if (clickCount == 1)
        {
            // Maximized: select the tab. Minimized: open the popup for a new tab, toggle it for the selected one.
            if (SelectedIndex < 0 || SelectedIndex != index)
            {
                SelectedIndex = index;
                if (IsMinimized)
                {
                    IsDropDownOpen = true;
                }

                _selectedTabClicked = false;
            }
            else
            {
                if (IsMinimized)
                {
                    IsDropDownOpen = !IsDropDownOpen;
                }

                _selectedTabClicked = true;
            }
        }
        else if (clickCount == 2)
        {
            // A second click on the selected tab toggles minimized.
            if (_selectedTabClicked || IsMinimized)
            {
                IsMinimized = !IsMinimized;
                IsDropDownOpen = false;
                _selectedTabClicked = false;
            }
            else
            {
                _selectedTabClicked = true;
            }
        }
        else if (clickCount == 3)
        {
            if (_selectedTabClicked)
            {
                IsMinimized = !IsMinimized;
                IsDropDownOpen = false;
            }
            else
            {
                IsDropDownOpen = true;
            }
        }
    }

    /// <summary>Selects the first visible tab of a contextual tab group.</summary>
    internal void SelectFirstTabOf(RibbonContextualTabGroup group)
    {
        foreach (var header in _tabHeaders)
        {
            if (header.RibbonTab is { IsTabVisible: true } tab && tab.ContextualTabGroup == group)
            {
                SelectedIndex = header.Index;
                if (IsMinimized)
                {
                    IsDropDownOpen = true;
                }

                return;
            }
        }
    }

    /// <summary>Measures the tab headers again (a contextual group title changed its ideal width).</summary>
    internal void InvalidateTabHeaders() => _tabHeaderItemsControl?.ItemsPanelRoot?.InvalidateMeasure();

    /// <summary>Links the tabs to their contextual tab groups and tints the headers.</summary>
    internal void UpdateContextualTabGroups()
    {
        foreach (var header in _tabHeaders)
        {
            if (header.RibbonTab is not { } tab)
            {
                continue;
            }

            RibbonContextualTabGroup? match = null;
            if (tab.ContextualTabGroupHeader is { } key)
            {
                for (var i = 0; i < ContextualTabGroupCount; i++)
                {
                    if (ContextualTabGroupAt(i) is { } group && Equals(group.Header, key))
                    {
                        match = group;
                        break;
                    }
                }
            }

            tab.ContextualTabGroup = match;
            header.ContextualTabGroup = match;
            header.ContextualTabGroupBackground = match?.Background;
        }

        EnsureVisibleSelection();
        InvalidateMeasure();
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<RibbonTab>(item, out recycleKey);

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new RibbonTab();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is RibbonTab tab && container != item && !tab.IsSet(HeaderedItemsControl.HeaderProperty))
        {
            tab.Header = item;
        }
    }

    /// <inheritdoc/>
    protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
    {
        base.ContainerForItemPreparedOverride(container, item, index);
        RebuildTabHeaders();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_tabHeaderItemsControl is not null)
        {
            _tabHeaderItemsControl.ItemsSource = null;
        }

        _tabHeaderItemsControl = e.NameScope.Find<RibbonTabHeaderItemsControl>("PART_TabHeaderItemsControl");
        if (_templateContextualTabGroupItemsControl is not null)
        {
            _templateContextualTabGroupItemsControl.ItemsSource = null;
        }

        _templateContextualTabGroupItemsControl = e.NameScope.Find<RibbonContextualTabGroupItemsControl>("PART_ContextualTabGroupItemsControl");
        _mainHost = e.NameScope.Find<Decorator>("PART_MainItemsPresenterHost");
        _popupHost = e.NameScope.Find<Decorator>("PART_PopupItemsPresenterHost");
        _groupsBorder = e.NameScope.Find<Control>("PART_GroupsBorder");
        if (_qatTopHost is not null)
        {
            _qatTopHost.Child = null;
        }

        if (_qatBottomHost is not null)
        {
            _qatBottomHost.Child = null;
        }

        _qatTopHost = e.NameScope.Find<Decorator>("PART_QatTopHost");
        _qatBottomHost = e.NameScope.Find<Decorator>("PART_QatBottomHost");
        if (_tabHeaderItemsControl is not null)
        {
            _tabHeaderItemsControl.ItemsSource = _tabHeaders;
        }

        UpdateContextualTabGroupsSource();
        MoveGroups();
        MoveQuickAccessToolBar();
        RebuildTabHeaders();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Bubble);
        AttachToCaption();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        _topLevel = null;
        DetachFromCaption();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty)
        {
            UpdateHeaderStates();
        }
        else if (change.Property == IsMinimizedProperty)
        {
            var minimized = change.GetNewValue<bool>();
            PseudoClasses.Set(":minimized", minimized);
            SetCurrentValue(IsDropDownOpenProperty, false);
            MoveGroups();
            UpdateHeaderStates();
            RaiseEvent(new RoutedEventArgs(minimized ? CollapsedEvent : ExpandedEvent));
        }
        else if (change.Property == IsDropDownOpenProperty)
        {
            var open = change.GetNewValue<bool>();
            if (open && !IsMinimized)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
                return;
            }

            PseudoClasses.Set(":dropdownopen", open);
            if (open && _popupHost is not null)
            {
                // The popup spans the Ribbon, as WPF's groups popup does.
                _popupHost.Width = Bounds.Width;
            }

            UpdateHeaderStates();
        }
        else if (change.Property == IsCollapsedProperty)
        {
            PseudoClasses.Set(":collapsed", change.GetNewValue<bool>());
            UpdateCaption();
        }
        else if (change.Property == ShowQuickAccessToolBarOnTopProperty)
        {
            PseudoClasses.Set(":qatbelow", !change.GetNewValue<bool>());
            MoveQuickAccessToolBar();
        }
        else if (change.Property == QuickAccessToolBarProperty)
        {
            PseudoClasses.Set(":hasqat", change.NewValue is not null);
            if (change.OldValue is RibbonQuickAccessToolBar old)
            {
                old.SetIsInCaption(false);
                if (_caption?.QuickAccessToolBarHost.Content == old)
                {
                    _caption.QuickAccessToolBarHost.Content = null;
                }

                if (_qatTopHost?.Child == old)
                {
                    _qatTopHost.Child = null;
                }

                if (_qatBottomHost?.Child == old)
                {
                    _qatBottomHost.Child = null;
                }
            }

            MoveQuickAccessToolBar();
        }
        else if (change.Property == ContextualTabGroupsSourceProperty)
        {
            UpdateContextualTabGroupsSource();
        }
    }

    /// <summary>Closes the popup of a minimized Ribbon when a control in it is used.</summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnDismissPopup(RibbonDismissPopupEventArgs e)
    {
        if (IsDropDownOpen && (e.DismissMode == RibbonDismissPopupMode.Always || !IsPointerOver))
        {
            IsDropDownOpen = false;
        }
    }

    /// <summary>The number of contextual tab groups (containers once the row is templated).</summary>
    private int ContextualTabGroupCount => ActiveContextualTabGroupItemsControl?.ItemCount ?? ContextualTabGroups.Count;

    /// <summary>A contextual tab group by index; no iterator, because it is read after every layout pass.</summary>
    private RibbonContextualTabGroup? ContextualTabGroupAt(int index) => ActiveContextualTabGroupItemsControl is { } itemsControl
        ? itemsControl.ContainerFromIndex(index) as RibbonContextualTabGroup
        : ContextualTabGroups[index];

    /// <summary>Puts the groups in the popup while minimized, and below the tab row otherwise.</summary>
    private void MoveGroups()
    {
        if (_groupsBorder is null || _mainHost is null || _popupHost is null)
        {
            return;
        }

        var target = IsMinimized ? _popupHost : _mainHost;
        var other = IsMinimized ? _mainHost : _popupHost;
        if (target.Child == _groupsBorder)
        {
            return;
        }

        other.Child = null;
        target.Child = _groupsBorder;
    }

    /// <summary>Puts the Quick Access Toolbar above the tabs (in the caption while hosted) or below the groups.</summary>
    private void MoveQuickAccessToolBar()
    {
        var qat = QuickAccessToolBar;
        ContentControl? captionHost = _caption?.QuickAccessToolBarHost;
        Decorator? top = captionHost is null ? _qatTopHost : null;
        var onTop = ShowQuickAccessToolBarOnTop;

        // Take the toolbar out of every place it should not be first: a control has one visual parent.
        if (_qatTopHost is not null && (_qatTopHost.Child != qat || top is null || !onTop))
        {
            _qatTopHost.Child = null;
        }

        if (_qatBottomHost is not null && (_qatBottomHost.Child != qat || onTop))
        {
            _qatBottomHost.Child = null;
        }

        if (captionHost is not null && (captionHost.Content != qat || !onTop))
        {
            captionHost.Content = null;
        }

        qat?.SetIsInCaption(onTop && captionHost is not null);
        if (onTop)
        {
            if (captionHost is not null)
            {
                captionHost.Content = qat;
            }
            else if (top is not null)
            {
                top.Child = qat;
            }
        }
        else if (_qatBottomHost is not null)
        {
            _qatBottomHost.Child = qat;
        }

        UpdateCaption();
    }

    /// <summary>Takes the caption of the window (or frame) the Ribbon is in, when it offers one.</summary>
    private void AttachToCaption()
    {
        if (_caption is not null || this.FindAncestorOfType<WindowFrame>() is not { } frame ||
            RibbonWindow.CaptionFor(frame) is not { } caption || !caption.TryAttach(this))
        {
            return;
        }

        _caption = caption;
        IsHostedInRibbonWindow = true;
        PseudoClasses.Set(":hosted", true);
        UpdateContextualTabGroupsSource();
        MoveQuickAccessToolBar();
    }

    /// <summary>Gives the caption back and takes the toolbar and the group headers home.</summary>
    private void DetachFromCaption()
    {
        if (_caption is not { } caption)
        {
            return;
        }

        caption.Detach(this);
        _caption = null;
        IsHostedInRibbonWindow = false;
        PseudoClasses.Set(":hosted", false);
        UpdateContextualTabGroupsSource();
        MoveQuickAccessToolBar();
    }

    /// <summary>Shows the toolbar and the group headers in the caption while they have something to show.</summary>
    private void UpdateCaption() =>
        _caption?.Update(
            showToolBar: !IsCollapsed && ShowQuickAccessToolBarOnTop && QuickAccessToolBar is not null,
            showGroups: !IsCollapsed);

    /// <summary>Gives the contextual tab groups to the row in use, and none to the other.</summary>
    private void UpdateContextualTabGroupsSource()
    {
        var source = ContextualTabGroupsSource ?? ContextualTabGroups;
        var active = ActiveContextualTabGroupItemsControl;
        if (_templateContextualTabGroupItemsControl is { } template && template != active)
        {
            template.ItemsSource = null;
        }

        if (active is not null && !ReferenceEquals(active.ItemsSource, source))
        {
            active.ItemsSource = source;
        }

        UpdateContextualTabGroups();
    }

    /// <summary>Keeps one <see cref="RibbonTabHeader"/> per tab, in tab order.</summary>
    private void RebuildTabHeaders()
    {
        var existing = new Dictionary<object, RibbonTabHeader>();
        foreach (var header in _tabHeaders)
        {
            if (header.RibbonTab is { } tab)
            {
                existing[tab] = header;
            }
        }

        var wanted = new List<RibbonTabHeader>();
        for (var i = 0; i < Items.Count; i++)
        {
            if ((ContainerFromIndex(i) ?? Items[i] as Control) is not RibbonTab tab)
            {
                continue;
            }

            if (!existing.TryGetValue(tab, out var header))
            {
                header = CreateHeader(tab);
            }

            header.Index = i;
            wanted.Add(header);
        }

        var same = wanted.Count == _tabHeaders.Count;
        for (var i = 0; same && i < wanted.Count; i++)
        {
            same = wanted[i] == _tabHeaders[i];
        }

        if (!same)
        {
            _tabHeaders.Clear();
            _tabHeaders.AddRange(wanted);
        }

        UpdateContextualTabGroups();
        UpdateHeaderStates();
    }

    private RibbonTabHeader CreateHeader(RibbonTab tab)
    {
        var header = new RibbonTabHeader { RibbonTab = tab };
        header.Bind(ContentControl.ContentProperty, tab.GetObservable(HeaderedItemsControl.HeaderProperty));
        header.Bind(ContentControl.ContentTemplateProperty, tab.GetObservable(HeaderedItemsControl.HeaderTemplateProperty));
        header.Bind(KeyTipService.KeyTipProperty, tab.GetObservable(KeyTipService.KeyTipProperty));
        header.Bind(IsVisibleProperty, tab.GetObservable(RibbonTab.IsTabVisibleProperty));
        header.Bind(RibbonTabHeader.IsRibbonTabSelectedProperty, tab.GetObservable(RibbonTab.IsSelectedProperty));
        tab.PropertyChanged += (_, e) =>
        {
            if (e.Property == RibbonTab.IsTabVisibleProperty)
            {
                EnsureVisibleSelection();
            }
        };
        return header;
    }

    private void UpdateHeaderStates()
    {
        foreach (var header in _tabHeaders)
        {
            header.UpdateRibbonState(IsMinimized, IsDropDownOpen);
        }
    }

    /// <summary>Moves the selection off a tab that is hidden (a contextual tab whose group was hidden).</summary>
    private void EnsureVisibleSelection()
    {
        var selected = SelectedIndex >= 0 && SelectedIndex < Items.Count ? (ContainerFromIndex(SelectedIndex) ?? Items[SelectedIndex] as Control) as RibbonTab : null;
        if (selected is { IsTabVisible: true } || _tabHeaders.Count == 0)
        {
            return;
        }

        foreach (var header in _tabHeaders)
        {
            if (header.RibbonTab is { IsTabVisible: true })
            {
                SelectedIndex = header.Index;
                return;
            }
        }
    }

    /// <summary>
    /// After layout, collapses the Ribbon below <see cref="CollapseWidth"/> and copies contextual header positions to
    /// the group titles, so sizes are never read during arrange.
    /// </summary>
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        var tooNarrow = Bounds.Width > 0 && Bounds.Width < CollapseWidth;
        if (tooNarrow != _autoCollapsed)
        {
            _autoCollapsed = tooNarrow;
            SetCurrentValue(IsCollapsedProperty, tooNarrow);
        }

        if (_tabHeaderItemsControl is null || ActiveContextualTabGroupItemsControl is null)
        {
            return;
        }

        var changed = false;
        for (var i = 0; i < ContextualTabGroupCount; i++)
        {
            if (ContextualTabGroupAt(i) is not { } group)
            {
                continue;
            }

            double left = double.MaxValue, right = double.MinValue;
            foreach (var header in _tabHeaders)
            {
                if (header.ContextualTabGroup != group || !header.IsVisible || header.TranslatePoint(default, ActiveContextualTabGroupItemsControl) is not { } origin)
                {
                    continue;
                }

                left = Math.Min(left, origin.X);
                right = Math.Max(right, origin.X + header.Bounds.Width);
            }

            var (newLeft, newWidth) = right > left ? (left, right - left) : (0.0, 0.0);
            if (Math.Abs(newLeft - group.TabsLeft) > 0.5 || Math.Abs(newWidth - group.TabsWidth) > 0.5)
            {
                group.TabsLeft = newLeft;
                group.TabsWidth = newWidth;
                changed = true;
            }
        }

        if (changed)
        {
            ActiveContextualTabGroupItemsControl.ItemsPanelRoot?.InvalidateMeasure();
        }
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1 && e.KeyModifiers == KeyModifiers.Control && !IsCollapsed)
        {
            IsMinimized = !IsMinimized;
            e.Handled = true;
        }
    }

    /// <summary>The nearest element at or above <paramref name="element"/> that can be copied to the Quick Access Toolbar.</summary>
    internal static Control? FindElementThatCanBeAddedToQuickAccessToolBar(Control? element)
    {
        for (var e = element; e is not null; e = e.GetLogicalParent() as Control ?? e.GetVisualParent() as Control)
        {
            if (e is Ribbon or RibbonTab or RibbonQuickAccessToolBar)
            {
                return null;
            }

            if (e.GetValue(RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty) && RibbonClone.CanClone(e) && e.TemplatedParent is null)
            {
                return e;
            }
        }

        return null;
    }

    private void OnContextRequested(ContextRequestedEventArgs e)
    {
        if (e.Handled || e.Source is not Control source)
        {
            return;
        }

        var inQat = source.GetValue(RibbonControlService.IsInQuickAccessToolBarProperty);
        var qatItem = inQat ? QatItemOf(source) : null;
        var target = inQat ? null : FindElementThatCanBeAddedToQuickAccessToolBar(source);
        var items = new List<object>();
        if (qatItem is not null)
        {
            items.Add(MenuItem(RibbonStrings.RemoveFromQuickAccessToolBar, () => RemoveFromQuickAccessToolBar(qatItem)));
        }
        else if (QuickAccessToolBar is { } qat)
        {
            var id = target is null ? null : RibbonHelper.QuickAccessToolBarId(target);
            var add = MenuItem(target is RibbonGallery ? RibbonStrings.AddGalleryToQuickAccessToolBar : RibbonStrings.AddToQuickAccessToolBar,
                () => { if (target is not null) { AddToQuickAccessToolBar(target); } });
            add.IsEnabled = target is not null && id is not null && !qat.ContainsId(id);
            items.Add(add);
        }

        if (QuickAccessToolBar is not null)
        {
            items.Add(new RibbonSeparator());
            items.Add(MenuItem(ShowQuickAccessToolBarOnTop ? RibbonStrings.ShowQuickAccessToolBarBelow : RibbonStrings.ShowQuickAccessToolBarAbove,
                () => ShowQuickAccessToolBarOnTop = !ShowQuickAccessToolBarOnTop));
            items.Add(new RibbonSeparator());
        }

        items.Add(MenuItem(IsMinimized ? RibbonStrings.MaximizeTheRibbon : RibbonStrings.MinimizeTheRibbon, () => IsMinimized = !IsMinimized));
        _contextMenu.ItemsSource = items;
        _contextMenu.Open(source);
        e.Handled = true;
    }

    private Control? QatItemOf(Control source)
    {
        if (QuickAccessToolBar is not { } qat)
        {
            return null;
        }

        for (Control? e = source; e is not null; e = e.GetLogicalParent() as Control)
        {
            if (qat.Items.Contains(e))
            {
                return e;
            }
        }

        return null;
    }

    private static RibbonMenuItem MenuItem(string header, Action action)
    {
        var item = new RibbonMenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonAutomationPeer(this);
}
