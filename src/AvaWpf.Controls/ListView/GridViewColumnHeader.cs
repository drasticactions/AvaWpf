// Ported from WPF $W/PresentationFramework/System/Windows/Controls/GridViewColumnHeader.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Automation.Peers;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Controls;

/// <summary>
/// A header of the <see cref="GridView"/> header row; its <c>PART_HeaderGripper</c> resizes the column.
/// </summary>
/// <remarks>
/// The <c>:floating</c> and <c>:padding</c> pseudo-classes mark the non-normal <see cref="Role"/>s for the theme.
/// </remarks>
[TemplatePart(PartHeaderGripper, typeof(Thumb))]
[TemplatePart(PartFloatingHeaderCanvas, typeof(Canvas))]
[PseudoClasses(PcFloating, PcPadding)]
public class GridViewColumnHeader : Button
{
    /// <summary>Defines the <see cref="Column"/> property.</summary>
    public static readonly DirectProperty<GridViewColumnHeader, GridViewColumn?> ColumnProperty =
        AvaloniaProperty.RegisterDirect<GridViewColumnHeader, GridViewColumn?>(nameof(Column), o => o.Column);

    /// <summary>Defines the <see cref="Role"/> property.</summary>
    public static readonly DirectProperty<GridViewColumnHeader, GridViewColumnHeaderRole> RoleProperty =
        AvaloniaProperty.RegisterDirect<GridViewColumnHeader, GridViewColumnHeaderRole>(nameof(Role), o => o.Role);

    private const string PartHeaderGripper = "PART_HeaderGripper";
    private const string PartFloatingHeaderCanvas = "PART_FloatingHeaderCanvas";
    private const string PcFloating = ":floating";
    private const string PcPadding = ":padding";

    private GridViewColumn? _column;
    private GridViewColumnHeaderRole _role;
    private Thumb? _headerGripper;
    private Canvas? _floatingHeaderCanvas;
    private double _originalWidth;
    private double _gripperStartX;
    private bool _resizing;
    private bool _resizeCanceled;

    static GridViewColumnHeader()
    {
        FocusableProperty.OverrideDefaultValue<GridViewColumnHeader>(false);
    }

    /// <summary>Initializes a new instance of the <see cref="GridViewColumnHeader"/> class.</summary>
    public GridViewColumnHeader()
    {
        UpdatePseudoClasses();
    }

    /// <summary>The column of the header; null for the padding header.</summary>
    public GridViewColumn? Column
    {
        get => _column;
        internal set => SetAndRaise(ColumnProperty, ref _column, value);
    }

    /// <summary>The role of the header in the header row.</summary>
    public GridViewColumnHeaderRole Role
    {
        get => _role;
        internal set => SetAndRaise(RoleProperty, ref _role, value);
    }

    /// <summary>Whether the header row created the header (false when a column's header is itself a header).</summary>
    internal bool IsInternalGenerated { get; set; }

    /// <summary>Set while the header is dragged, so the release does not count as a click.</summary>
    internal bool SuppressClickEvent { get; set; }

    /// <summary>The header to the left, whose gripper reaches over this one.</summary>
    internal GridViewColumnHeader? PreviousVisualHeader { get; set; }

    /// <summary>For the floating header: the header it shows a copy of.</summary>
    internal GridViewColumnHeader? FloatSourceHeader { get; set; }

    /// <summary>Whether the gripper is being dragged.</summary>
    internal bool IsResizing => _resizing;

    private double ColumnActualWidth => Column?.ActualWidth ?? Bounds.Width;

