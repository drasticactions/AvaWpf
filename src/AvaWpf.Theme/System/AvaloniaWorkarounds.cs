using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaWpf;

/// <summary>Process-wide fixes for Avalonia behaviour the WPF templates depend on, installed once by the theme.</summary>
internal static class AvaloniaWorkarounds
{
    private static bool s_registered;

    internal static void EnsureRegistered()
    {
        if (s_registered)
        {
            return;
        }

        s_registered = true;

        // Avalonia folds RenderTransformOrigin into the composition transform only when something else invalidates the
        // visual, so a storyboard that animates the origin alone (WPF's indeterminate ProgressBar sweep) never moves.
        Visual.RenderTransformOriginProperty.Changed.AddClassHandler<Visual>((v, _) => v.InvalidateVisual());

        // Avalonia's Wayland backend raises TextInput for an unhandled Escape (and other control keys) with the control
        // character as its text, which a TextBox inserts as a missing-glyph box. No control character is text input:
        // the keys that edit (Back, Tab, Enter) are handled on KeyDown.
        InputElement.TextInputEvent.AddClassHandler<TopLevel>((_, e) =>
        {
            if (e.Text is { Length: 1 } text && (text[0] < ' ' || text[0] == '\x7f'))
            {
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
    }
}
