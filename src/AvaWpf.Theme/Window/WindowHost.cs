using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaWpf;

/// <summary>
/// Shows dialog content in a window the platform can have: a <see cref="ThemeWindow"/> where windows exist (the
/// desktop) and an <see cref="InPageWindow"/> in the page where they do not (the browser). The content declares its
/// window with <see cref="WindowSettings"/> and closes it with <see cref="CloseDialog"/>.
/// </summary>
public static class WindowHost
{
    /// <summary>Raised on the content once its window has opened and laid out.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> OpenedEvent =
        RoutedEvent.Register<Control, RoutedEventArgs>("WindowHostOpened", RoutingStrategies.Direct);

    /// <summary>Raised on the content when its window is closing; set Cancel to keep it open.</summary>
    public static readonly RoutedEvent<WindowHostClosingEventArgs> ClosingEvent =
        RoutedEvent.Register<Control, WindowHostClosingEventArgs>("WindowHostClosing", RoutingStrategies.Direct);

    /// <summary>Raised on the content after its window closed.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClosedEvent =
        RoutedEvent.Register<Control, RoutedEventArgs>("WindowHostClosed", RoutingStrategies.Direct);

    /// <summary>
    /// True shows windows in the page. Unset, the application lifetime decides: in-page windows for a single-view
    /// lifetime (browser, mobile), real windows otherwise. Tests set it to try both.
    /// </summary>
    public static bool? UseInPageWindows { get; set; }

    /// <summary>Whether windows open in the page under the current settings.</summary>
    public static bool InPage =>
        UseInPageWindows ?? Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime;

    /// <summary>Adds a handler for <see cref="OpenedEvent"/>.</summary>
    public static void AddOpenedHandler(Control content, EventHandler<RoutedEventArgs> handler) => content.AddHandler(OpenedEvent, handler);

    /// <summary>Adds a handler for <see cref="ClosingEvent"/>.</summary>
    public static void AddClosingHandler(Control content, EventHandler<WindowHostClosingEventArgs> handler) => content.AddHandler(ClosingEvent, handler);

    /// <summary>Adds a handler for <see cref="ClosedEvent"/>.</summary>
    public static void AddClosedHandler(Control content, EventHandler<RoutedEventArgs> handler) => content.AddHandler(ClosedEvent, handler);

    /// <summary>Shows <paramref name="content"/> as a modal dialog over <paramref name="owner"/>'s window; completes with the result passed to <see cref="CloseDialog"/>.</summary>
    public static async Task<T?> ShowDialogAsync<T>(Control content, Visual owner)
    {
        object? result = await ShowDialogCoreAsync(content, owner);
        return result is T t ? t : default;
    }

    /// <summary>Shows <paramref name="content"/> as a modal dialog over <paramref name="owner"/>'s window.</summary>
    public static Task ShowDialogAsync(Control content, Visual owner) => ShowDialogCoreAsync(content, owner);

    /// <summary>Shows <paramref name="content"/> in a modeless window over <paramref name="owner"/>'s window.</summary>
    public static IWindowHandle Show(Control content, Visual owner)
    {
        if (InPage)
        {
            InPageWindow window = CreateInPage(content);
            Layer(owner).Show(window, modal: false);
            return new InPageHandle(window);
        }

        ThemeWindow desktop = CreateDesktop(content);
        if (TopLevel.GetTopLevel(owner) is Window ownerWindow)
        {
            desktop.Show(ownerWindow);
        }
        else
        {
            desktop.Show();
        }

        return new DesktopHandle(desktop);
    }

    /// <summary>Closes the window showing <paramref name="content"/> with <paramref name="result"/>.</summary>
    public static void CloseDialog(Control content, object? result = null)
    {
        switch (HostOf(content))
        {
            case InPageWindow page:
                page.Close(result);
                break;
            case Window window:
                window.Close(result);
                break;
        }
    }

    /// <summary>The window (a <see cref="Window"/> or an <see cref="InPageWindow"/>) showing <paramref name="content"/>, or null.</summary>
    public static Control? HostOf(Control content)
    {
        for (Visual? v = content; v is not null; v = v.GetVisualParent())
        {
            if (v is InPageWindow or Window)
            {
                return (Control)v;
            }
        }

        return content.Parent as Control;
    }

    private static async Task<object?> ShowDialogCoreAsync(Control content, Visual owner)
    {
        if (InPage)
        {
            InPageWindow window = CreateInPage(content);
            Layer(owner).Show(window, modal: true);
            return await window.Result;
        }

        ThemeWindow desktop = CreateDesktop(content);
        if (TopLevel.GetTopLevel(owner) is not Window ownerWindow)
        {
            throw new InvalidOperationException("A desktop dialog needs an owner inside a window.");
        }

        return await desktop.ShowDialog<object?>(ownerWindow);
    }

