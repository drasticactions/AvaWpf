using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AvaWpf;

/// <summary>
/// WPF's <c>Selector.IsSelectionActive</c>: an inherited attached property that tells a selector's items whether the
/// selector has keyboard focus. Selector themes bind it to <see cref="InputElement.IsKeyboardFocusWithin"/>.
/// </summary>
public static class SelectionState
{
    /// <summary>Defines the inherited <c>IsSelectionActive</c> attached property.</summary>
    public static readonly AttachedProperty<bool> IsSelectionActiveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsSelectionActive", typeof(SelectionState), inherits: true);

    /// <summary>Gets whether the selector that owns <paramref name="element"/> has keyboard focus.</summary>
    /// <param name="element">The selector or one of its items.</param>
    public static bool GetIsSelectionActive(Control element) => element.GetValue(IsSelectionActiveProperty);

    /// <summary>Sets whether the selector has keyboard focus (normally bound by the selector's theme).</summary>
    /// <param name="element">The selector.</param>
    /// <param name="value">Whether keyboard focus is within the selector.</param>
    public static void SetIsSelectionActive(Control element, bool value) => element.SetValue(IsSelectionActiveProperty, value);
}
