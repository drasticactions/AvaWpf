// Ported from WPF $R/Microsoft/Windows/Controls/KeyTipService.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaWpf.Ribbon;

/// <summary>
/// KeyTips: the keyboard shortcuts the Ribbon shows when Alt or F10 is pressed and released.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="KeyTipProperty"/> gives an element a KeyTip; <see cref="IsKeyTipScopeProperty"/> makes an element a scope
/// whose KeyTips show only after its own KeyTip is typed. The window is the outermost scope.
/// </para>
/// <para>
/// Typing a KeyTip raises <see cref="PreviewKeyTipAccessedEvent"/> and <see cref="KeyTipAccessedEvent"/> on its element,
/// then shows the next scope or ends KeyTip mode. Esc steps back one scope; Alt+Esc, a pointer press, Enter, Space,
/// Tab, the arrow keys or Alt again leave KeyTip mode.
/// </para>
/// </remarks>
public static class KeyTipService
{
    /// <summary>Defines the KeyTip attached property: the letters that activate an element in KeyTip mode.</summary>
    public static readonly AttachedProperty<string?> KeyTipProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("KeyTip", typeof(KeyTipService));

    /// <summary>Defines the IsKeyTipScope attached property: whether the element's descendants form a KeyTip scope.</summary>
    public static readonly AttachedProperty<bool> IsKeyTipScopeProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsKeyTipScope", typeof(KeyTipService));

    /// <summary>Defines the KeyTipTheme attached property: the ControlTheme of the element's <see cref="KeyTipControl"/> (WPF's KeyTipStyle).</summary>
    public static readonly AttachedProperty<ControlTheme?> KeyTipThemeProperty =
        AvaloniaProperty.RegisterAttached<Control, ControlTheme?>("KeyTipTheme", typeof(KeyTipService), inherits: true);

    /// <summary>Raised on an element just before its KeyTip shows, so a handler can place or hide it.</summary>
    public static readonly RoutedEvent<ActivatingKeyTipEventArgs> ActivatingKeyTipEvent =
        RoutedEvent.Register<ActivatingKeyTipEventArgs>("ActivatingKeyTip", RoutingStrategies.Bubble, typeof(KeyTipService));

    /// <summary>Raised (tunnelling) when the user types an element's KeyTip.</summary>
    public static readonly RoutedEvent<KeyTipAccessedEventArgs> PreviewKeyTipAccessedEvent =
        RoutedEvent.Register<KeyTipAccessedEventArgs>("PreviewKeyTipAccessed", RoutingStrategies.Tunnel, typeof(KeyTipService));

    /// <summary>Raised (bubbling) when the user types an element's KeyTip.</summary>
    public static readonly RoutedEvent<KeyTipAccessedEventArgs> KeyTipAccessedEvent =
        RoutedEvent.Register<KeyTipAccessedEventArgs>("KeyTipAccessed", RoutingStrategies.Bubble, typeof(KeyTipService));

    private static readonly Stack<Control> s_scopeStack = new();
    private static readonly Dictionary<Control, KeyTipAdorner> s_shown = new();
    private static List<Control> s_active = new();
    private static TopLevel? s_globalScope;
    private static string s_prefix = string.Empty;
    private static Key s_modeEnterKey;
    private static Key s_probableModeEnterKey;
    private static bool s_textFromKeyDown;