    private static InPageWindowLayer Layer(Visual owner) =>
        InPageWindowLayer.GetOrCreate(owner) ?? throw new InvalidOperationException("The owner is not in a top level with an overlay layer.");

    private static InPageWindow CreateInPage(Control content)
    {
        var window = new InPageWindow
        {
            Title = WindowSettings.GetTitle(content),
            FrameIcon = WindowSettings.GetIcon(content),
            FrameKind = WindowSettings.GetFrameKind(content),
            ShowFrameTitle = WindowSettings.GetShowFrameTitle(content),
            ShowFrameIcon = WindowSettings.GetShowFrameIcon(content),
            CanResize = WindowSettings.GetCanResize(content),
            WindowStartupLocation = WindowSettings.GetWindowStartupLocation(content),
            MinWidth = WindowSettings.GetMinWidth(content),
            MinHeight = WindowSettings.GetMinHeight(content),
            MaxWidth = WindowSettings.GetMaxWidth(content),
            MaxHeight = WindowSettings.GetMaxHeight(content),
            Content = content,
        };
        ApplySize(window, content);
        window.Bind(InPageWindow.TitleProperty, content.GetObservable(WindowSettings.TitleProperty));
        return window;
    }

    private static ThemeWindow CreateDesktop(Control content)
    {
        var window = new ThemeWindow
        {
            Title = WindowSettings.GetTitle(content),
            FrameIcon = WindowSettings.GetIcon(content),
            FrameKind = WindowSettings.GetFrameKind(content),
            ShowFrameTitle = WindowSettings.GetShowFrameTitle(content),
            ShowFrameIcon = WindowSettings.GetShowFrameIcon(content),
            CanResize = WindowSettings.GetCanResize(content),
            WindowStartupLocation = WindowSettings.GetWindowStartupLocation(content),
            ShowInTaskbar = WindowSettings.GetShowInTaskbar(content),
            MinWidth = WindowSettings.GetMinWidth(content),
            MinHeight = WindowSettings.GetMinHeight(content),
            MaxWidth = WindowSettings.GetMaxWidth(content),
            MaxHeight = WindowSettings.GetMaxHeight(content),
            Content = content,
        };
        window.SizeToContent = WindowSettings.GetSizeToContent(content);
        if (!double.IsNaN(WindowSettings.GetWidth(content)))
        {
            window.Width = WindowSettings.GetWidth(content);
        }

        if (!double.IsNaN(WindowSettings.GetHeight(content)))
        {
            window.Height = WindowSettings.GetHeight(content);
        }

        window.Bind(Window.TitleProperty, content.GetObservable(WindowSettings.TitleProperty));
        window.Opened += (_, _) => content.RaiseEvent(new RoutedEventArgs(OpenedEvent));
        window.Closing += (_, e) =>
        {
            var args = new WindowHostClosingEventArgs();
            content.RaiseEvent(new WindowHostClosingEventArgs(ClosingEvent, args));
            if (args.Cancel)
            {
                e.Cancel = true;
            }
        };
        window.Closed += (_, _) => content.RaiseEvent(new RoutedEventArgs(ClosedEvent));
        return window;
    }

    private static void ApplySize(InPageWindow window, Control content)
    {
        double w = WindowSettings.GetWidth(content);
        double h = WindowSettings.GetHeight(content);
        if (!double.IsNaN(w))
        {
            window.Width = w;
        }

        if (!double.IsNaN(h))
        {
            window.Height = h;
        }

        window.SizeToContent = WindowSettings.GetSizeToContent(content);
    }

    private sealed class InPageHandle : IWindowHandle
    {
        private readonly InPageWindow _window;

        public InPageHandle(InPageWindow window)
        {
            _window = window;
            window.Closed += (_, _) => Closed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Closed;

        public bool IsOpen => !_window.IsClosed;

        public void Activate() => _window.Activate();

        public void Close() => _window.Close();
    }

    private sealed class DesktopHandle : IWindowHandle
    {
        private readonly Window _window;
        private bool _closed;

        public DesktopHandle(Window window)
        {
            _window = window;
            window.Closed += (_, _) =>
            {
                _closed = true;
                Closed?.Invoke(this, EventArgs.Empty);
            };
        }

        public event EventHandler? Closed;

        public bool IsOpen => !_closed;

        public void Activate() => _window.Activate();

        public void Close() => _window.Close();
    }
}

/// <summary>A modeless window opened by <see cref="WindowHost.Show"/>.</summary>
public interface IWindowHandle
{
    /// <summary>Raised after the window closed.</summary>
    event EventHandler? Closed;

    /// <summary>True until the window closes.</summary>
    bool IsOpen { get; }

    /// <summary>Brings the window to the front.</summary>
    void Activate();

    /// <summary>Closes the window.</summary>
    void Close();
}
