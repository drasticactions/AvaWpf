// Ported from WPF $W/PresentationFramework/System/Windows/Controls/GridViewHeaderRowPresenter.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Avalonia.Automation.Peers;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Controls;

/// <summary>
/// The header row of a <see cref="GridView"/>: one <see cref="GridViewColumnHeader"/> per column, then a padding header.
/// </summary>
/// <remarks>
/// Unless set locally, it takes its columns and header settings from the list's <see cref="GridView"/>.
/// </remarks>
public class GridViewHeaderRowPresenter : GridViewRowPresenterBase
{
    /// <summary>Defines the <see cref="ColumnHeaderTheme"/> property.</summary>
    public static readonly StyledProperty<ControlTheme?> ColumnHeaderThemeProperty =
        GridView.ColumnHeaderThemeProperty.AddOwner<GridViewHeaderRowPresenter>();

    /// <summary>Defines the <see cref="ColumnHeaderTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> ColumnHeaderTemplateProperty =
        GridView.ColumnHeaderTemplateProperty.AddOwner<GridViewHeaderRowPresenter>();

    /// <summary>Defines the <see cref="ColumnHeaderContextMenu"/> property.</summary>
    public static readonly StyledProperty<ContextMenu?> ColumnHeaderContextMenuProperty =
        GridView.ColumnHeaderContextMenuProperty.AddOwner<GridViewHeaderRowPresenter>();

    /// <summary>Defines the <see cref="ColumnHeaderToolTip"/> property.</summary>
    public static readonly StyledProperty<object?> ColumnHeaderToolTipProperty =
        GridView.ColumnHeaderToolTipProperty.AddOwner<GridViewHeaderRowPresenter>();

    /// <summary>Defines the <see cref="AllowsColumnReorder"/> property.</summary>
    public static readonly StyledProperty<bool> AllowsColumnReorderProperty =
        GridView.AllowsColumnReorderProperty.AddOwner<GridViewHeaderRowPresenter>();

    /// <summary>The distance the pointer must move sideways before a header drag starts.</summary>
    private const double ThresholdX = 4.0;

    private static readonly IBrush s_indicatorBrush = new ImmutableSolidColorBrush(Color.FromUInt32(0xFF000080));

    private readonly List<GridViewColumnHeader> _headers = new();
    private readonly List<Rect> _headersPositionList = new();
    private readonly List<IDisposable> _viewBindings = new();
    private GridViewColumnHeader? _paddingHeader;
    private GridViewColumnHeader? _floatingHeader;
    private Separator? _indicator;
    private ScrollViewer? _mainSV;
    private ScrollViewer? _headerSV;
    private ListView? _listView;
    private GridViewColumnHeader? _draggingSrcHeader;
    private Point _startPos;
    private Point _relativeStartPos;
    private Point _currentPos;
    private int _startColumnIndex;
    private int _desColumnIndex;
    private bool _isHeaderDragging;
    private bool _isColumnChangedOrCreated;
    private bool _prepareDragging;
    private bool _layoutUpdatedHooked;

    static GridViewHeaderRowPresenter()
    {
        ColumnHeaderThemeProperty.Changed.AddClassHandler<GridViewHeaderRowPresenter>((p, _) => p.UpdateAllHeaders());
        ColumnHeaderTemplateProperty.Changed.AddClassHandler<GridViewHeaderRowPresenter>((p, _) => p.UpdateAllHeaders());
        ColumnHeaderContextMenuProperty.Changed.AddClassHandler<GridViewHeaderRowPresenter>((p, _) => p.UpdateAllHeaders());
        ColumnHeaderToolTipProperty.Changed.AddClassHandler<GridViewHeaderRowPresenter>((p, _) => p.UpdateAllHeaders());
    }

