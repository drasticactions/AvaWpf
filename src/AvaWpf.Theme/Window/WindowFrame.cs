using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaWpf.Automation.Peers;

namespace AvaWpf;

/// <summary>
/// The window frame of the theme family in effect. It has no window logic: it raises <see cref="CaptionButtonInvoked"/>,
/// <see cref="DragRequested"/> and <see cref="ResizeRequested"/> for <see cref="ThemeWindow"/> to handle.
/// </summary>
[TemplatePart(CaptionPartName, typeof(Control))]
[TemplatePart(MinimizePartName, typeof(Button))]
[TemplatePart(MaximizePartName, typeof(Button))]
[TemplatePart(ClosePartName, typeof(Button))]
[TemplatePart("PART_ContentPresenter", typeof(Avalonia.Controls.Presenters.ContentPresenter))]
[TemplatePart(CaptionContentPartName, typeof(Avalonia.Controls.Presenters.ContentPresenter))]
[TemplatePart(CaptionOverlayPartName, typeof(Avalonia.Controls.Presenters.ContentPresenter))]
[PseudoClasses(":active", ":inactive", ":maximized", ":minimized", ":tool", ":dialog", ":shadow", ":noicon", ":backdrop")]
public class WindowFrame : ContentControl
{
    /// <summary>The caption bar part; pressing it raises <see cref="DragRequested"/>.</summary>
    public const string CaptionPartName = "PART_Caption";

    /// <summary>The minimize button part.</summary>
    public const string MinimizePartName = "PART_MinimizeButton";

    /// <summary>The maximize/restore button part.</summary>
    public const string MaximizePartName = "PART_MaximizeButton";

    /// <summary>The close button part.</summary>
    public const string ClosePartName = "PART_CloseButton";

    /// <summary>The presenter of <see cref="CaptionContent"/>, in the caption after the icon.</summary>
    public const string CaptionContentPartName = "PART_CaptionContentPresenter";

    /// <summary>The presenter of <see cref="CaptionOverlay"/>, in the title area of the caption.</summary>
    public const string CaptionOverlayPartName = "PART_CaptionOverlayPresenter";

    /// <summary>The name of the template's title TextBlock, whose Foreground the caption slots take.</summary>
    private const string TitleElementName = "title";

    /// <summary>Defines the <see cref="Title"/> property.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<WindowFrame, string?>(nameof(Title));

    /// <summary>Defines the <see cref="Icon"/> property.</summary>
    public static readonly StyledProperty<IImage?> IconProperty =
        AvaloniaProperty.Register<WindowFrame, IImage?>(nameof(Icon));

    /// <summary>Defines the <see cref="IsActive"/> property.</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<WindowFrame, bool>(nameof(IsActive), true);

    /// <summary>Defines the <see cref="WindowState"/> property.</summary>
    public static readonly StyledProperty<WindowState> WindowStateProperty =
        AvaloniaProperty.Register<WindowFrame, WindowState>(nameof(WindowState));

    /// <summary>Defines the <see cref="Kind"/> property.</summary>
    public static readonly StyledProperty<WindowFrameKind> KindProperty =
        AvaloniaProperty.Register<WindowFrame, WindowFrameKind>(nameof(Kind));

    /// <summary>Defines the <see cref="CanMinimize"/> property.</summary>
    public static readonly StyledProperty<bool> CanMinimizeProperty =
        AvaloniaProperty.Register<WindowFrame, bool>(nameof(CanMinimize), true);

    /// <summary>Defines the <see cref="CanMaximize"/> property.</summary>
    public static readonly StyledProperty<bool> CanMaximizeProperty =
        AvaloniaProperty.Register<WindowFrame, bool>(nameof(CanMaximize), true);

    /// <summary>Defines the <see cref="CanResize"/> property.</summary>
    public static readonly StyledProperty<bool> CanResizeProperty =
        AvaloniaProperty.Register<WindowFrame, bool>(nameof(CanResize), true);

