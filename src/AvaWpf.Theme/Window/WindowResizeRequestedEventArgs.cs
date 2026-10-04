using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaWpf;

/// <summary>Raised by a <see cref="WindowFrame"/> when a border is pressed, so the host can resize the window.</summary>
public sealed class WindowResizeRequestedEventArgs : RoutedEventArgs
{
    /// <summary>Initializes the event arguments.</summary>
    public WindowResizeRequestedEventArgs(RoutedEvent routedEvent, WindowEdge edge, PointerPressedEventArgs pointer) : base(routedEvent)
    {
        Edge = edge;
        Pointer = pointer;
    }

    /// <summary>The edge or corner pressed.</summary>
    public WindowEdge Edge { get; }

    /// <summary>The press that started the resize.</summary>
    public PointerPressedEventArgs Pointer { get; }
}
