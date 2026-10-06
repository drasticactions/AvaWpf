using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaWpf.Automation.Peers;

namespace AvaWpf;

/// <summary>
/// A framed window drawn inside a <see cref="TopLevel"/>, for platforms that cannot open windows (the browser). The
/// family's <see cref="WindowFrame"/> draws it exactly as it draws a <see cref="ThemeWindow"/>; the window moves by
/// its caption and resizes by its borders within its <see cref="InPageWindowLayer"/>.
/// </summary>
/// <remarks>
/// Show and close windows through <see cref="WindowHost"/>, or add one to a layer with
/// <see cref="InPageWindowLayer.Show"/>. A modal window blocks the page beneath it and takes Enter, Esc and Tab
/// for itself.
/// </remarks>
[TemplatePart(FramePartName, typeof(WindowFrame))]
public class InPageWindow : ContentControl
{
    /// <summary>The frame part.</summary>
    public const string FramePartName = "PART_Frame";

    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<InPageWindow, string?>(nameof(Title));

    /// <summary>Defines the <see cref="FrameIcon"/> property.</summary>
    public static readonly StyledProperty<IImage?> FrameIconProperty =
        AvaloniaProperty.Register<InPageWindow, IImage?>(nameof(FrameIcon));

    /// <summary>Defines the <see cref="FrameKind"/> property.</summary>
    public static readonly StyledProperty<WindowFrameKind> FrameKindProperty =
        AvaloniaProperty.Register<InPageWindow, WindowFrameKind>(nameof(FrameKind), WindowFrameKind.Dialog);

    /// <summary>Defines the <see cref="ShowFrameTitle"/> property.</summary>
    public static readonly StyledProperty<bool> ShowFrameTitleProperty =
        AvaloniaProperty.Register<InPageWindow, bool>(nameof(ShowFrameTitle), true);

    /// <summary>Defines the <see cref="ShowFrameIcon"/> property.</summary>
    public static readonly StyledProperty<bool> ShowFrameIconProperty =
        AvaloniaProperty.Register<InPageWindow, bool>(nameof(ShowFrameIcon), true);

    /// <summary>Defines the <see cref="CanResize"/> property.</summary>
    public static readonly StyledProperty<bool> CanResizeProperty =
        AvaloniaProperty.Register<InPageWindow, bool>(nameof(CanResize));

    /// <summary>Defines the <see cref="SizeToContent"/> property.</summary>
    public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
        AvaloniaProperty.Register<InPageWindow, SizeToContent>(nameof(SizeToContent));

    /// <summary>Defines the <see cref="WindowStartupLocation"/> property.</summary>
    public static readonly StyledProperty<WindowStartupLocation> WindowStartupLocationProperty =
        AvaloniaProperty.Register<InPageWindow, WindowStartupLocation>(nameof(WindowStartupLocation), WindowStartupLocation.CenterOwner);

    /// <summary>Defines the <see cref="IsActive"/> property.</summary>
    public static readonly DirectProperty<InPageWindow, bool> IsActiveProperty =
        AvaloniaProperty.RegisterDirect<InPageWindow, bool>(nameof(IsActive), o => o.IsActive);

    /// <summary>Defines the <see cref="IsModal"/> property.</summary>
    public static readonly DirectProperty<InPageWindow, bool> IsModalProperty =
        AvaloniaProperty.RegisterDirect<InPageWindow, bool>(nameof(IsModal), o => o.IsModal);

    /// <summary>Defines the <see cref="Position"/> property.</summary>
    public static readonly StyledProperty<Point> PositionProperty =
        AvaloniaProperty.Register<InPageWindow, Point>(nameof(Position));

    private readonly TaskCompletionSource<object?> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private WindowFrame? _frame;
    private bool _isActive;
    private bool _isModal;
    private bool _closing;
    private IInputElement? _focusBeforeOpen;
    private IInputElement? _focusInside;
    private Point _dragStart;
    private Point _dragOrigin;
    private Size _dragSize;
    private WindowEdge? _dragEdge;
    private bool _dragging;

    static InPageWindow()
    {
        KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<InPageWindow>(KeyboardNavigationMode.Cycle);
        FocusableProperty.OverrideDefaultValue<InPageWindow>(false);
    }