    /// <summary>Defines the <see cref="ShowIcon"/> property.</summary>
    public static readonly StyledProperty<bool> ShowIconProperty =
        AvaloniaProperty.Register<WindowFrame, bool>(nameof(ShowIcon), true);

    /// <summary>Defines the <see cref="MenuBar"/> property.</summary>
    public static readonly StyledProperty<object?> MenuBarProperty =
        AvaloniaProperty.Register<WindowFrame, object?>(nameof(MenuBar));

    /// <summary>Defines the <see cref="CaptionContent"/> property.</summary>
    public static readonly StyledProperty<object?> CaptionContentProperty =
        AvaloniaProperty.Register<WindowFrame, object?>(nameof(CaptionContent));

    /// <summary>Defines the <see cref="CaptionOverlay"/> property.</summary>
    public static readonly StyledProperty<object?> CaptionOverlayProperty =
        AvaloniaProperty.Register<WindowFrame, object?>(nameof(CaptionOverlay));

    /// <summary>Defines the <see cref="IsShadowVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsShadowVisibleProperty =
        AvaloniaProperty.Register<WindowFrame, bool>(nameof(IsShadowVisible));

    /// <summary>Defines the <see cref="IsBackdropVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsBackdropVisibleProperty =
        AvaloniaProperty.Register<WindowFrame, bool>(nameof(IsBackdropVisible));

    /// <summary>Defines the <see cref="IsMinimizeButtonVisible"/> property.</summary>
    public static readonly DirectProperty<WindowFrame, bool> IsMinimizeButtonVisibleProperty =
        AvaloniaProperty.RegisterDirect<WindowFrame, bool>(nameof(IsMinimizeButtonVisible), o => o.IsMinimizeButtonVisible);

    /// <summary>Defines the <see cref="IsMaximizeButtonVisible"/> property.</summary>
    public static readonly DirectProperty<WindowFrame, bool> IsMaximizeButtonVisibleProperty =
        AvaloniaProperty.RegisterDirect<WindowFrame, bool>(nameof(IsMaximizeButtonVisible), o => o.IsMaximizeButtonVisible);

