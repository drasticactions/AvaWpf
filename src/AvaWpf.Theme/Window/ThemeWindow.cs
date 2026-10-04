using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaWpf.Animations;
using Avalonia.Automation.Peers;
using AvaWpf.Automation.Peers;

namespace AvaWpf;

/// <summary>
/// A window without system decorations that draws its frame in the theme family in effect, through a
/// <see cref="WindowFrame"/> whose requests it turns into window operations.
/// </summary>
/// <remarks>
/// The shadow and the open and close motion need a transparent window; otherwise the frame is opaque and still.
/// </remarks>
[TemplatePart(FramePartName, typeof(WindowFrame))]
public class ThemeWindow : Window
{
    /// <summary>The frame part.</summary>
    public const string FramePartName = "PART_Frame";

    /// <summary>The resource key that names the <see cref="WindowMotion"/> of the family in effect.</summary>
    public const string WindowMotionKey = "AvaWpf.WindowMotion";

    /// <summary>Defines the <see cref="FrameKind"/> property.</summary>
    public static readonly StyledProperty<WindowFrameKind> FrameKindProperty =
        AvaloniaProperty.Register<ThemeWindow, WindowFrameKind>(nameof(FrameKind));

    /// <summary>Defines the <see cref="MenuBar"/> property.</summary>
    public static readonly StyledProperty<object?> MenuBarProperty =
        AvaloniaProperty.Register<ThemeWindow, object?>(nameof(MenuBar));

    /// <summary>Defines the <see cref="FrameIcon"/> property.</summary>
    public static readonly StyledProperty<IImage?> FrameIconProperty =
        AvaloniaProperty.Register<ThemeWindow, IImage?>(nameof(FrameIcon));

    /// <summary>Defines the <see cref="ShowFrameIcon"/> property.</summary>
    public static readonly StyledProperty<bool> ShowFrameIconProperty =
        AvaloniaProperty.Register<ThemeWindow, bool>(nameof(ShowFrameIcon), true);

    /// <summary>Defines the <see cref="IsBackdropVisible"/> property.</summary>
    public static readonly DirectProperty<ThemeWindow, bool> IsBackdropVisibleProperty =
        AvaloniaProperty.RegisterDirect<ThemeWindow, bool>(nameof(IsBackdropVisible), o => o.IsBackdropVisible);

    /// <summary>Defines the <see cref="CaptionContent"/> property.</summary>
    public static readonly StyledProperty<object?> CaptionContentProperty =
        WindowFrame.CaptionContentProperty.AddOwner<ThemeWindow>();

    /// <summary>Defines the <see cref="CaptionOverlay"/> property.</summary>
    public static readonly StyledProperty<object?> CaptionOverlayProperty =
        WindowFrame.CaptionOverlayProperty.AddOwner<ThemeWindow>();

    /// <summary>Defines the <see cref="IsShadowVisible"/> property.</summary>
    public static readonly DirectProperty<ThemeWindow, bool> IsShadowVisibleProperty =
        AvaloniaProperty.RegisterDirect<ThemeWindow, bool>(nameof(IsShadowVisible), o => o.IsShadowVisible);

    private WindowFrame? _frame;
    private bool _closeAnimated;
    private bool _minimizing;
    private bool _isShadowVisible;
    private bool _isBackdropVisible;
    private WindowState _lastState;

