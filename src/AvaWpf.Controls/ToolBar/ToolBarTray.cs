// Ported from WPF $W/PresentationFramework/System/Windows/Controls/ToolBarTray.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Avalonia.Automation.Peers;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Controls;

/// <summary>
/// Arranges <see cref="ToolBar"/>s in bands by <see cref="ToolBar.Band"/> and <see cref="ToolBar.BandIndex"/>; a gripper
/// drag moves a toolbar within or between bands.
/// </summary>
/// <remarks>Ctrl+Tab and Ctrl+Shift+Tab move the focus to the next and previous toolbar.</remarks>
public class ToolBarTray : Control
{
    /// <summary>Defines the <see cref="Background"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Panel.BackgroundProperty.AddOwner<ToolBarTray>();

    /// <summary>Defines the <see cref="Orientation"/> property.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ToolBarTray, Orientation>(nameof(Orientation));

    /// <summary>
    /// Defines the <see cref="IsLocked"/> property, also the inherited attached property <c>ToolBarTray.IsLocked</c>.
    /// </summary>
    public static readonly AttachedProperty<bool> IsLockedProperty =
        AvaloniaProperty.RegisterAttached<ToolBarTray, Control, bool>("IsLocked", inherits: true);

    private readonly List<BandInfo> _bands = new();
    private bool _bandsDirty = true;
    private Point _pointer;

    static ToolBarTray()
    {
        AffectsRender<ToolBarTray>(BackgroundProperty);
        AffectsMeasure<ToolBarTray>(OrientationProperty, IsLockedProperty);
        Thumb.DragDeltaEvent.AddClassHandler<ToolBarTray>((t, e) => t.OnThumbDragDelta(e));
    }

    /// <summary>Initializes a new instance of the <see cref="ToolBarTray"/> class.</summary>
    public ToolBarTray()
    {
        ToolBars.CollectionChanged += OnToolBarsChanged;

        // The pointer position, read before the gripper raises its drag event.
        AddHandler(PointerMovedEvent, (_, e) => _pointer = e.GetPosition(this), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerPressedEvent, (_, e) => _pointer = e.GetPosition(this), RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <summary>The brush drawn behind the bands.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>The orientation of the bands, and of every toolbar in the tray. Default horizontal.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Whether the toolbars are locked in place. Inherited by the toolbars.</summary>
    public bool IsLocked
    {
        get => GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    /// <summary>The toolbars of the tray.</summary>
    [Content]
    public AvaloniaList<ToolBar> ToolBars { get; } = new();

    /// <summary>Gets whether a control is locked (the inherited <c>ToolBarTray.IsLocked</c>).</summary>
    /// <param name="element">The control.</param>
    /// <returns>The value.</returns>
    public static bool GetIsLocked(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(IsLockedProperty);
    }

    /// <summary>Sets <c>ToolBarTray.IsLocked</c> on a control.</summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The value.</param>
    public static void SetIsLocked(Control element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IsLockedProperty, value);
    }

    /// <summary>Draws the background.</summary>
    /// <param name="context">The drawing context.</param>
    public override void Render(DrawingContext context)
    {
        if (Background is { } background)
        {
            context.FillRectangle(background, new Rect(Bounds.Size));
        }
    }

    /// <summary>
    /// Measures each band so a toolbar shrinks before the toolbars after it lose their minimum length.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        GenerateBands();
        var desired = default(Size);
        var horizontal = Orientation == Orientation.Horizontal;
        var childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);

        foreach (var info in _bands)
        {
            // The remaining length is the constraint minus the sum of all minimum lengths.
            var remainingLength = horizontal ? availableSize.Width : availableSize.Height;
            var band = info.Band;
            double bandThickness = 0;
            double bandLength = 0;
            foreach (var toolBar in band)
            {
                remainingLength -= toolBar.MinLength;
                if (remainingLength < 0)
                {
                    remainingLength = 0;
                    break;
                }
            }

            foreach (var toolBar in band)
            {
                remainingLength += toolBar.MinLength;
                childConstraint = horizontal ? childConstraint.WithWidth(remainingLength) : childConstraint.WithHeight(remainingLength);
                toolBar.Measure(childConstraint);
                var size = toolBar.DesiredSize;
                bandThickness = Math.Max(bandThickness, horizontal ? size.Height : size.Width);
                bandLength += horizontal ? size.Width : size.Height;
                remainingLength -= horizontal ? size.Width : size.Height;
                if (remainingLength < 0)
                {
                    remainingLength = 0;
                }
            }

            info.Thickness = bandThickness;
            desired = horizontal
                ? new Size(Math.Max(desired.Width, bandLength), desired.Height + bandThickness)
                : new Size(desired.Width + bandThickness, Math.Max(desired.Height, bandLength));
        }

        return desired;
    }