    /// <summary>Defines the <see cref="CaptionButtonInvoked"/> event.</summary>
    public static readonly RoutedEvent<CaptionButtonInvokedEventArgs> CaptionButtonInvokedEvent =
        RoutedEvent.Register<WindowFrame, CaptionButtonInvokedEventArgs>(nameof(CaptionButtonInvoked), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="DragRequested"/> event.</summary>
    public static readonly RoutedEvent<WindowDragRequestedEventArgs> DragRequestedEvent =
        RoutedEvent.Register<WindowFrame, WindowDragRequestedEventArgs>(nameof(DragRequested), RoutingStrategies.Bubble);

    /// <summary>Defines the <see cref="ResizeRequested"/> event.</summary>
    public static readonly RoutedEvent<WindowResizeRequestedEventArgs> ResizeRequestedEvent =
        RoutedEvent.Register<WindowFrame, WindowResizeRequestedEventArgs>(nameof(ResizeRequested), RoutingStrategies.Bubble);

    /// <summary>Names the edge a template border part resizes; pressing the part raises <see cref="ResizeRequested"/>.</summary>
    public static readonly AttachedProperty<WindowEdge?> ResizeEdgeProperty =
        AvaloniaProperty.RegisterAttached<WindowFrame, Control, WindowEdge?>("ResizeEdge");

    private Control? _caption;
    private Control? _captionContent;
    private Control? _captionOverlay;
    private Button? _minimize;
    private Button? _maximize;
    private Button? _close;
    private bool _isMinimizeButtonVisible = true;
    private bool _isMaximizeButtonVisible = true;
    private bool _namesMinimize;
    private bool _namesMaximize;
    private bool _namesClose;

    static WindowFrame()
    {
        ResizeEdgeProperty.Changed.AddClassHandler<Control>(OnResizeEdgeChanged);
    }

    /// <summary>Initializes a new instance of the <see cref="WindowFrame"/> class.</summary>
    public WindowFrame()
    {
        UpdatePseudoClasses();
    }

    /// <summary>Raised when a caption button is clicked, or the caption double-clicked (Maximize or Restore).</summary>
    public event EventHandler<CaptionButtonInvokedEventArgs>? CaptionButtonInvoked
    {
        add => AddHandler(CaptionButtonInvokedEvent, value);
        remove => RemoveHandler(CaptionButtonInvokedEvent, value);
    }

    /// <summary>Raised when the caption is pressed with the left button.</summary>
    public event EventHandler<WindowDragRequestedEventArgs>? DragRequested
    {
        add => AddHandler(DragRequestedEvent, value);
        remove => RemoveHandler(DragRequestedEvent, value);
    }

    /// <summary>Raised when a resize border is pressed with the left button.</summary>
    public event EventHandler<WindowResizeRequestedEventArgs>? ResizeRequested
    {
        add => AddHandler(ResizeRequestedEvent, value);
        remove => RemoveHandler(ResizeRequestedEvent, value);
    }

    /// <summary>The caption text.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The caption icon.</summary>
    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Whether the frame draws the active caption. Default true.</summary>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>The window state the frame draws.</summary>
    public WindowState WindowState
    {
        get => GetValue(WindowStateProperty);
        set => SetValue(WindowStateProperty, value);
    }

    /// <summary>Normal, Tool (small caption) or Dialog (no minimize or maximize).</summary>
    public WindowFrameKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>Whether the minimize button shows (except for dialogs).</summary>
    public bool CanMinimize
    {
        get => GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }

    /// <summary>Whether the maximize button shows (except for dialogs).</summary>
    public bool CanMaximize
    {
        get => GetValue(CanMaximizeProperty);
        set => SetValue(CanMaximizeProperty, value);
    }

    /// <summary>Whether the borders resize the window.</summary>
    public bool CanResize
    {
        get => GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    /// <summary>Whether the caption shows the icon.</summary>
    public bool ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    /// <summary>Optional content of the menu-bar slot under the caption.</summary>
    public object? MenuBar
    {
        get => GetValue(MenuBarProperty);
        set => SetValue(MenuBarProperty, value);
    }

    /// <summary>
    /// Optional content in the caption between the icon and the title. Its controls do not drag the window; its empty
    /// areas do.
    /// </summary>
    public object? CaptionContent
    {
        get => GetValue(CaptionContentProperty);
        set => SetValue(CaptionContentProperty, value);
    }

    /// <summary>
    /// Optional content laid over the left of the caption's title area; the title starts after it. Its empty areas drag
    /// the window.
    /// </summary>
    public object? CaptionOverlay
    {
        get => GetValue(CaptionOverlayProperty);
        set => SetValue(CaptionOverlayProperty, value);
    }

    /// <summary>Whether the frame draws its shadow, which needs a transparent window.</summary>
    public bool IsShadowVisible
    {
        get => GetValue(IsShadowVisibleProperty);
        set => SetValue(IsShadowVisibleProperty, value);
    }

    /// <summary>Whether the platform draws a backdrop material (Mica) behind the window.</summary>
    public bool IsBackdropVisible
    {
        get => GetValue(IsBackdropVisibleProperty);
        set => SetValue(IsBackdropVisibleProperty, value);
    }

    /// <summary>True when the minimize button shows: <see cref="CanMinimize"/> and not a dialog.</summary>
    public bool IsMinimizeButtonVisible
    {
        get => _isMinimizeButtonVisible;
        private set => SetAndRaise(IsMinimizeButtonVisibleProperty, ref _isMinimizeButtonVisible, value);
    }

    /// <summary>True when the maximize button shows: <see cref="CanMaximize"/> and not a dialog.</summary>
    public bool IsMaximizeButtonVisible
    {
        get => _isMaximizeButtonVisible;
        private set => SetAndRaise(IsMaximizeButtonVisibleProperty, ref _isMaximizeButtonVisible, value);
    }

    /// <summary>Gets the resize edge of a template part.</summary>
    public static WindowEdge? GetResizeEdge(Control control) => control.GetValue(ResizeEdgeProperty);

    /// <summary>Sets the resize edge of a template part.</summary>
    public static void SetResizeEdge(Control control, WindowEdge? value) => control.SetValue(ResizeEdgeProperty, value);

    /// <summary>The caption bar part of the template, or null.</summary>
    internal Control? CaptionPart => _caption;

    /// <summary>The caption buttons of the template that are shown, in order: minimize, maximize or restore, close.</summary>
    internal IEnumerable<Button> VisibleCaptionButtons
    {
        get
        {
            foreach (var button in new[] { _minimize, _maximize, _close })
            {
                if (button is { IsVisible: true })
                {
                    yield return button;
                }
            }
        }
    }

    /// <summary>Raises <see cref="CaptionButtonInvoked"/>.</summary>
    public void InvokeCaptionButton(CaptionButton button) =>
        RaiseEvent(new CaptionButtonInvokedEventArgs(CaptionButtonInvokedEvent, button));

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_caption is not null)
        {
            _caption.PointerPressed -= OnCaptionPressed;
            _caption.DoubleTapped -= OnCaptionDoubleTapped;
        }

        if (_minimize is not null) { _minimize.Click -= OnMinimizeClick; }
        if (_maximize is not null) { _maximize.Click -= OnMaximizeClick; }
        if (_close is not null) { _close.Click -= OnCloseClick; }

        _caption = e.NameScope.Find<Control>(CaptionPartName);
        _captionContent = e.NameScope.Find<Control>(CaptionContentPartName);
        _captionOverlay = e.NameScope.Find<Control>(CaptionOverlayPartName);

        // The caption slots follow the title's Foreground, which the family styles change when inactive.
        if (e.NameScope.Find<TextBlock>(TitleElementName) is { } title)
        {
            foreach (var slot in new[] { _captionContent, _captionOverlay })
            {
                if (slot is ContentPresenter presenter)
                {
                    presenter.Bind(ContentPresenter.ForegroundProperty, title.GetObservable(TextBlock.ForegroundProperty));
                }
            }
        }
        _minimize = e.NameScope.Find<Button>(MinimizePartName);
        _maximize = e.NameScope.Find<Button>(MaximizePartName);
        _close = e.NameScope.Find<Button>(ClosePartName);

        if (_caption is not null)
        {
            _caption.PointerPressed += OnCaptionPressed;
            _caption.DoubleTapped += OnCaptionDoubleTapped;
        }

        if (_minimize is not null) { _minimize.Click += OnMinimizeClick; }
        if (_maximize is not null) { _maximize.Click += OnMaximizeClick; }
        if (_close is not null) { _close.Click += OnCloseClick; }

        // Name the glyph-only caption buttons for screen readers, unless the template names them.
        _namesMinimize = _minimize is not null && !_minimize.IsSet(AutomationProperties.NameProperty);
        _namesMaximize = _maximize is not null && !_maximize.IsSet(AutomationProperties.NameProperty);
        _namesClose = _close is not null && !_close.IsSet(AutomationProperties.NameProperty);
        UpdateCaptionButtonNames();
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new WindowFrameAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsActiveProperty || change.Property == WindowStateProperty || change.Property == KindProperty ||
            change.Property == IsShadowVisibleProperty || change.Property == ShowIconProperty || change.Property == IconProperty ||
            change.Property == CanMinimizeProperty || change.Property == CanMaximizeProperty || change.Property == IsBackdropVisibleProperty)
        {
            UpdatePseudoClasses();
        }

