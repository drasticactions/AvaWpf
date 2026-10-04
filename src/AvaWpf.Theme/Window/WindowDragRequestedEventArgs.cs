using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaWpf;

/// <summary>Raised by a <see cref="WindowFrame"/> when the caption is pressed, so the host can move the window.</summary>
public sealed class WindowDragRequestedEventArgs : RoutedEventArgs
{
    /// <summary>Initializes the event arguments.</summary>
    public WindowDragRequestedEventArgs(RoutedEvent routedEvent, PointerPressedEventArgs pointer) : base(routedEvent)
    {
        Pointer = pointer;
    }

    /// <summary>The press that started the drag.</summary>
    public PointerPressedEventArgs Pointer { get; }
}