    /// <summary>Initializes a new instance of the <see cref="ThemeWindow"/> class.</summary>
    public ThemeWindow()
    {
        WindowDecorations = WindowDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None];
        Background = Brushes.Transparent;
        ResourcesChanged += (_, _) => UpdateTransparencyHint();
        if (OperatingSystem.IsMacOS())
        {
            Activated += (_, _) => EnableNativeResize();
        }
    }

    /// <summary>Normal, Tool (small caption) or Dialog (no minimize or maximize).</summary>
    public WindowFrameKind FrameKind
    {
        get => GetValue(FrameKindProperty);
        set => SetValue(FrameKindProperty, value);
    }

    /// <summary>Optional content of the frame's menu-bar slot, under the caption.</summary>
    public object? MenuBar
    {
        get => GetValue(MenuBarProperty);
        set => SetValue(MenuBarProperty, value);
    }

    /// <summary>The caption icon.</summary>
    public IImage? FrameIcon
    {
        get => GetValue(FrameIconProperty);
        set => SetValue(FrameIconProperty, value);
    }

    /// <summary>Whether the caption shows <see cref="FrameIcon"/>. Default true.</summary>
    public bool ShowFrameIcon
    {
        get => GetValue(ShowFrameIconProperty);
        set => SetValue(ShowFrameIconProperty, value);
    }

    /// <summary>Content in the frame's caption, after the icon (see <see cref="WindowFrame.CaptionContent"/>).</summary>
    public object? CaptionContent
    {
        get => GetValue(CaptionContentProperty);
        set => SetValue(CaptionContentProperty, value);
    }

    /// <summary>Content laid over the title area of the frame's caption (see <see cref="WindowFrame.CaptionOverlay"/>).</summary>
    public object? CaptionOverlay
    {
        get => GetValue(CaptionOverlayProperty);
        set => SetValue(CaptionOverlayProperty, value);
    }

    /// <summary>True when the window is transparent, so the frame can draw its shadow and the window can animate.</summary>
    public bool IsShadowVisible
    {
        get => _isShadowVisible;
        private set => SetAndRaise(IsShadowVisibleProperty, ref _isShadowVisible, value);
    }

    /// <summary>True when the platform granted Mica, which only Fluent requests.</summary>
    public bool IsBackdropVisible
    {
        get => _isBackdropVisible;
        private set => SetAndRaise(IsBackdropVisibleProperty, ref _isBackdropVisible, value);
    }

    /// <summary>The frame part, once the template is applied.</summary>
    public WindowFrame? Frame => _frame;

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ThemeWindow);

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
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActualTransparencyLevelProperty)
        {
            IsShadowVisible = ActualTransparencyLevel != WindowTransparencyLevel.None;
            IsBackdropVisible = ActualTransparencyLevel == WindowTransparencyLevel.Mica;
        }
        else if (change.Property == WindowStateProperty)
        {
            var state = change.GetNewValue<WindowState>();
            if (_lastState == WindowState.Minimized && state != WindowState.Minimized && _frame is not null && CanAnimate())
            {
                WindowAnimations.Reset(_frame);
                _ = WindowAnimations.PlayOpen(_frame, Motion());
            }

            _lastState = state;
            if (state == WindowState.Normal && OperatingSystem.IsMacOS())
            {
                EnableNativeResize();
            }
        }
        else if (change.Property == CanResizeProperty && change.GetNewValue<bool>() && OperatingSystem.IsMacOS())
        {
            EnableNativeResize();
        }
    }

    /// <summary>Fluent asks for Mica first (Windows 11); every family then asks for a transparent window, else opaque.</summary>
    private void UpdateTransparencyHint()
    {
        var fluent = this.TryFindResource(Chrome.ChromeResources.FamilyKey, ActualThemeVariant, out var f) && f is ThemeFamily.Fluent;
        var wantsMica = TransparencyLevelHint.Count > 0 && TransparencyLevelHint[0] == WindowTransparencyLevel.Mica;
        if (fluent != wantsMica)
        {
            TransparencyLevelHint = fluent
                ? [WindowTransparencyLevel.Mica, WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None]
                : [WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None];
        }
    }

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        UpdateTransparencyHint();
        base.OnOpened(e);
        IsShadowVisible = ActualTransparencyLevel != WindowTransparencyLevel.None;
        if (_frame is not null && CanAnimate())
        {
            _ = WindowAnimations.PlayOpen(_frame, Motion());
        }
    }

    /// <inheritdoc/>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel || _closeAnimated || _frame is null || !CanAnimate() || Motion() == WindowMotion.None)
        {
            return;
        }

        e.Cancel = true;
        _closeAnimated = true;
        var frame = _frame;
        WindowAnimations.PlayClose(frame, Motion()).ContinueWith(_ => Dispatcher.UIThread.Post(Close), System.Threading.Tasks.TaskScheduler.Default);
    }

    /// <summary>The window motion of the family in effect.</summary>
    protected WindowMotion Motion() =>
        this.TryFindResource(WindowMotionKey, ActualThemeVariant, out var v) && v is WindowMotion m ? m : WindowMotion.None;

    private bool CanAnimate() => IsShadowVisible && WpfAnimations.IsMotionEnabled(this);

    private void OnCaptionButton(object? sender, CaptionButtonInvokedEventArgs e)
    {
        switch (e.Button)
        {
            case CaptionButton.Minimize:
                Minimize();
                break;
            case CaptionButton.Maximize:
                WindowState = WindowState.Maximized;
                break;
            case CaptionButton.Restore:
                WindowState = WindowState.Normal;
                break;
            case CaptionButton.Close:
                Close();
                break;
        }

        e.Handled = true;
    }

    private async void Minimize()
    {
        if (_minimizing)
        {
            return;
        }

        _minimizing = true;
        try
        {
            if (_frame is not null && CanAnimate())
            {
                await WindowAnimations.PlayClose(_frame, Motion());
            }

            WindowState = WindowState.Minimized;
        }
        finally
        {
            _minimizing = false;
        }
    }

    private void OnDragRequested(object? sender, WindowDragRequestedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            return;
        }

        BeginMoveDrag(e.Pointer);
        e.Handled = true;
    }

    private void OnResizeRequested(object? sender, WindowResizeRequestedEventArgs e)
    {
        // On macOS the window itself is resizable (see EnableNativeResize), so the system resizes it from its edges.
        if (!CanResize || WindowState != WindowState.Normal || OperatingSystem.IsMacOS())
        {
            return;
        }

        BeginResizeDrag(e.Edge, e.Pointer);
        e.Handled = true;
    }

    /// <summary>
    /// Avalonia.Native has no resize drag for an undecorated window, so on macOS the window is made resizable natively.
    /// Avalonia rewrites the style mask as the window changes, so this runs again after each activation.
    /// </summary>
    private void EnableNativeResize()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (CanResize && TryGetPlatformHandle() is { HandleDescriptor: "NSWindow" } handle)
            {
                NativeMacOS.EnableResizable(handle.Handle);
            }
        }, DispatcherPriority.Background);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new ThemeWindowAutomationPeer(this);

    private static class NativeMacOS
    {
        private const string ObjCLibrary = "/usr/lib/libobjc.dylib";
        private const ulong NSWindowStyleMaskResizable = 1 << 3;

        [DllImport(ObjCLibrary)]
        private static extern IntPtr sel_registerName(string selector);

        [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        private static extern ulong SendUInt64(IntPtr receiver, IntPtr selector);

        [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
        private static extern void SendVoid(IntPtr receiver, IntPtr selector, ulong arg);

        /// <summary>Adds NSWindowStyleMaskResizable to an NSWindow's style mask.</summary>
        public static void EnableResizable(IntPtr window)
        {
            var mask = SendUInt64(window, sel_registerName("styleMask"));
            if ((mask & NSWindowStyleMaskResizable) == 0)
            {
                SendVoid(window, sel_registerName("setStyleMask:"), mask | NSWindowStyleMaskResizable);
            }
        }
    }
}