    /// <summary>Arranges the bands one after another and the toolbars of a band side by side.</summary>
    /// <param name="finalSize">The final size.</param>
    /// <returns>The size used.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        double bandOffset = 0;
        foreach (var info in _bands)
        {
            double offset = 0;
            foreach (var toolBar in info.Band)
            {
                var size = toolBar.DesiredSize;
                var rect = horizontal
                    ? new Rect(offset, bandOffset, size.Width, info.Thickness)
                    : new Rect(bandOffset, offset, info.Thickness, size.Height);
                toolBar.Arrange(rect);
                offset += horizontal ? size.Width : size.Height;
            }

            bandOffset += info.Thickness;
        }

        return finalSize;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OrientationProperty)
        {
            foreach (var toolBar in ToolBars)
            {
                toolBar.UpdateOrientation();
            }
        }
    }

    /// <summary>Moves the focus to the next toolbar on Ctrl+Tab and to the previous one on Ctrl+Shift+Tab.</summary>
    /// <param name="e">The event data.</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.Key != Key.Tab || !e.KeyModifiers.HasFlag(KeyModifiers.Control) || ToolBars.Count == 0)
        {
            return;
        }

        var ordered = OrderedToolBars();
        var current = (e.Source as Visual)?.FindAncestorOfType<ToolBar>(includeSelf: true);
        var index = current is null ? -1 : ordered.IndexOf(current);
        var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1;
        for (var n = 1; n <= ordered.Count; n++)
        {
            var next = ordered[((index + (step * n)) % ordered.Count + ordered.Count) % ordered.Count];
            if (next.ToolBarPanel is { } panel)
            {
                foreach (var item in ToolBar.FocusableItems(panel))
                {
                    if (item.Focus(NavigationMethod.Tab, e.KeyModifiers))
                    {
                        e.Handled = true;
                        return;
                    }
                }
            }
        }
    }

    /// <summary>The toolbars in band order, then band index order.</summary>
    internal List<ToolBar> OrderedToolBars()
    {
        GenerateBands();
        var list = new List<ToolBar>();
        foreach (var info in _bands)
        {
            list.AddRange(info.Band);
        }

        return list;
    }

    private void OnToolBarsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ToolBar toolBar in e.OldItems)
            {
                VisualChildren.Remove(toolBar);
                LogicalChildren.Remove(toolBar);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            VisualChildren.Clear();
            LogicalChildren.Clear();
        }

        if (e.NewItems is not null)
        {
            foreach (ToolBar toolBar in e.NewItems)
            {
                LogicalChildren.Add(toolBar);
                VisualChildren.Add(toolBar);
                toolBar.UpdateOrientation();
            }
        }

        _bandsDirty = true;
        InvalidateMeasure();
    }

    private void OnThumbDragDelta(VectorEventArgs e)
    {
        if (IsLocked || e.Source is not Thumb thumb || thumb.TemplatedParent is not ToolBar toolBar || toolBar.Parent != this)
        {
            return;
        }

        if (_bandsDirty)
        {
            GenerateBands();
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var currentBand = toolBar.Band;
        var pointInTray = _pointer;
        var pointInToolBar = TransformPointToToolBar(toolBar, pointInTray);
        var hitBand = GetBandFromOffset(horizontal ? pointInTray.Y : pointInTray.X);
        var thumbChange = horizontal ? e.Vector.X : e.Vector.Y;
        var toolBarPosition = horizontal ? pointInTray.X - pointInToolBar.X : pointInTray.Y - pointInToolBar.Y;
        var newPosition = toolBarPosition + thumbChange;

        if (hitBand == currentBand)
        {
            // Move within the band.
            var band = _bands[currentBand].Band;
            var toolBarIndex = toolBar.BandIndex;
            if (thumbChange < 0)
            {
                // Move left/up: shrink the toolbars before it while they can, else swap with the previous one.
                var totalMinimum = ToolBarsTotalMinimum(band, 0, toolBarIndex - 1);
                if (totalMinimum <= newPosition)
                {
                    ShrinkToolBars(band, 0, toolBarIndex - 1, -thumbChange);
                }
                else if (toolBarIndex > 0)
                {
                    var prev = band[toolBarIndex - 1];
                    var pointInPrev = TransformPointToToolBar(prev, pointInTray);
                    if ((horizontal ? pointInPrev.X : pointInPrev.Y) < 0)
                    {
                        // The pointer is before the previous toolbar: swap.
                        prev.BandIndex = toolBarIndex;
                        band[toolBarIndex] = prev;
                        toolBar.BandIndex = toolBarIndex - 1;
                        band[toolBarIndex - 1] = toolBar;
                        if (toolBarIndex + 1 == band.Count)
                        {
                            prev.ClearValue(horizontal ? WidthProperty : HeightProperty);
                        }
                    }
                    else if (totalMinimum < toolBarPosition)
                    {
                        ShrinkToolBars(band, 0, toolBarIndex - 1, toolBarPosition - totalMinimum);
                    }
                }
            }
            else
            {
                // Move right/down: expand the toolbars before it while they can, else swap with the next one.
                var totalMaximum = ToolBarsTotalMaximum(band, 0, toolBarIndex - 1);
                if (totalMaximum > newPosition)
                {
                    ExpandToolBars(band, 0, toolBarIndex - 1, thumbChange);
                }
                else if (toolBarIndex < band.Count - 1)
                {
                    var next = band[toolBarIndex + 1];
                    var pointInNext = TransformPointToToolBar(next, pointInTray);
                    if ((horizontal ? pointInNext.X : pointInNext.Y) >= 0)
                    {
                        // The pointer is over the next toolbar: swap.
                        next.BandIndex = toolBarIndex;
                        band[toolBarIndex] = next;
                        toolBar.BandIndex = toolBarIndex + 1;
                        band[toolBarIndex + 1] = toolBar;
                        if (toolBarIndex + 2 == band.Count)
                        {
                            toolBar.ClearValue(horizontal ? WidthProperty : HeightProperty);
                        }
                    }
                    else
                    {
                        ExpandToolBars(band, 0, toolBarIndex - 1, thumbChange);
                    }
                }
                else
                {
                    ExpandToolBars(band, 0, toolBarIndex - 1, thumbChange);
                }
            }

            InvalidateMeasure();
        }
        else
        {
            // Move to another band; -1 and the band count make a new band before or after the others.
            _bandsDirty = true;
            toolBar.Band = hitBand;
            toolBar.ClearValue(horizontal ? WidthProperty : HeightProperty);
            if (hitBand >= 0 && hitBand < _bands.Count)
            {
                MoveToolBar(toolBar, hitBand, newPosition);
            }

            // The old band's toolbars return to their natural lengths.
            foreach (var t in _bands[currentBand].Band)
            {
                t.ClearValue(horizontal ? WidthProperty : HeightProperty);
            }

            InvalidateMeasure();
        }

        e.Handled = true;
    }

    private Point TransformPointToToolBar(ToolBar toolBar, Point point) => this.TranslatePoint(point, toolBar) ?? point;

    private void ShrinkToolBars(List<ToolBar> band, int startIndex, int endIndex, double shrinkAmount)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        for (var i = endIndex; i >= startIndex; i--)
        {
            var toolBar = band[i];
            var length = horizontal ? toolBar.Bounds.Width : toolBar.Bounds.Height;
            if (length - shrinkAmount >= toolBar.MinLength)
            {
                SetLength(toolBar, length - shrinkAmount, horizontal);
                break;
            }

            SetLength(toolBar, toolBar.MinLength, horizontal);
            shrinkAmount -= length - toolBar.MinLength;
        }
    }

    private void ExpandToolBars(List<ToolBar> band, int startIndex, int endIndex, double expandAmount)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        for (var i = endIndex; i >= startIndex; i--)
        {
            var toolBar = band[i];
            var length = horizontal ? toolBar.Bounds.Width : toolBar.Bounds.Height;
            if (length + expandAmount <= toolBar.MaxLength)
            {
                SetLength(toolBar, length + expandAmount, horizontal);
                break;
            }

            SetLength(toolBar, toolBar.MaxLength, horizontal);
            expandAmount -= toolBar.MaxLength - length;
        }
    }

    private static void SetLength(ToolBar toolBar, double length, bool horizontal)
    {
        if (horizontal)
        {
            toolBar.Width = length;
        }
        else
        {
            toolBar.Height = length;
        }
    }

    private static double ToolBarsTotalMinimum(List<ToolBar> band, int startIndex, int endIndex)
    {
        double total = 0;
        for (var i = startIndex; i <= endIndex; i++)
        {
            total += band[i].MinLength;
        }

        return total;
    }

    private static double ToolBarsTotalMaximum(List<ToolBar> band, int startIndex, int endIndex)
    {
        double total = 0;
        for (var i = startIndex; i <= endIndex; i++)
        {
            total += band[i].MaxLength;
        }

        return total;
    }

    private void MoveToolBar(ToolBar toolBar, int newBandNumber, double position)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var newBand = _bands[newBandNumber].Band;

        // Find the new BandIndex from the lengths of the toolbars before the position.
        if (position <= 0)
        {
            toolBar.BandIndex = -1; // First place.
            return;
        }

        double toolBarOffset = 0;
        var newToolBarIndex = -1;
        int i;
        for (i = 0; i < newBand.Count; i++)
        {
            var current = newBand[i];
            if (newToolBarIndex == -1)
            {
                toolBarOffset += horizontal ? current.Bounds.Width : current.Bounds.Height; // The end of current.
                if (toolBarOffset > position)
                {
                    newToolBarIndex = i + 1;
                    toolBar.BandIndex = newToolBarIndex;

                    // The toolbar it lands on ends where the moved one starts.
                    var length = (horizontal ? current.Bounds.Width : current.Bounds.Height) - toolBarOffset + position;
                    SetLength(current, Math.Max(current.MinLength, length), horizontal);
                }
            }
            else
            {
                // Shift the indices after the inserted toolbar.
                current.BandIndex = i + 1;
            }
        }

        if (newToolBarIndex == -1)
        {
            toolBar.BandIndex = i;
        }
    }

    private int GetBandFromOffset(double toolBarOffset)
    {
        if (toolBarOffset < 0)
        {
            return -1;
        }

        double bandOffset = 0;
        for (var i = 0; i < _bands.Count; i++)
        {
            bandOffset += _bands[i].Thickness;
            if (bandOffset > toolBarOffset)
            {
                return i;
            }
        }

        return _bands.Count;
    }

    /// <summary>Groups the toolbars into bands and renumbers Band and BandIndex from 0.</summary>
    private void GenerateBands()
    {
        if (!IsBandsDirty())
        {
            return;
        }

        _bands.Clear();
        for (var i = 0; i < ToolBars.Count; i++)
        {
            InsertBand(ToolBars[i], i);
        }

        for (var bandIndex = 0; bandIndex < _bands.Count; bandIndex++)
        {
            var band = _bands[bandIndex].Band;
            for (var toolBarIndex = 0; toolBarIndex < band.Count; toolBarIndex++)
            {
                band[toolBarIndex].Band = bandIndex;
                band[toolBarIndex].BandIndex = toolBarIndex;
            }
        }

        _bandsDirty = false;
    }

    private bool IsBandsDirty()
    {
        if (_bandsDirty)
        {
            return true;
        }

        var total = 0;
        for (var bandIndex = 0; bandIndex < _bands.Count; bandIndex++)
        {
            var band = _bands[bandIndex].Band;
            for (var toolBarIndex = 0; toolBarIndex < band.Count; toolBarIndex++)
            {
                var toolBar = band[toolBarIndex];
                if (toolBar.Band != bandIndex || toolBar.BandIndex != toolBarIndex || !ToolBars.Contains(toolBar))
                {
                    return true;
                }
            }

            total += band.Count;
        }

        return total != ToolBars.Count;
    }

    private void InsertBand(ToolBar toolBar, int toolBarIndex)
    {
        var bandNumber = toolBar.Band;
        for (var i = 0; i < _bands.Count; i++)
        {
            var currentBandNumber = _bands[i].Band[0].Band;
            if (bandNumber == currentBandNumber)
            {
                return;
            }

            if (bandNumber < currentBandNumber)
            {
                _bands.Insert(i, CreateBand(toolBarIndex));
                return;
            }
        }

        _bands.Add(CreateBand(toolBarIndex));
    }

    private BandInfo CreateBand(int startIndex)
    {
        var info = new BandInfo();
        var toolBar = ToolBars[startIndex];
        info.Band.Add(toolBar);
        var bandNumber = toolBar.Band;
        for (var i = startIndex + 1; i < ToolBars.Count; i++)
        {
            toolBar = ToolBars[i];
            if (bandNumber == toolBar.Band)
            {
                InsertToolBar(toolBar, info.Band);
            }
        }

        return info;
    }

    private static void InsertToolBar(ToolBar toolBar, List<ToolBar> band)
    {
        for (var i = 0; i < band.Count; i++)
        {
            if (toolBar.BandIndex < band[i].BandIndex)
            {
                band.Insert(i, toolBar);
                return;
            }
        }

        band.Add(toolBar);
    }

    /// <summary>One band: its toolbars in BandIndex order and its thickness from the last measure.</summary>
    private sealed class BandInfo
    {
        public List<ToolBar> Band { get; } = new();

        public double Thickness { get; set; }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new ToolBarTrayAutomationPeer(this);
}
