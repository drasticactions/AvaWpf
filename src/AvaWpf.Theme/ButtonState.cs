using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaWpf;

/// <summary>
/// WPF's <c>Button.IsDefaulted</c>: true for the default button while focus is in its window and on no other button.
/// Avalonia does not expose it, so it is computed on focus changes and set as the <c>:defaulted</c> pseudo-class.
/// </summary>
public static class ButtonState
{
    /// <summary>The pseudo-class set on a defaulted button.</summary>
    public const string DefaultedPseudoClass = ":defaulted";

    /// <summary>Defines the read-only <c>IsDefaulted</c> attached property.</summary>
    public static readonly AttachedProperty<bool> IsDefaultedProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsDefaulted", typeof(ButtonState));

    private static bool s_registered;

    /// <summary>Gets whether a button is drawn as the default button.</summary>
    public static bool GetIsDefaulted(Button button) => button.GetValue(IsDefaultedProperty);

    internal static void EnsureRegistered()
    {
        if (s_registered)
        {
            return;
        }

        s_registered = true;
        InputElement.GotFocusEvent.AddClassHandler<TopLevel>((top, _) => Update(top), RoutingStrategies.Bubble, handledEventsToo: true);
        InputElement.LostFocusEvent.AddClassHandler<TopLevel>((top, _) => Update(top), RoutingStrategies.Bubble, handledEventsToo: true);
        WindowBase.IsActiveProperty.Changed.AddClassHandler<WindowBase>((w, _) => Update(w));
        Control.LoadedEvent.AddClassHandler<Button>((b, _) =>
        {
            if (b.IsDefault && TopLevel.GetTopLevel(b) is { } top)
            {
                Update(top);
            }
        });
        Button.IsDefaultProperty.Changed.AddClassHandler<Button>((b, _) =>
        {
            if (TopLevel.GetTopLevel(b) is { } top)
            {
                Update(top);
            }
            else
            {
                Set(b, false);
            }
        });
    }

    /// <summary>Recomputes the defaulted state of every default button in <paramref name="top"/>.</summary>
    public static void Update(TopLevel top)
    {
        var focused = top.FocusManager?.GetFocusedElement() as Visual;
        var focusOnButton = focused is Button;
        // With nothing focused, an active window counts as focused, as WPF does.
        var focusInside = focused is null
            ? top is not WindowBase window || window.IsActive
            : focused == top || top.IsVisualAncestorOf(focused);
        foreach (var v in top.GetVisualDescendants())
        {
            if (v is Button b && (b.IsDefault || GetIsDefaulted(b)))
            {
                var defaulted = b.IsDefault && b.IsEffectivelyEnabled && focusInside && (!focusOnButton || focused == b);
                Set(b, defaulted);
            }
        }
    }

    private static void Set(Button b, bool value)
    {
        b.SetValue(IsDefaultedProperty, value);
        ((IPseudoClasses)b.Classes).Set(DefaultedPseudoClass, value);
    }
}
