// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonQuickAccessToolBarCloneEventArgs.cs (MIT, see NOTICE.md).
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaWpf.Ribbon;

/// <summary>
/// The data of <see cref="RibbonQuickAccessToolBar.CloneEvent"/>; set <see cref="CloneInstance"/> to supply the copy.
/// </summary>
public class RibbonQuickAccessToolBarCloneEventArgs : RoutedEventArgs
{
    /// <summary>Initializes the arguments.</summary>
    /// <param name="instanceToBeCloned">The control being added.</param>
    public RibbonQuickAccessToolBarCloneEventArgs(Control instanceToBeCloned)
        : base(RibbonQuickAccessToolBar.CloneEvent)
    {
        InstanceToBeCloned = instanceToBeCloned;
    }

    /// <summary>The control being added.</summary>
    public Control InstanceToBeCloned { get; }

    /// <summary>The copy that goes into the toolbar.</summary>
    public Control? CloneInstance { get; set; }
}
