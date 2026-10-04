// Ported from WPF $R/Microsoft/Windows/Controls/KeyTipAccessedEventArgs.cs (MIT, see NOTICE.md).
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaWpf.Ribbon;

/// <summary>
/// The data of <see cref="KeyTipService.PreviewKeyTipAccessedEvent"/> and <see cref="KeyTipService.KeyTipAccessedEvent"/>,
/// raised when the user types an element's KeyTip.
/// </summary>
public class KeyTipAccessedEventArgs : RoutedEventArgs
{
    /// <summary>Initializes the arguments.</summary>
    /// <param name="routedEvent">The event.</param>
    public KeyTipAccessedEventArgs(RoutedEvent routedEvent)
        : base(routedEvent)
    {
    }

    /// <summary>
    /// The KeyTip scope to show next (an element with <see cref="KeyTipService.IsKeyTipScopeProperty"/> set, such as a
    /// tab or a drop-down button). Null leaves KeyTip mode, unless the element itself is a scope.
    /// </summary>
    public Control? TargetKeyTipScope { get; set; }
}
