using Avalonia.Automation.Peers;

namespace AvaWpf.Automation.Peers;

/// <summary>
/// Exposes a <see cref="ThemeWindow"/> to UI Automation as a window named by its title; the title bar comes from the
/// <see cref="WindowFrameAutomationPeer"/> inside it.
/// </summary>
public class ThemeWindowAutomationPeer : WindowAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="ThemeWindowAutomationPeer"/> class.</summary>
    /// <param name="owner">The window.</param>
    public ThemeWindowAutomationPeer(ThemeWindow owner)
        : base(owner)
    {
    }

    /// <summary>The window.</summary>
    public new ThemeWindow Owner => (ThemeWindow)base.Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "ThemeWindow";
}