    /// <summary>Hooks the gripper (normal role) or paints the floating copy (floating role).</summary>
    /// <param name="e">The event data.</param>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UnhookGripperEvents();
        _floatingHeaderCanvas = null;
        if (Role == GridViewColumnHeaderRole.Normal)
        {
            _headerGripper = e.NameScope.Find<Thumb>(PartHeaderGripper);
            if (_headerGripper is not null)
            {
                _headerGripper.DragStarted += OnGripperDragStarted;
                _headerGripper.DragDelta += OnGripperDragDelta;
                _headerGripper.DragCompleted += OnGripperDragCompleted;
                _headerGripper.AddHandler(PointerPressedEvent, OnGripperPressed, RoutingStrategies.Tunnel);
            }
        }
        else if (Role == GridViewColumnHeaderRole.Floating)
        {
            _floatingHeaderCanvas = e.NameScope.Find<Canvas>(PartFloatingHeaderCanvas);
            UpdateFloatingHeaderCanvas();
        }
    }

    /// <summary>Raises <see cref="Button.Click"/> unless the header is being dragged.</summary>
    protected override void OnClick()
    {
        if (!SuppressClickEvent)
        {
            base.OnClick();
            (Parent as GridViewHeaderRowPresenter)?.MakeParentItemsControlGotFocus();
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RoleProperty)
        {
            UpdatePseudoClasses();
        }
        else if (change.Property == BoundsProperty)
        {
            CheckWidthForPreviousHeaderGripper();
        }
    }

    /// <summary>Hides the right half of the previous header's gripper when this header is narrower than the gripper.</summary>
    internal void CheckWidthForPreviousHeaderGripper()
    {
        var hide = _headerGripper is not null && Bounds.Width < _headerGripper.Width;
        PreviousVisualHeader?.HideGripperRightHalf(hide);
    }

    /// <summary>Restores the width the column had when the gripper drag started (Escape during a resize).</summary>
    /// <returns>True if a resize was cancelled.</returns>
    internal bool CancelResize()
    {
        if (!_resizing || _resizeCanceled)
        {
            return false;
        }

        _resizeCanceled = true;
        UpdateColumnHeaderWidth(_originalWidth);
        return true;
    }

    /// <summary>Sets the width of the column, or of the header when it has no column.</summary>
    /// <param name="width">The width.</param>
    internal void UpdateColumnHeaderWidth(double width)
    {
        if (Column is not null)
        {
            Column.Width = width;
        }
        else
        {
            Width = width;
        }
    }

    /// <summary>Removes the copy painted for a drag.</summary>
    internal void ResetFloatingHeaderCanvasBackground()
    {
        if (_floatingHeaderCanvas is not null)
        {
            _floatingHeaderCanvas.Background = null;
        }
    }

    /// <summary>Paints the source header into the floating header's canvas.</summary>
    internal void UpdateFloatingHeaderCanvas()
    {
        if (_floatingHeaderCanvas is not null && FloatSourceHeader is { } source)
        {
            _floatingHeaderCanvas.Background = new VisualBrush(source)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
            };
            FloatSourceHeader = null;
        }
    }

    private void HideGripperRightHalf(bool hide)
    {
        if (_headerGripper?.GetVisualParent() is Visual container)
        {
            container.ClipToBounds = hide;
        }
    }

    private void UnhookGripperEvents()
    {
        if (_headerGripper is not null)
        {
            _headerGripper.DragStarted -= OnGripperDragStarted;
            _headerGripper.DragDelta -= OnGripperDragDelta;
            _headerGripper.DragCompleted -= OnGripperDragCompleted;
            _headerGripper.RemoveHandler(PointerPressedEvent, OnGripperPressed);
            _headerGripper = null;
        }
    }

    private void OnGripperDragStarted(object? sender, VectorEventArgs e)
    {
        (Parent as GridViewHeaderRowPresenter)?.MakeParentItemsControlGotFocus();
        _originalWidth = ColumnActualWidth;
        _gripperStartX = GripperX();
        _resizing = true;
        _resizeCanceled = false;
        e.Handled = true;
    }

    private void OnGripperDragDelta(object? sender, VectorEventArgs e)
    {
        if (!_resizing || _resizeCanceled)
        {
            return;
        }

        // The pointer moved by the gripper's own movement since the drag started plus the delta from the gripper.
        var width = _originalWidth + (GripperX() - _gripperStartX) + e.Vector.X;
        UpdateColumnHeaderWidth(Math.Max(0.0, width));
        e.Handled = true;
    }

    private void OnGripperDragCompleted(object? sender, VectorEventArgs e)
    {
        _resizing = false;
        _resizeCanceled = false;
        e.Handled = true;
    }

    private void OnGripperPressed(object? sender, PointerPressedEventArgs e)
    {
        // A double click on the gripper sizes the column to its content.
        if (e.ClickCount == 2 && Column is { } column)
        {
            AutoSizeColumn(column);
            e.Handled = true;
        }
    }

    /// <summary>Sizes a column to its content: sets the width and returns it to auto, so the column measures again.</summary>
    internal static void AutoSizeColumn(GridViewColumn column)
    {
        if (double.IsNaN(column.Width))
        {
            column.Width = column.ActualWidth;
        }

        column.Width = double.NaN;
    }

    private double GripperX() => _headerGripper?.TranslatePoint(default, this)?.X ?? 0.0;

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(PcFloating, Role == GridViewColumnHeaderRole.Floating);
        PseudoClasses.Set(PcPadding, Role == GridViewColumnHeaderRole.Padding);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new GridViewColumnHeaderAutomationPeer(this);
}
