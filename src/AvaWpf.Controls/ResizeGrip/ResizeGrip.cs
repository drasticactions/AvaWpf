// Ported from WPF $W/PresentationFramework/System/Windows/Controls/Primitives/ResizeGrip.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Automation.Peers;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Controls;

/// <summary>
/// The grip in the bottom corner of a window; pressing it resizes the <see cref="Window"/>, and it hides while the window
/// is maximized or cannot resize.
/// </summary>
public class ResizeGrip : TemplatedControl
{
    private Window? _window;

    static ResizeGrip()
    {
        FocusableProperty.OverrideDefaultValue<ResizeGrip>(false);
    }

    /// <summary>Raised before a resize drag is handed to the window, with the edge being dragged.</summary>
    internal event EventHandler<WindowEdge>? ResizeDragRequested;

    /// <summary>The edge a drag resizes: the bottom-right corner, flipped in right-to-left flow.</summary>
    internal WindowEdge Edge => FlowDirection == FlowDirection.RightToLeft ? WindowEdge.SouthWest : WindowEdge.SouthEast;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is not null)
        {
            _window.PropertyChanged += OnWindowPropertyChanged;
        }

        UpdateVisibility();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_window is not null)
        {
            _window.PropertyChanged -= OnWindowPropertyChanged;
            _window = null;
        }
    }

    /// <summary>Starts the window resize drag.</summary>
    /// <param name="e">The event data.</param>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || _window is not { CanResize: true, WindowState: not WindowState.Maximized } window ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var edge = Edge;
        ResizeDragRequested?.Invoke(this, edge);
        WindowResizeDrag.Begin(window, edge, e);
        e.Handled = true;
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty || e.Property == Window.CanResizeProperty)
        {
            UpdateVisibility();
        }
    }

    private void UpdateVisibility()
    {
        if (_window is { } window)
        {
            SetCurrentValue(IsVisibleProperty, window.CanResize && window.WindowState != WindowState.Maximized);
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new ResizeGripAutomationPeer(this);
}