    /// <summary>Initializes a new instance of the <see cref="GridViewHeaderRowPresenter"/> class.</summary>
    public GridViewHeaderRowPresenter()
    {
        AddHandler(PointerPressedEvent, OnHeaderPointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnHeaderPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnHeaderPointerReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>The theme of the column headers (WPF's <c>ColumnHeaderContainerStyle</c>).</summary>
    public ControlTheme? ColumnHeaderTheme
    {
        get => GetValue(ColumnHeaderThemeProperty);
        set => SetValue(ColumnHeaderThemeProperty, value);
    }

    /// <summary>The template of the header content of the columns without a <see cref="GridViewColumn.HeaderTemplate"/>.</summary>
    public IDataTemplate? ColumnHeaderTemplate
    {
        get => GetValue(ColumnHeaderTemplateProperty);
        set => SetValue(ColumnHeaderTemplateProperty, value);
    }

    /// <summary>The context menu of the headers.</summary>
    public ContextMenu? ColumnHeaderContextMenu
    {
        get => GetValue(ColumnHeaderContextMenuProperty);
        set => SetValue(ColumnHeaderContextMenuProperty, value);
    }

    /// <summary>The tooltip of the headers.</summary>
    public object? ColumnHeaderToolTip
    {
        get => GetValue(ColumnHeaderToolTipProperty);
        set => SetValue(ColumnHeaderToolTipProperty, value);
    }

    /// <summary>Whether the user can move a column by dragging its header.</summary>
    public bool AllowsColumnReorder
    {
        get => GetValue(AllowsColumnReorderProperty);
        set => SetValue(AllowsColumnReorderProperty, value);
    }

    /// <summary>The column headers, in column order.</summary>
    internal IReadOnlyList<GridViewColumnHeader> Headers => _headers;

    /// <summary>The padding header after the last column.</summary>
    internal GridViewColumnHeader? PaddingHeader => _paddingHeader;

    /// <summary>The floating header shown while a header is dragged.</summary>
    internal GridViewColumnHeader? FloatingHeader => _floatingHeader;

    /// <summary>The drop indicator shown while a header is dragged.</summary>
    internal Control? Indicator => _indicator;

    /// <summary>Whether a header is being dragged.</summary>
    internal bool IsHeaderDragging => _isHeaderDragging;

    /// <inheritdoc/>
    internal override void BuildChildren()
    {
        if (_paddingHeader is null)
        {
            AddPaddingColumnHeader();
            AddIndicator();
            AddFloatingHeader();
        }

        while (_headers.Count > 0)
        {
            RemoveHeaderAt(_headers.Count - 1);
        }

        UpdateHeader(_paddingHeader!);
        if (Columns is { } columns)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                CreateAndInsertHeader(columns[i], i);
            }
        }

        BuildHeaderLinks();
        _isColumnChangedOrCreated = true;
    }

    /// <inheritdoc/>
    internal override void OnColumnPropertyChanged(GridViewColumn column, string propertyName)
    {
        var index = Columns?.IndexOf(column) ?? -1;
        if (index < 0 || index >= _headers.Count)
        {
            return;
        }

        var header = _headers[index];
        if (propertyName is nameof(GridViewColumn.Width) or nameof(GridViewColumn.ActualWidth))
        {
            InvalidateMeasure();
        }
        else if (propertyName == nameof(GridViewColumn.Header))
        {
            if (!header.IsInternalGenerated || column.Header is GridViewColumnHeader)
            {
                RemoveHeaderAt(index);
                CreateAndInsertHeader(column, index);
                BuildHeaderLinks();
            }
            else
            {
                UpdateHeaderContent(header);
            }
        }
        else if (propertyName == nameof(GridViewColumn.HeaderTemplate))
        {
            UpdateHeader(header);
        }
    }

    /// <inheritdoc/>
    internal override void OnColumnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnColumnCollectionChanged(e);
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Move:
                var header = _headers[e.OldStartingIndex];
                _headers.RemoveAt(e.OldStartingIndex);
                _headers.Insert(e.NewStartingIndex, header);
                VisualChildren.Remove(header);
                VisualChildren.Insert(VisualIndex(e.NewStartingIndex), header);
                break;
            case NotifyCollectionChangedAction.Add:
                CreateAndInsertHeader((GridViewColumn)e.NewItems![0]!, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Remove:
                RemoveHeaderAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                RemoveHeaderAt(e.OldStartingIndex);
                CreateAndInsertHeader((GridViewColumn)e.NewItems![0]!, e.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                while (_headers.Count > 0)
                {
                    RemoveHeaderAt(_headers.Count - 1);
                }

                if (Columns is { } columns)
                {
                    for (var i = 0; i < columns.Count; i++)
                    {
                        CreateAndInsertHeader(columns[i], i);
                    }
                }

                break;
        }

        BuildHeaderLinks();
        _isColumnChangedOrCreated = true;
        InvalidateMeasure();
    }

    /// <summary>Focuses the list (its selected item when there is one), as WPF does when a header is used.</summary>
    internal void MakeParentItemsControlGotFocus()
    {
        if (_listView is { IsKeyboardFocusWithin: false } list)
        {
            if (list.SelectedIndex >= 0 && list.ContainerFromIndex(list.SelectedIndex) is { } container && container.Focus())
            {
                return;
            }

            list.Focus();
        }
    }

    /// <summary>
    /// Measures the headers: an unmeasured auto-size column's header widens the column, the others get the column width.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        double maxHeight = 0.0;
        double accumulatedWidth = 0.0;
        if (Columns is { } columns)
        {
            for (var i = 0; i < columns.Count && i < _headers.Count; i++)
            {
                var child = _headers[i];
                var column = columns[i];
                var childConstraintWidth = Math.Max(0.0, availableSize.Width - accumulatedWidth);
                if (column.State == ColumnMeasureState.Init)
                {
                    HookLayoutUpdated();
                    child.Measure(new Size(childConstraintWidth, availableSize.Height));
                    DesiredWidthList[column] = column.EnsureWidth(child.DesiredSize.Width);
                    accumulatedWidth += column.DesiredWidth;
                }
                else if (column.State is ColumnMeasureState.Headered or ColumnMeasureState.Data)
                {
                    child.Measure(new Size(Math.Min(childConstraintWidth, column.DesiredWidth), availableSize.Height));
                    accumulatedWidth += column.DesiredWidth;
                }
                else
                {
                    child.Measure(new Size(Math.Min(childConstraintWidth, column.Width), availableSize.Height));
                    accumulatedWidth += column.Width;
                }

                maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            }
        }

        if (_paddingHeader is not null)
        {
            _paddingHeader.Measure(new Size(0.0, availableSize.Height));
            maxHeight = Math.Max(maxHeight, _paddingHeader.DesiredSize.Height);
        }

        accumulatedWidth += PaddingHeaderMinWidth;
        if (_isHeaderDragging)
        {
            _indicator?.Measure(availableSize);
            _floatingHeader?.Measure(availableSize);
        }

        return new Size(accumulatedWidth, maxHeight);
    }

