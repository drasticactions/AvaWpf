using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaWpf;

/// <summary>
/// The layer that shows <see cref="InPageWindow"/>s over a <see cref="TopLevel"/>'s content, in its overlay layer so
/// that a window's menus, drop-downs and tooltips open above it. Windows stack in the order they were activated.
/// While a modal window is open, a transparent scrim blocks the page beneath it (it is not dimmed, as on Windows);
/// a click on the scrim flashes the modal window's frame.
/// </summary>
public class InPageWindowLayer : Panel
{
    private readonly List<InPageWindow> _windows = [];
    private TopLevel? _topLevel;

    static InPageWindowLayer()
    {
        ClipToBoundsProperty.OverrideDefaultValue<InPageWindowLayer>(true);
    }

    /// <summary>Initializes a new instance of the <see cref="InPageWindowLayer"/> class.</summary>
    public InPageWindowLayer()
    {
        AddHandler(PointerPressedEvent, OnScrimPressed, RoutingStrategies.Bubble, handledEventsToo: false);
    }

    /// <summary>The open windows, bottom to top.</summary>
    public IReadOnlyList<InPageWindow> Windows => _windows;

    /// <summary>The window that has the keyboard: the topmost modal window, else the topmost window.</summary>
    public InPageWindow? ActiveWindow => _windows.LastOrDefault(w => w.IsModal) ?? _windows.LastOrDefault();

    /// <summary>True while a modal window is open.</summary>
    public bool HasModal => _windows.Any(w => w.IsModal);

    /// <summary>The layer of <paramref name="visual"/>'s top level, created in its overlay layer the first time.</summary>
    public static InPageWindowLayer? GetOrCreate(Visual visual)
    {
        if (OverlayLayer.GetOverlayLayer(visual) is not { } overlay)
        {
            return null;
        }

        if (overlay.Children.OfType<InPageWindowLayer>().FirstOrDefault() is { } existing)
        {
            return existing;
        }

        var layer = new InPageWindowLayer();
        overlay.Children.Add(layer);
        return layer;
    }

    /// <summary>Shows <paramref name="window"/> at the top of the layer.</summary>
    public void Show(InPageWindow window, bool modal)
    {
        if (window.Layer is not null)
        {
            throw new InvalidOperationException("The window is already open.");
        }

        var focusBefore = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        window.Layer = this;
        window.IsModal = modal;
        window.Opacity = 0;
        _windows.Add(window);
        Children.Add(window);
        UpdateScrim();
        Activate(window);
        window.OnShown(focusBefore);
    }

    /// <summary>Moves <paramref name="window"/> to the top and makes it the active window (unless a modal window is above).</summary>
    public void Activate(InPageWindow window)
    {
        if (!_windows.Contains(window))
        {
            return;
        }

        if (ActiveWindow is { IsModal: true } modal && modal != window)
        {
            modal.Flash();
            return;
        }

        RememberFocus();
        _windows.Remove(window);
        _windows.Add(window);
        Children.Remove(window);
        Children.Add(window);
        foreach (var w in _windows)
        {
            w.IsActive = w == window;
        }

        if (window.IsArrangeValid)
        {
            window.FocusFirst();
        }
    }

    /// <summary>Clamps a window position so the caption stays reachable.</summary>
    public Point Clamp(InPageWindow window, Point position)
    {
        var size = Bounds.Size;
        var w = window.Bounds.Width;
        var x = Math.Clamp(position.X, Math.Min(0, size.Width - w), Math.Max(0, size.Width - 48));
        var y = Math.Clamp(position.Y, 0, Math.Max(0, size.Height - 24));
        return new Point(x, y);
    }

    internal void Remove(InPageWindow window)
    {
        _windows.Remove(window);
        Children.Remove(window);
        window.Layer = null;
        window.IsActive = false;
        if (ActiveWindow is { } next)
        {
            next.IsActive = true;
        }

        UpdateScrim();
    }

    private void RememberFocus()
    {
        if (ActiveWindow is { } active && TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is { } focused)
        {
            active.RememberFocus(focused);
        }
    }

    private void UpdateScrim() => Background = HasModal ? Brushes.Transparent : null;