    /// <summary>Raised when <see cref="Close"/> is called; set <see cref="WindowHostClosingEventArgs.Cancel"/> to keep the window open.</summary>
    public event EventHandler<WindowHostClosingEventArgs>? Closing;

    /// <summary>Raised after the window left its layer.</summary>
    public event EventHandler? Closed;

    /// <summary>Raised after the window was added to its layer and laid out.</summary>
    public event EventHandler? Opened;

    /// <summary>The caption text.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The caption icon.</summary>
    public IImage? FrameIcon
    {
        get => GetValue(FrameIconProperty);
        set => SetValue(FrameIconProperty, value);
    }

    /// <summary>Normal, Tool or Dialog (the default: no minimize or maximize).</summary>
    public WindowFrameKind FrameKind
    {
        get => GetValue(FrameKindProperty);
        set => SetValue(FrameKindProperty, value);
    }

    /// <summary>Whether the caption shows <see cref="Title"/>.</summary>
    public bool ShowFrameTitle
    {
        get => GetValue(ShowFrameTitleProperty);
        set => SetValue(ShowFrameTitleProperty, value);
    }

    /// <summary>Whether the caption shows <see cref="FrameIcon"/>.</summary>
    public bool ShowFrameIcon
    {
        get => GetValue(ShowFrameIconProperty);
        set => SetValue(ShowFrameIconProperty, value);
    }

    /// <summary>Whether the borders resize the window.</summary>
    public bool CanResize
    {
        get => GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    /// <summary>How the window sizes to its content; a resize by the user ends it, as on a desktop window.</summary>
    public SizeToContent SizeToContent
    {
        get => GetValue(SizeToContentProperty);
        set => SetValue(SizeToContentProperty, value);
    }

    /// <summary>Where the window opens: centered on the layer, or where <see cref="Position"/> says (Manual).</summary>
    public WindowStartupLocation WindowStartupLocation
    {
        get => GetValue(WindowStartupLocationProperty);
        set => SetValue(WindowStartupLocationProperty, value);
    }

    /// <summary>True for the window that has the keyboard: the topmost one the user last used.</summary>
    public bool IsActive
    {
        get => _isActive;
        internal set => SetAndRaise(IsActiveProperty, ref _isActive, value);
    }

    /// <summary>True when the window blocks the page beneath it.</summary>
    public bool IsModal
    {
        get => _isModal;
        internal set => SetAndRaise(IsModalProperty, ref _isModal, value);
    }

    /// <summary>The top-left corner in the layer.</summary>
    public Point Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <summary>The frame part, once the template is applied.</summary>
    public WindowFrame? Frame => _frame;

    /// <summary>The layer showing the window, while it is open.</summary>
    public InPageWindowLayer? Layer { get; internal set; }

    /// <summary>The value passed to <see cref="Close"/>; completes when the window closed.</summary>
    public Task<object?> Result => _result.Task;

    /// <summary>True once the window has closed.</summary>
    public bool IsClosed => _result.Task.IsCompleted;

    /// <summary>Closes the window with <paramref name="result"/>, unless a <see cref="Closing"/> handler cancels.</summary>
    public void Close(object? result = null)
    {
        if (IsClosed || _closing)
        {
            return;
        }

        var e = new WindowHostClosingEventArgs();
        _closing = true;
        try
        {
            Closing?.Invoke(this, e);
            if (Content is Control content)
            {
                content.RaiseEvent(new WindowHostClosingEventArgs(WindowHost.ClosingEvent, e));
            }
        }
        finally
        {
            _closing = false;
        }

        if (e.Cancel)
        {
            return;
        }

        Layer?.Remove(this);
        _result.TrySetResult(result);
        RestoreFocus();
        Closed?.Invoke(this, EventArgs.Empty);
        if (Content is Control c)
        {
            c.RaiseEvent(new RoutedEventArgs(WindowHost.ClosedEvent));
        }
    }

    /// <summary>Brings the window to the front of its layer and gives it the keyboard.</summary>
    public void Activate() => Layer?.Activate(this);

    /// <summary>Blinks the frame, as Windows does when a click lands beside a modal window.</summary>
    public async void Flash()
    {
        if (_frame is null)
        {
            return;
        }

        for (var i = 0; i < 6; i++)
        {
            _frame.IsActive = !_frame.IsActive;
            await Task.Delay(70);
        }

        _frame.IsActive = IsActive;
    }

    internal void OnShown(IInputElement? focusBefore)
    {
        _focusBeforeOpen = focusBefore;
        Dispatcher.UIThread.Post(() =>
        {
            if (IsClosed)
            {
                return;
            }

            FocusFirst();
            Opened?.Invoke(this, EventArgs.Empty);
            if (Content is Control content)
            {
                content.RaiseEvent(new RoutedEventArgs(WindowHost.OpenedEvent));
            }
        }, DispatcherPriority.Loaded);
    }

    /// <summary>Moves the keyboard into the window: back where it was, else to the first control that takes focus.</summary>
    internal void FocusFirst()
    {
        if (_focusInside is Visual v && v.IsAttachedToVisualTree() && this.IsVisualAncestorOf(v) && _focusInside.Focus())
        {
            return;
        }

        var first = this.GetVisualDescendants().OfType<InputElement>().FirstOrDefault(e =>
            e.Focusable && e.IsEffectivelyEnabled && e.IsEffectivelyVisible && KeyboardNavigation.GetIsTabStop(e));
        if (first is not null)
        {
            first.Focus(NavigationMethod.Tab);
        }
        else
        {
            Focus();
        }
    }

    /// <summary>True when <paramref name="element"/> is in the window or in a popup opened from it.</summary>
    public bool Contains(object? element) =>
        element is ILogical logical && (logical == this || logical.GetLogicalAncestors().Contains(this) ||
            (element is Visual v && this.IsVisualAncestorOf(v)));

    /// <summary>Remembers the focused element inside the window, for <see cref="FocusFirst"/> after another window.</summary>
    internal void RememberFocus(IInputElement? focused)
    {
        if (focused is Visual v && this.IsVisualAncestorOf(v))
        {
            _focusInside = focused;
        }
    }

    private void RestoreFocus()
    {
        if (Layer?.ActiveWindow is { } next)
        {
            next.FocusFirst();
            return;
        }

        if (_focusBeforeOpen is Visual { } v && v.IsAttachedToVisualTree())
        {
            _focusBeforeOpen.Focus();
        }
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(InPageWindow);

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new InPageWindowAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_frame is not null)
        {
            _frame.CaptionButtonInvoked -= OnCaptionButton;
            _frame.DragRequested -= OnDragRequested;
            _frame.ResizeRequested -= OnResizeRequested;
        }

        _frame = e.NameScope.Find<WindowFrame>(FramePartName);
        if (_frame is not null)
        {
            _frame.CaptionButtonInvoked += OnCaptionButton;
            _frame.DragRequested += OnDragRequested;
            _frame.ResizeRequested += OnResizeRequested;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Layer?.ActiveWindow != this)
        {
            Activate();
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled)
        {
            e.Handled = HandleDialogKey(e);
        }
    }