    /// <summary>Arranges the headers at the column widths, the padding header after them, and the drag visuals.</summary>
    /// <param name="finalSize">The final size.</param>
    /// <returns>The size used.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        double accumulatedWidth = 0.0;
        var remainingWidth = finalSize.Width;
        Rect rect;
        _headersPositionList.Clear();
        if (Columns is { } columns)
        {
            for (var i = 0; i < columns.Count && i < _headers.Count; i++)
            {
                var column = columns[i];
                var width = Math.Max(0.0, Math.Min(remainingWidth, column.State == ColumnMeasureState.SpecificWidth ? column.Width : column.DesiredWidth));
                rect = new Rect(accumulatedWidth, 0.0, width, finalSize.Height);
                _headers[i].Arrange(rect);
                _headersPositionList.Add(rect);
                remainingWidth -= width;
                accumulatedWidth += width;
            }

            if (_isColumnChangedOrCreated)
            {
                foreach (var header in _headers)
                {
                    header.CheckWidthForPreviousHeaderGripper();
                }

                _paddingHeader?.CheckWidthForPreviousHeaderGripper();
                _isColumnChangedOrCreated = false;
            }
        }

        rect = new Rect(accumulatedWidth, 0.0, Math.Max(remainingWidth, 0.0), finalSize.Height);
        _paddingHeader?.Arrange(rect);
        _headersPositionList.Add(rect);