    private void OnScrimPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source == this && ActiveWindow is { IsModal: true } modal)
        {
            modal.Flash();
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel);
        _topLevel?.AddHandler(GotFocusEvent, OnTopLevelGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        _topLevel?.RemoveHandler(GotFocusEvent, OnTopLevelGotFocus);
        _topLevel = null;
    }

    /// <summary>
    /// While a modal window is open the keyboard belongs to it: its Enter, Esc and access keys run before the page's
    /// (whose default buttons and access keys listen on the top level), and keys from outside it do nothing.
    /// </summary>
    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (ActiveWindow is not { } active)
        {
            return;
        }

        // A popup (a drop-down, a menu) takes its own Enter, Esc and arrows, as a popup window does on the desktop.
        if (IsInPopup(e.Source))
        {
            return;
        }

        var inside = active.Contains(e.Source);
        if (!inside && !active.IsModal)
        {
            return;
        }

        if (active.HandleDialogKey(e))
        {
            e.Handled = true;
        }
        else if (!inside)
        {
            // A modal window is open but the keyboard was elsewhere: bring it back.
            active.FocusFirst();
            e.Handled = e.Key is not (Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift);
        }
    }

    /// <summary>True for an element in a popup (a menu, a drop-down).</summary>
    private static bool IsInPopup(object? element) =>
        element is Visual v && v.GetSelfAndVisualAncestors().Any(a => a is OverlayPopupHost or PopupRoot);

    private void OnTopLevelGotFocus(object? sender, RoutedEventArgs e)
    {
        if (ActiveWindow is { IsModal: true } modal && !modal.Contains(e.Source) && !IsInPopup(e.Source))
        {
            modal.FocusFirst();
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
        }

        return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            if (child is not InPageWindow window)
            {
                child.Arrange(new Rect(finalSize));
                continue;
            }

            var size = window.DesiredSize;
            if (window.Opacity == 0 && !window.IsClosed)
            {
                // First layout: place the window, then show it.
                if (window.WindowStartupLocation != WindowStartupLocation.Manual)
                {
                    window.SetCurrentValue(InPageWindow.PositionProperty, new Point(
                        Math.Max(0, Math.Round((finalSize.Width - size.Width) / 2)),
                        Math.Max(0, Math.Round((finalSize.Height - size.Height) / 2))));
                }

                window.Opacity = 1;
            }

            window.Arrange(new Rect(window.Position, size));
        }

        return finalSize;
    }
}

/// <summary>Access keys (Alt+letter) of one window, so a dialog's keys do not reach the page beneath.</summary>
internal static class AccessKeys
{
    public static bool Invoke(Visual scope, Key key)
    {
        var letter = KeyToString(key);
        if (letter is null)
        {
            return false;
        }

        foreach (var text in scope.GetVisualDescendants().OfType<AccessText>())
        {
            if (!string.Equals(text.AccessKey, letter, StringComparison.OrdinalIgnoreCase) || !text.IsEffectivelyVisible)
            {
                continue;
            }

            for (var v = text.GetVisualParent(); v is not null && v != scope; v = v.GetVisualParent())
            {
                switch (v)
                {
                    case Label { Target: { } target }:
                        target.Focus(NavigationMethod.Tab);
                        return true;
                    case TabItem tab when tab.IsEffectivelyEnabled:
                        tab.IsSelected = true;
                        tab.Focus(NavigationMethod.Tab);
                        return true;
                    case Button button when button.IsEffectivelyEnabled:
                        button.Focus(NavigationMethod.Tab);
                        var peer = ControlAutomationPeer.CreatePeerForElement(button);
                        if (peer.GetProvider<IToggleProvider>() is { } toggle)
                        {
                            toggle.Toggle();
                        }
                        else
                        {
                            peer.GetProvider<IInvokeProvider>()?.Invoke();
                        }

                        return true;
                    case InputElement { Focusable: true, IsEffectivelyEnabled: true } input:
                        input.Focus(NavigationMethod.Tab);
                        return true;
                }
            }
        }

        return false;
    }

    private static string? KeyToString(Key key) => key switch
    {
        >= Key.A and <= Key.Z => ((char)('a' + (key - Key.A))).ToString(),
        >= Key.D0 and <= Key.D9 => ((char)('0' + (key - Key.D0))).ToString(),
        _ => null,
    };
}