        if (change.Property == WindowStateProperty)
        {
            UpdateCaptionButtonNames();
        }
    }

    private void UpdateCaptionButtonNames()
    {
        if (_namesMinimize)
        {
            AutomationProperties.SetName(_minimize!, "Minimize");
        }

        if (_namesMaximize)
        {
            AutomationProperties.SetName(_maximize!, WindowState == WindowState.Maximized ? "Restore" : "Maximize");
        }

        if (_namesClose)
        {
            AutomationProperties.SetName(_close!, "Close");
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":active", IsActive);
        PseudoClasses.Set(":inactive", !IsActive);
        PseudoClasses.Set(":maximized", WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen);
        PseudoClasses.Set(":minimized", WindowState == WindowState.Minimized);
        PseudoClasses.Set(":tool", Kind == WindowFrameKind.Tool);
        PseudoClasses.Set(":dialog", Kind == WindowFrameKind.Dialog);
        PseudoClasses.Set(":shadow", IsShadowVisible && WindowState == WindowState.Normal);
        PseudoClasses.Set(":noicon", !ShowIcon || Icon is null);
        PseudoClasses.Set(":backdrop", IsBackdropVisible);
        IsMinimizeButtonVisible = CanMinimize && Kind != WindowFrameKind.Dialog;
        IsMaximizeButtonVisible = CanMaximize && Kind != WindowFrameKind.Dialog;
    }

    private void OnCaptionPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.ClickCount == 1)
        {
            RaiseEvent(new WindowDragRequestedEventArgs(DragRequestedEvent, e));
        }
    }

    private void OnCaptionDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (IsMaximizeButtonVisible && !IsInCaptionSlot(e.Source as Visual))
        {
            InvokeCaptionButton(WindowState == WindowState.Maximized ? CaptionButton.Restore : CaptionButton.Maximize);
        }
    }

    /// <summary>True when <paramref name="source"/> is a control inside <see cref="CaptionContent"/> or <see cref="CaptionOverlay"/>.</summary>
    private bool IsInCaptionSlot(Visual? source)
    {
        for (var v = source; v is not null && v != _caption; v = v.GetVisualParent())
        {
            if (v.GetVisualParent() is { } parent && (parent == _captionContent || parent == _captionOverlay))
            {
                return true;
            }
        }

        return false;
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) => InvokeCaptionButton(CaptionButton.Minimize);

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) =>
        InvokeCaptionButton(WindowState == WindowState.Maximized ? CaptionButton.Restore : CaptionButton.Maximize);

    private void OnCloseClick(object? sender, RoutedEventArgs e) => InvokeCaptionButton(CaptionButton.Close);

    private static void OnResizeEdgeChanged(Control part, AvaloniaPropertyChangedEventArgs e)
    {
        part.PointerPressed -= OnResizePartPressed;

        // On macOS the system resizes ThemeWindow from the window's own edges; the parts stay inert.
        if (e.NewValue is WindowEdge edge && !OperatingSystem.IsMacOS())
        {
            part.PointerPressed += OnResizePartPressed;
            part.Cursor = new Cursor(edge switch
            {
                WindowEdge.North => StandardCursorType.TopSide,
                WindowEdge.South => StandardCursorType.BottomSide,
                WindowEdge.West => StandardCursorType.LeftSide,
                WindowEdge.East => StandardCursorType.RightSide,
                WindowEdge.NorthWest => StandardCursorType.TopLeftCorner,
                WindowEdge.NorthEast => StandardCursorType.TopRightCorner,
                WindowEdge.SouthWest => StandardCursorType.BottomLeftCorner,
                _ => StandardCursorType.BottomRightCorner,
            });
        }
    }

    private static void OnResizePartPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control part || GetResizeEdge(part) is not { } edge || !e.GetCurrentPoint(part).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (part.TemplatedParent is WindowFrame { CanResize: true, WindowState: WindowState.Normal } frame)
        {
            frame.RaiseEvent(new WindowResizeRequestedEventArgs(ResizeRequestedEvent, edge, e));
            e.Handled = true;
        }
    }
}