        if (_isHeaderDragging && _floatingHeader is not null && _indicator is not null)
        {
            _floatingHeader.Arrange(new Rect(new Point(_currentPos.X - _relativeStartPos.X, 0), _headersPositionList[_startColumnIndex].Size));
            var pos = FindPositionByIndex(_desColumnIndex);
            _indicator.Arrange(new Rect(pos, new Size(_indicator.Width, finalSize.Height)));
        }

        return finalSize;
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RenewEvents();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        SetScrollViewers(null, null);
        SetListView(null);
    }

    private void HookLayoutUpdated()
    {
        if (!_layoutUpdatedHooked)
        {
            _layoutUpdatedHooked = true;
            LayoutUpdated += OnLayoutUpdated;
        }
    }

    /// <summary>After the layout pass, marks the measured auto-size columns as headered and remeasures if one grew.</summary>
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        LayoutUpdated -= OnLayoutUpdated;
        _layoutUpdatedHooked = false;
        var desiredWidthChanged = false;
        if (Columns is { } columns)
        {
            foreach (var column in columns)
            {
                if (column.State == ColumnMeasureState.SpecificWidth)
                {
                    continue;
                }

                if (column.State == ColumnMeasureState.Init)
                {
                    column.State = ColumnMeasureState.Headered;
                }

                if (!DesiredWidthList.TryGetValue(column, out var seen) || Math.Abs(seen - column.DesiredWidth) > 1e-6)
                {
                    DesiredWidthList[column] = column.DesiredWidth;
                    desiredWidthChanged = true;
                }
            }
        }

        if (desiredWidthChanged)
        {
            InvalidateMeasure();
        }
    }

    /// <summary>The visual index of a column's header: column 0 is drawn last, so each gripper overlaps the next header.</summary>
    private int VisualIndex(int columnIndex) => 1 + (_headers.Count - 1 - columnIndex);

    private void BuildHeaderLinks()
    {
        GridViewColumnHeader? last = null;
        foreach (var header in _headers)
        {
            header.PreviousVisualHeader = last;
            last = header;
        }

        if (_paddingHeader is not null)
        {
            _paddingHeader.PreviousVisualHeader = last;
        }
    }

    private void CreateAndInsertHeader(GridViewColumn column, int index)
    {
        var container = column.Header as GridViewColumnHeader;
        if (container is not null)
        {
            // The header is its own container: take it from wherever it is.
            if (container.Parent is GridViewHeaderRowPresenter other)
            {
                other.ReleaseHeader(container);
            }
        }
        else
        {
            container = new GridViewColumnHeader { IsInternalGenerated = true };
        }

        container.Column = column;
        _headers.Insert(index, container);
        LogicalChildren.Add(container);
        VisualChildren.Insert(VisualIndex(index), container);
        UpdateHeader(container);
    }

    private void ReleaseHeader(GridViewColumnHeader header)
    {
        var index = _headers.IndexOf(header);
        if (index >= 0)
        {
            RemoveHeaderAt(index);
        }
    }

    private void RemoveHeaderAt(int index)
    {
        var header = _headers[index];
        _headers.RemoveAt(index);
        VisualChildren.Remove(header);
        LogicalChildren.Remove(header);
    }

    private void RenewEvents()
    {
        // Walk the templated parents up to the list. The scroll viewer whose templated parent is the list scrolls the
        // rows; the one that holds this presenter scrolls the headers.
        ScrollViewer? main = null;
        ListView? list = null;
        var current = TemplatedParent as Control;
        while (current is not null)
        {
            if (current is ListView lv)
            {
                list = lv;
                break;
            }

            if (current is ScrollViewer sv)
            {
                main = sv;
            }

            current = current.TemplatedParent as Control;
        }

        SetScrollViewers(main, Parent as ScrollViewer);
        SetListView(list);
    }

    private void SetScrollViewers(ScrollViewer? main, ScrollViewer? header)
    {
        if (_mainSV is not null)
        {
            _mainSV.ScrollChanged -= OnMasterScrollChanged;
        }

        if (_headerSV is not null)
        {
            _headerSV.ScrollChanged -= OnHeaderScrollChanged;
        }

        _mainSV = main;
        _headerSV = ReferenceEquals(main, header) ? null : header;
        if (_mainSV is not null)
        {
            _mainSV.ScrollChanged += OnMasterScrollChanged;
        }

        if (_headerSV is not null)
        {
            _headerSV.ScrollChanged += OnHeaderScrollChanged;
        }
    }

    private void SetListView(ListView? list)
    {
        if (_listView is not null)
        {
            _listView.ViewChanged -= OnViewChanged;
            _listView.RemoveHandler(KeyDownEvent, OnListKeyDown);
        }

        _listView = list;
        if (_listView is not null)
        {
            _listView.ViewChanged += OnViewChanged;
            _listView.AddHandler(KeyDownEvent, OnListKeyDown, RoutingStrategies.Bubble);
        }

        BindToView();
    }

    private void OnViewChanged(object? sender, EventArgs e) => BindToView();

    /// <summary>Takes the columns and header settings of the list's <see cref="GridView"/>, below any local value.</summary>
    private void BindToView()
    {
        foreach (var b in _viewBindings)
        {
            b.Dispose();
        }

        _viewBindings.Clear();
        if (_listView?.View is not GridView view)
        {
            return;
        }

        view.HeaderRowPresenter = this;
        if (SetValue(ColumnsProperty, view.Columns, BindingPriority.Template) is { } columns)
        {
            _viewBindings.Add(columns);
        }

        _viewBindings.Add(Bind(ColumnHeaderThemeProperty, view.GetObservable(GridView.ColumnHeaderThemeProperty), BindingPriority.Template));
        _viewBindings.Add(Bind(ColumnHeaderTemplateProperty, view.GetObservable(GridView.ColumnHeaderTemplateProperty), BindingPriority.Template));
        _viewBindings.Add(Bind(ColumnHeaderContextMenuProperty, view.GetObservable(GridView.ColumnHeaderContextMenuProperty), BindingPriority.Template));
        _viewBindings.Add(Bind(ColumnHeaderToolTipProperty, view.GetObservable(GridView.ColumnHeaderToolTipProperty), BindingPriority.Template));
        _viewBindings.Add(Bind(AllowsColumnReorderProperty, view.GetObservable(GridView.AllowsColumnReorderProperty), BindingPriority.Template));
    }

    private void OnMasterScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_headerSV is not null && _mainSV is not null && Math.Abs(_headerSV.Offset.X - _mainSV.Offset.X) > 0.01)
        {
            _headerSV.Offset = new Vector(_mainSV.Offset.X, 0);
        }
    }

    private void OnHeaderScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_headerSV is not null && _mainSV is not null && Math.Abs(_headerSV.Offset.X - _mainSV.Offset.X) > 0.01)
        {
            _mainSV.Offset = new Vector(_headerSV.Offset.X, _mainSV.Offset.Y);
        }
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (_isHeaderDragging && _draggingSrcHeader is { } src)
        {
            // Cancel the drag, but keep tracking the pointer as a fresh press.
            FinishHeaderDrag(true);
            PrepareHeaderDrag(src, _currentPos, _relativeStartPos, true);
            InvalidateArrange();
            e.Handled = true;
            return;
        }

        foreach (var header in _headers)
        {
            if (header.CancelResize())
            {
                e.Handled = true;
            }
        }
    }

    private void AddPaddingColumnHeader()
    {
        _paddingHeader = new GridViewColumnHeader
        {
            IsInternalGenerated = true,
            Role = GridViewColumnHeaderRole.Padding,
            MinWidth = 0,
            Padding = default,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            Focusable = false,
        };
        LogicalChildren.Add(_paddingHeader);
        VisualChildren.Insert(0, _paddingHeader);
    }

    private void AddIndicator()
    {
        _indicator = new Separator
        {
            IsVisible = false,
            Margin = default,
            Width = 2.0,
            Template = new FuncControlTemplate<Separator>((_, _) => new Border { Background = s_indicatorBrush }),
        };
        LogicalChildren.Add(_indicator);
        VisualChildren.Add(_indicator);
    }

    private void AddFloatingHeader()
    {
        _floatingHeader = new GridViewColumnHeader
        {
            IsInternalGenerated = true,
            Role = GridViewColumnHeaderRole.Floating,
            IsVisible = false,
            IsHitTestVisible = false,
        };
        LogicalChildren.Add(_floatingHeader);
        VisualChildren.Add(_floatingHeader);
    }

    private void UpdateFloatingHeader(GridViewColumnHeader src)
    {
        var floating = _floatingHeader!;
        floating.Theme = src.Theme;
        floating.FloatSourceHeader = src;
        floating.Width = src.Bounds.Width;
        floating.Height = src.Bounds.Height;
        floating.Column = src.Column;
        floating.MinWidth = src.MinWidth;
        floating.MinHeight = src.MinHeight;
        floating.ContentTemplate = src.ContentTemplate;
        floating.Content = src.Content is Visual ? null : src.Content;
        floating.UpdateFloatingHeaderCanvas();
    }

    private int FindIndexByPosition(Point pos, bool findNearestColumn)
    {
        var index = -1;
        if (pos.X < 0.0)
        {
            return 0;
        }

        for (var i = 0; i < _headersPositionList.Count; i++)
        {
            index++;
            var rect = _headersPositionList[i];
            var startX = rect.X;
            var endX = startX + rect.Width;
            if (pos.X >= startX && pos.X <= endX)
            {
                if (findNearestColumn)
                {
                    var midX = (startX + endX) * 0.5;
                    if (pos.X >= midX && i != _headersPositionList.Count - 1)
                    {
                        index++;
                    }
                }

                break;
            }
        }

        return index;
    }

    private Point FindPositionByIndex(int index) =>
        index >= 0 && index < _headersPositionList.Count ? new Point(_headersPositionList[index].X, 0) : default;

    private void UpdateHeader(GridViewColumnHeader header)
    {
        UpdateHeaderContent(header);
        var column = header.Column;
        header.SetValue(ThemeProperty, ColumnHeaderTheme, BindingPriority.Template);
        header.SetValue(ContextMenuProperty, ColumnHeaderContextMenu, BindingPriority.Template);
        header.SetValue(ToolTip.TipProperty, ColumnHeaderToolTip, BindingPriority.Template);
        if (header.Role != GridViewColumnHeaderRole.Padding)
        {
            header.SetValue(ContentControl.ContentTemplateProperty, column?.HeaderTemplate ?? ColumnHeaderTemplate, BindingPriority.Template);
        }
    }

    private static void UpdateHeaderContent(GridViewColumnHeader header)
    {
        if (header.IsInternalGenerated && header.Column is { } column)
        {
            header.Content = column.Header;
        }
    }

    private void UpdateAllHeaders()
    {
        foreach (var header in _headers)
        {
            UpdateHeader(header);
        }

        if (_paddingHeader is not null)
        {
            UpdateHeader(_paddingHeader);
        }
    }

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // A press on a gripper resizes instead.
        var source = e.Source as Visual;
        if (source?.FindAncestorOfType<Thumb>(includeSelf: true) is not null)
        {
            return;
        }

        if (source?.FindAncestorOfType<GridViewColumnHeader>(includeSelf: true) is { } header && header.Parent == this && AllowsColumnReorder)
        {
            PrepareHeaderDrag(header, e.GetPosition(this), e.GetPosition(header), false);
            MakeParentItemsControlGotFocus();
        }
    }

    private void OnHeaderPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_prepareDragging || _draggingSrcHeader is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _currentPos = e.GetPosition(this);
        _desColumnIndex = FindIndexByPosition(_currentPos, true);
        if (!_isHeaderDragging)
        {
            if (Math.Abs(_currentPos.X - _startPos.X) > ThresholdX)
            {
                StartHeaderDrag();
                InvalidateMeasure();
            }
        }
        else
        {
            var visible = IsMousePositionValid(_floatingHeader!, _currentPos, 2.0);
            _indicator!.IsVisible = visible;
            _floatingHeader!.IsVisible = visible;
            InvalidateArrange();
        }
    }

    private void OnHeaderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _prepareDragging = false;
        if (_isHeaderDragging)
        {
            _currentPos = e.GetPosition(this);
            FinishHeaderDrag(false);
        }
    }

    private void PrepareHeaderDrag(GridViewColumnHeader header, Point pos, Point relativePos, bool cancelInvoke)
    {
        if (header.Role != GridViewColumnHeaderRole.Normal)
        {
            return;
        }

        _prepareDragging = true;
        _isHeaderDragging = false;
        _draggingSrcHeader = header;
        _startPos = pos;
        _relativeStartPos = relativePos;
        if (!cancelInvoke)
        {
            _startColumnIndex = FindIndexByPosition(_startPos, false);
        }
    }

    private void StartHeaderDrag()
    {
        _startPos = _currentPos;
        _isHeaderDragging = true;
        _draggingSrcHeader!.SuppressClickEvent = true;
        Columns?.BlockWrite();
        UpdateFloatingHeader(_draggingSrcHeader);
    }

    private void FinishHeaderDrag(bool isCancel)
    {
        _prepareDragging = false;
        _isHeaderDragging = false;
        if (_draggingSrcHeader is not null)
        {
            _draggingSrcHeader.SuppressClickEvent = false;
        }

        _floatingHeader!.IsVisible = false;
        _floatingHeader.ResetFloatingHeaderCanvasBackground();
        _indicator!.IsVisible = false;
        Columns?.UnblockWrite();

        if (!isCancel && Columns is { } columns)
        {
            var isMoveHeader = IsMousePositionValid(_floatingHeader, _currentPos, 2.0);
            var newColumnIndex = _startColumnIndex >= _desColumnIndex ? _desColumnIndex : _desColumnIndex - 1;
            newColumnIndex = Math.Clamp(newColumnIndex, 0, Math.Max(0, columns.Count - 1));
            if (isMoveHeader && _startColumnIndex >= 0 && _startColumnIndex < columns.Count && newColumnIndex != _startColumnIndex)
            {
                columns.Move(_startColumnIndex, newColumnIndex);
            }
        }

        InvalidateMeasure();
    }

    /// <summary>Whether the pointer is within two header heights above or below the header row.</summary>
    private static bool IsMousePositionValid(Control floatingHeader, Point currentPos, double arrange)
    {
        var height = double.IsNaN(floatingHeader.Height) ? floatingHeader.Bounds.Height : floatingHeader.Height;
        return -height * arrange <= currentPos.Y && currentPos.Y <= height * (arrange + 1);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new GridViewHeaderRowPresenterAutomationPeer(this);
}