    /// <summary>Enter clicks the default button, Esc the cancel button (or closes the window), Alt+key an access key.</summary>
    internal bool HandleDialogKey(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter when e.KeyModifiers == KeyModifiers.None:
                // As in a desktop dialog, a focused button takes Enter itself; otherwise the default button does.
                return Invoke(FocusedButton() ?? FindButton(b => b.IsDefault));
            case Key.Escape when e.KeyModifiers == KeyModifiers.None:
                if (FindButton(b => b.IsCancel) is { } cancel)
                {
                    return Invoke(cancel);
                }

                if (FrameKind == WindowFrameKind.Dialog)
                {
                    Close();
                    return true;
                }

                return false;
            default:
                return e.KeyModifiers == KeyModifiers.Alt && AccessKeys.Invoke(this, e.Key);
        }
    }

    private Button? FocusedButton() =>
        TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is Button { IsEffectivelyEnabled: true } b && Contains(b) ? b : null;

    private Button? FindButton(Func<Button, bool> predicate) =>
        this.GetVisualDescendants().OfType<Button>().FirstOrDefault(b => predicate(b) && b.IsEffectivelyVisible && b.IsEffectivelyEnabled);

    private static bool Invoke(Button? button)
    {
        if (button is null)
        {
            return false;
        }

        if (ControlAutomationPeer.CreatePeerForElement(button).GetProvider<Avalonia.Automation.Provider.IInvokeProvider>() is { } invoke)
        {
            invoke.Invoke();
            return true;
        }

        return false;
    }

    private void OnCaptionButton(object? sender, CaptionButtonInvokedEventArgs e)
    {
        if (e.Button == CaptionButton.Close)
        {
            Close();
        }

        e.Handled = true;
    }

    private void OnDragRequested(object? sender, WindowDragRequestedEventArgs e) => BeginDrag(e.Pointer, null);

    private void OnResizeRequested(object? sender, WindowResizeRequestedEventArgs e)
    {
        if (CanResize)
        {
            BeginDrag(e.Pointer, e.Edge);
        }
    }

    private void BeginDrag(PointerPressedEventArgs pointer, WindowEdge? edge)
    {
        if (Layer is null)
        {
            return;
        }

        Activate();
        _dragging = true;
        _dragEdge = edge;
        _dragStart = pointer.GetPosition(Layer);
        _dragOrigin = Position;
        _dragSize = Bounds.Size;
        pointer.Pointer.Capture(this);
        pointer.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging || Layer is null)
        {
            return;
        }

        var delta = e.GetPosition(Layer) - _dragStart;
        if (_dragEdge is not { } edge)
        {
            Position = Layer.Clamp(this, _dragOrigin + delta);
            return;
        }

        SizeToContent = SizeToContent.Manual;
        double x = _dragOrigin.X, y = _dragOrigin.Y, w = _dragSize.Width, h = _dragSize.Height;
        double minW = Math.Max(MinWidth, 80), minH = Math.Max(MinHeight, 40);
        if (edge is WindowEdge.West or WindowEdge.NorthWest or WindowEdge.SouthWest)
        {
            var nw = Math.Clamp(w - delta.X, minW, MaxWidth);
            x += w - nw;
            w = nw;
        }
        else if (edge is WindowEdge.East or WindowEdge.NorthEast or WindowEdge.SouthEast)
        {
            w = Math.Clamp(w + delta.X, minW, MaxWidth);
        }

        if (edge is WindowEdge.North or WindowEdge.NorthWest or WindowEdge.NorthEast)
        {
            var nh = Math.Clamp(h - delta.Y, minH, MaxHeight);
            y += h - nh;
            h = nh;
        }
        else if (edge is WindowEdge.South or WindowEdge.SouthWest or WindowEdge.SouthEast)
        {
            h = Math.Clamp(h + delta.Y, minH, MaxHeight);
        }

        Width = w;
        Height = h;
        Position = new Point(x, y);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        EndDrag(e.Pointer);
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragging = false;
    }

    private void EndDrag(IPointer pointer)
    {
        if (_dragging)
        {
            _dragging = false;
            pointer.Capture(null);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeToContentProperty)
        {
            var mode = change.GetNewValue<SizeToContent>();
            if (mode is SizeToContent.Width or SizeToContent.WidthAndHeight)
            {
                ClearValue(WidthProperty);
            }

            if (mode is SizeToContent.Height or SizeToContent.WidthAndHeight)
            {
                ClearValue(HeightProperty);
            }
        }
        else if (change.Property == PositionProperty)
        {
            Layer?.InvalidateArrange();
        }
    }
}

/// <summary>Arguments of a window's closing: set <see cref="Cancel"/> to keep it open.</summary>
public sealed class WindowHostClosingEventArgs : RoutedEventArgs
{
    private readonly WindowHostClosingEventArgs? _shared;
    private bool _cancel;

    /// <summary>Initializes arguments raised as a plain event.</summary>
    public WindowHostClosingEventArgs()
    {
    }

    /// <summary>Initializes arguments raised on the content as <paramref name="routedEvent"/>, sharing <paramref name="shared"/>'s Cancel.</summary>
    public WindowHostClosingEventArgs(RoutedEvent routedEvent, WindowHostClosingEventArgs shared)
        : base(routedEvent)
    {
        _shared = shared;
    }

    /// <summary>True keeps the window open.</summary>
    public bool Cancel
    {
        get => _shared?.Cancel ?? _cancel;
        set
        {
            if (_shared is not null)
            {
                _shared.Cancel = value;
            }
            else
            {
                _cancel = value;
            }
        }
    }
}