    static KeyTipService()
    {
        InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.KeyDownEvent.AddClassHandler<TopLevel>(OnKeyDown, RoutingStrategies.Bubble);
        InputElement.KeyUpEvent.AddClassHandler<TopLevel>(OnPreviewKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.TextInputEvent.AddClassHandler<TopLevel>(OnPreviewTextInput, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(OnPreviewPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerWheelChangedEvent.AddClassHandler<TopLevel>(OnPreviewPointerWheel, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <summary>The state of KeyTip mode.</summary>
    internal static KeyTipState State { get; private set; }

    /// <summary>The elements whose KeyTips are showing (after prefix filtering).</summary>
    internal static IReadOnlyList<Control> ActiveElements => s_active;

    /// <summary>The innermost scope whose KeyTips are showing.</summary>
    internal static Control? CurrentScope => s_scopeStack.Count > 0 ? s_scopeStack.Peek() : null;

    /// <summary>Registers the input handlers. Called by the Ribbon controls; harmless to call again.</summary>
    internal static void EnsureRegistered()
    {
    }

    /// <summary>Gets the KeyTip of an element.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The KeyTip.</returns>
    public static string? GetKeyTip(Control element) => element.GetValue(KeyTipProperty);

    /// <summary>Sets the KeyTip of an element.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The KeyTip.</param>
    public static void SetKeyTip(Control element, string? value) => element.SetValue(KeyTipProperty, value);

    /// <summary>Gets whether an element is a KeyTip scope.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The value.</returns>
    public static bool GetIsKeyTipScope(Control element) => element.GetValue(IsKeyTipScopeProperty);

    /// <summary>Sets whether an element is a KeyTip scope.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetIsKeyTipScope(Control element, bool value) => element.SetValue(IsKeyTipScopeProperty, value);

    /// <summary>Gets the KeyTip theme of an element.</summary>
    /// <param name="element">The element.</param>
    /// <returns>The theme.</returns>
    public static ControlTheme? GetKeyTipTheme(Control element) => element.GetValue(KeyTipThemeProperty);

    /// <summary>Sets the KeyTip theme of an element.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The theme.</param>
    public static void SetKeyTipTheme(Control element, ControlTheme? value) => element.SetValue(KeyTipThemeProperty, value);

    /// <summary>Adds a handler for <see cref="ActivatingKeyTipEvent"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="handler">The handler.</param>
    public static void AddActivatingKeyTipHandler(Interactive element, EventHandler<ActivatingKeyTipEventArgs> handler) => element.AddHandler(ActivatingKeyTipEvent, handler);

    /// <summary>Removes a handler for <see cref="ActivatingKeyTipEvent"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="handler">The handler.</param>
    public static void RemoveActivatingKeyTipHandler(Interactive element, EventHandler<ActivatingKeyTipEventArgs> handler) => element.RemoveHandler(ActivatingKeyTipEvent, handler);

    /// <summary>Adds a handler for <see cref="PreviewKeyTipAccessedEvent"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="handler">The handler.</param>
    public static void AddPreviewKeyTipAccessedHandler(Interactive element, EventHandler<KeyTipAccessedEventArgs> handler) => element.AddHandler(PreviewKeyTipAccessedEvent, handler);

    /// <summary>Removes a handler for <see cref="PreviewKeyTipAccessedEvent"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="handler">The handler.</param>
    public static void RemovePreviewKeyTipAccessedHandler(Interactive element, EventHandler<KeyTipAccessedEventArgs> handler) => element.RemoveHandler(PreviewKeyTipAccessedEvent, handler);

    /// <summary>Adds a handler for <see cref="KeyTipAccessedEvent"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="handler">The handler.</param>
    public static void AddKeyTipAccessedHandler(Interactive element, EventHandler<KeyTipAccessedEventArgs> handler) => element.AddHandler(KeyTipAccessedEvent, handler);

    /// <summary>Removes a handler for <see cref="KeyTipAccessedEvent"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="handler">The handler.</param>
    public static void RemoveKeyTipAccessedHandler(Interactive element, EventHandler<KeyTipAccessedEventArgs> handler) => element.RemoveHandler(KeyTipAccessedEvent, handler);

    /// <summary>Leaves KeyTip mode and hides every KeyTip.</summary>
    public static void DismissKeyTips() => LeaveKeyTipMode();

    private static bool IsKeyTipKey(KeyEventArgs e) =>
        e.Key is Key.LeftAlt or Key.RightAlt or Key.F10 &&
        (e.KeyModifiers & (KeyModifiers.Shift | KeyModifiers.Control)) == 0;

    private static bool IsAltKey(Key key) => key is Key.LeftAlt or Key.RightAlt;

    private static bool IsKeyTipClosingKey(Key key) => key is Key.Space or Key.Enter or Key.Left or Key.Right or Key.Up or Key.Down
        or Key.PageDown or Key.PageUp or Key.Home or Key.End or Key.Tab or Key.NumLock or Key.LeftShift or Key.RightShift;

    private static void OnPreviewKeyDown(TopLevel sender, KeyEventArgs e)
    {
        if (State == KeyTipState.None)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            if ((e.KeyModifiers & KeyModifiers.Alt) != 0)
            {
                LeaveKeyTipMode();
            }
            else
            {
                PopKeyTipScope();
            }

            e.Handled = true;
            return;
        }

        if (IsAltKey(e.Key))
        {
            if (s_modeEnterKey == Key.None && s_probableModeEnterKey == Key.None)
            {
                s_probableModeEnterKey = e.Key;
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.F10)
        {
            e.Handled = true;
            return;
        }

        if (IsKeyTipClosingKey(e.Key))
        {
            LeaveKeyTipMode();
            return;
        }

        if (TextOf(e) is { } text)
        {
            s_textFromKeyDown = true;
            ProcessText(text);
            e.Handled = true;
        }
    }

    private static void OnKeyDown(TopLevel sender, KeyEventArgs e)
    {
        if (State != KeyTipState.None || !IsKeyTipKey(e))
        {
            return;
        }

        if (EnterKeyTipMode(sender))
        {
            e.Handled = true;
            if (e.Key != Key.F10)
            {
                s_modeEnterKey = e.Key;
            }
        }
    }

    private static void OnPreviewKeyUp(TopLevel sender, KeyEventArgs e)
    {
        if (State == KeyTipState.None || !(IsAltKey(e.Key) || e.Key == Key.F10))
        {
            return;
        }

        if (State == KeyTipState.Pending && ((s_modeEnterKey == Key.None && e.Key == Key.F10) || IsAltKey(e.Key)))
        {
            ShowKeyTips();
        }
        else if (s_modeEnterKey == Key.None)
        {
            LeaveKeyTipMode();
            e.Handled = true;
        }

        if (s_modeEnterKey == e.Key)
        {
            s_modeEnterKey = Key.None;
        }

        if (s_probableModeEnterKey == e.Key)
        {
            s_probableModeEnterKey = Key.None;
        }
    }

    private static void OnPreviewTextInput(TopLevel sender, TextInputEventArgs e)
    {
        if (State == KeyTipState.None)
        {
            return;
        }

        e.Handled = true;
        if (s_textFromKeyDown)
        {
            s_textFromKeyDown = false;
            return;
        }

        if (!string.IsNullOrEmpty(e.Text) && e.Text != " ")
        {
            ProcessText(e.Text);
        }
    }

    private static void OnPreviewPointerPressed(TopLevel sender, PointerPressedEventArgs e)
    {
        if (State != KeyTipState.None)
        {
            LeaveKeyTipMode();
        }
    }

    private static void OnPreviewPointerWheel(TopLevel sender, PointerWheelEventArgs e)
    {
        if (State != KeyTipState.None)
        {
            e.Handled = true;
        }
    }

    /// <summary>The letter or digit a key types, from its key symbol or, with Alt held, from the key itself.</summary>
    private static string? TextOf(KeyEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.KeySymbol) && e.KeySymbol.Length == 1 && !char.IsControl(e.KeySymbol[0]) && e.KeySymbol != " ")
        {
            return e.KeySymbol;
        }

        return e.Key switch
        {
            >= Key.A and <= Key.Z => ((char)('A' + (e.Key - Key.A))).ToString(),
            >= Key.D0 and <= Key.D9 => ((char)('0' + (e.Key - Key.D0))).ToString(),
            >= Key.NumPad0 and <= Key.NumPad9 => ((char)('0' + (e.Key - Key.NumPad0))).ToString(),
            _ => null,
        };
    }

    private static void ProcessText(string text)
    {
        if (State == KeyTipState.Pending)
        {
            ShowKeyTips();
        }

        if (s_active.Count == 0)
        {
            return;
        }

        if (s_probableModeEnterKey != Key.None && s_modeEnterKey == Key.None)
        {
            // Text typed with Alt held: the Alt release does not dismiss the KeyTips this once.
            s_modeEnterKey = s_probableModeEnterKey;
            s_probableModeEnterKey = Key.None;
        }

        var prefix = s_prefix + text;
        Control? exact = null;
        var partial = new List<Control>();
        foreach (var element in s_active)
        {
            var keyTip = GetKeyTip(element) ?? string.Empty;
            if (string.Equals(keyTip, prefix, StringComparison.CurrentCultureIgnoreCase))
            {
                exact = element;
                break;
            }

            if (keyTip.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
            {
                partial.Add(element);
            }
        }

        if (exact is not null)
        {
            OnKeyTipExactMatch(exact);
        }
        else if (partial.Count > 0)
        {
            foreach (var element in s_active)
            {
                if (!partial.Contains(element))
                {
                    HideKeyTipForElement(element);
                }
            }

            s_active = partial;
            s_prefix = prefix;
        }
    }

    private static void OnKeyTipExactMatch(Control element)
    {
        if (!element.IsEffectivelyEnabled)
        {
            return;
        }

        HideCurrentShowingKeyTips();
        s_prefix = string.Empty;
        var args = new KeyTipAccessedEventArgs(PreviewKeyTipAccessedEvent) { Source = element };
        element.RaiseEvent(args);
        var bubble = new KeyTipAccessedEventArgs(KeyTipAccessedEvent) { Source = element, TargetKeyTipScope = args.TargetKeyTipScope };
        element.RaiseEvent(bubble);

        // A handler may have left KeyTip mode.
        if (State == KeyTipState.None)
        {
            return;
        }

        var newScope = bubble.TargetKeyTipScope;
        if (newScope is null && GetIsKeyTipScope(element))
        {
            newScope = element;
        }

        if (newScope is not null)
        {
            // The scope's content (a popup, a newly selected tab) appears in the next layout pass.
            Dispatcher.UIThread.Post(() =>
            {
                if (State != KeyTipState.None)
                {
                    ShowKeyTipsForScope(newScope, pushOnEmpty: true);
                }
            }, DispatcherPriority.Loaded);
        }
        else
        {
            LeaveKeyTipMode();
        }
    }

    private static bool EnterKeyTipMode(TopLevel topLevel)
    {
        if (CollectKeyTipElements(topLevel).Count == 0)
        {
            return false;
        }

        s_globalScope = topLevel;
        if (topLevel is WindowBase window)
        {
            window.Deactivated += OnWindowDeactivated;
        }

        State = KeyTipState.Pending;
        return true;
    }

    private static void OnWindowDeactivated(object? sender, EventArgs e) => LeaveKeyTipMode();

    private static void ShowKeyTips()
    {
        if (State != KeyTipState.Pending || s_globalScope is null)
        {
            return;
        }

        if (ShowKeyTipsForScope(s_globalScope, pushOnEmpty: false))
        {
            if (State == KeyTipState.Pending)
            {
                State = KeyTipState.Enabled;
            }
        }
        else
        {
            LeaveKeyTipMode();
        }
    }

    private static bool ShowKeyTipsForScope(Control scope, bool pushOnEmpty)
    {
        var shown = false;
        HideCurrentShowingKeyTips();
        foreach (var element in CollectKeyTipElements(scope))
        {
            shown |= ShowKeyTipForElement(element);
        }

        if (State == KeyTipState.None)
        {
            return shown;
        }

        if ((shown || pushOnEmpty) && (s_scopeStack.Count == 0 || s_scopeStack.Peek() != scope))
        {
            s_scopeStack.Push(scope);
        }

        if (State == KeyTipState.Pending && pushOnEmpty)
        {
            State = KeyTipState.Enabled;
        }

        return shown;
    }

    private static void PopKeyTipScope()
    {
        if (s_scopeStack.Count == 0)
        {
            LeaveKeyTipMode();
            return;
        }

        var current = s_scopeStack.Pop();
        HideCurrentShowingKeyTips();
        s_prefix = string.Empty;
        CloseScope(current);
        if (s_scopeStack.Count > 0)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (State != KeyTipState.None && s_scopeStack.Count > 0)
                {
                    ShowKeyTipsForScope(s_scopeStack.Peek(), pushOnEmpty: true);
                }
            }, DispatcherPriority.Loaded);
        }
        else
        {
            LeaveKeyTipMode();
        }
    }

    /// <summary>Closes the drop-down a scope opened when the user steps back out of it with Esc.</summary>
    private static void CloseScope(Control scope)
    {
        switch (scope)
        {
            case RibbonMenuButton menu:
                menu.IsDropDownOpen = false;
                break;
            case RibbonGroup group:
                group.IsDropDownOpen = false;
                break;
            case RibbonTab tab when tab.Ribbon is { IsMinimized: true } ribbon:
                ribbon.IsDropDownOpen = false;
                break;
            case RibbonMenuItem item:
                item.IsSubMenuOpen = false;
                break;
        }
    }

    private static void LeaveKeyTipMode()
    {
        HideCurrentShowingKeyTips();
        if (s_globalScope is WindowBase window)
        {
            window.Deactivated -= OnWindowDeactivated;
        }

        s_globalScope = null;
        s_prefix = string.Empty;
        s_modeEnterKey = Key.None;
        s_probableModeEnterKey = Key.None;
        s_textFromKeyDown = false;
        s_scopeStack.Clear();
        State = KeyTipState.None;
    }

    private static void HideCurrentShowingKeyTips()
    {
        foreach (var element in s_active)
        {
            HideKeyTipForElement(element);
        }

        s_active = new List<Control>();
    }

    private static bool ShowKeyTipForElement(Control element)
    {
        var args = new ActivatingKeyTipEventArgs { Source = element };
        element.RaiseEvent(args);
        if (!args.KeyTipVisibility)
        {
            return false;
        }

        s_active.Add(element);
        var target = args.PlacementTarget ?? element;
        if (AdornerLayer.GetAdornerLayer(target) is not null && AdornerLayer.GetAdorner(target) is null)
        {
            var keyTip = new KeyTipControl { Text = GetKeyTip(element) };
            if (GetKeyTipTheme(element) is { } theme)
            {
                keyTip.Theme = theme;
            }

            var adorner = new KeyTipAdorner(keyTip, args);
            AdornerLayer.SetIsClipEnabled(adorner, false);
            AdornerLayer.SetAdorner(target, adorner);
            s_shown[element] = adorner;
        }

        return true;
    }

    private static void HideKeyTipForElement(Control element)
    {
        if (!s_shown.Remove(element, out var adorner))
        {
            return;
        }

        if (adorner.GetValue(AdornerLayer.AdornedElementProperty) is Visual target && AdornerLayer.GetAdorner(target) == adorner)
        {
            AdornerLayer.SetAdorner(target, null);
        }
    }

    /// <summary>
    /// The elements of a scope that have a KeyTip: every visible descendant with a KeyTip whose nearest scope is
    /// <paramref name="scope"/>. Open popups count as descendants of the control that owns them.
    /// </summary>
    internal static List<Control> CollectKeyTipElements(Control scope)
    {
        var result = new List<Control>();
        foreach (var child in Children(scope))
        {
            Visit(child, result);
        }

        return result;
    }

    private static void Visit(Visual visual, List<Control> result)
    {
        if (!visual.IsVisible)
        {
            return;
        }

        if (visual is Control control)
        {
            if (!string.IsNullOrEmpty(GetKeyTip(control)) && control.IsEffectivelyVisible)
            {
                result.Add(control);
            }

            if (control is IKeyTipSiblingProvider provider)
            {
                foreach (var sibling in provider.KeyTipSiblings)
                {
                    if (!string.IsNullOrEmpty(GetKeyTip(sibling)) && sibling.IsEffectivelyVisible)
                    {
                        result.Add(sibling);
                    }
                }
            }

            if (GetIsKeyTipScope(control))
            {
                return;
            }
        }

        foreach (var child in Children(visual))
        {
            Visit(child, result);
        }
    }

    private static IEnumerable<Visual> Children(Visual visual)
    {
        foreach (var child in visual.GetVisualChildren())
        {
            yield return child;
        }

        if (visual is Popup { IsOpen: true, Child: { } popupChild })
        {
            yield return popupChild;
        }
    }
}
