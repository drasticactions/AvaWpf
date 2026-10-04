// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonDismissPopupEventArgs.cs (MIT, see NOTICE.md).
using Avalonia.Interactivity;

namespace AvaWpf.Ribbon;

/// <summary>The data of <see cref="RibbonControlService.DismissPopupEvent"/>.</summary>
public class RibbonDismissPopupEventArgs : RoutedEventArgs
{
    /// <summary>Initializes the arguments with <see cref="RibbonDismissPopupMode.Always"/>.</summary>
    public RibbonDismissPopupEventArgs()
        : this(RibbonDismissPopupMode.Always)
    {
    }

    /// <summary>Initializes the arguments.</summary>
    /// <param name="dismissMode">Which popups close.</param>
    public RibbonDismissPopupEventArgs(RibbonDismissPopupMode dismissMode)
        : base(RibbonControlService.DismissPopupEvent)
    {
        DismissMode = dismissMode;
    }

    /// <summary>Which popups close.</summary>
    public RibbonDismissPopupMode DismissMode { get; }
}
