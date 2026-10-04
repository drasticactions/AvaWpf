// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonToggleButtonAutomationPeer.cs (MIT, see NOTICE.md).
using Avalonia.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonToggleButton"/> to UI Automation as a button with the toggle pattern, named by its label,
/// with its KeyTip as the access key.
/// </summary>
public class RibbonToggleButtonAutomationPeer : ToggleButtonAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="RibbonToggleButtonAutomationPeer"/> class.</summary>
    /// <param name="owner">The toggle button.</param>
    public RibbonToggleButtonAutomationPeer(RibbonToggleButton owner)
        : base(owner)
    {
    }

    /// <summary>The toggle button.</summary>
    public new RibbonToggleButton Owner => (RibbonToggleButton)base.Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonToggleButton";

    /// <inheritdoc/>
    protected override string? GetNameCore() => RibbonAutomation.Name(Owner, base.GetNameCore());

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());
}
