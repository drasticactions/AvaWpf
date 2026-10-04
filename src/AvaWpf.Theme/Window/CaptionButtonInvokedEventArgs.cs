using Avalonia.Interactivity;

namespace AvaWpf;

/// <summary>Raised by a <see cref="WindowFrame"/> when a caption button is clicked or the caption is double-clicked.</summary>
public sealed class CaptionButtonInvokedEventArgs : RoutedEventArgs
{
    /// <summary>Initializes the event arguments.</summary>
    public CaptionButtonInvokedEventArgs(RoutedEvent routedEvent, CaptionButton button) : base(routedEvent)
    {
        Button = button;
    }

    /// <summary>The button.</summary>
    public CaptionButton Button { get; }
}
