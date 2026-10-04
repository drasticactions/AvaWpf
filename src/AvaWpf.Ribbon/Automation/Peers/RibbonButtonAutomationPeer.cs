// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonButtonAutomationPeer.cs (MIT, see NOTICE.md).
using Avalonia.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonButton"/> to UI Automation as a button named by its label, with its KeyTip as the access
/// key and its tool tip description as help text.
/// </summary>
public class RibbonButtonAutomationPeer : ButtonAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="RibbonButtonAutomationPeer"/> class.</summary>
    /// <param name="owner">The button.</param>
    public RibbonButtonAutomationPeer(RibbonButton owner)
        : base(owner)
    {
    }

    /// <summary>The button.</summary>
    public new RibbonButton Owner => (RibbonButton)base.Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonButton";

    /// <inheritdoc/>
    protected override string? GetNameCore() => RibbonAutomation.Name(Owner, base.GetNameCore());

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());
}
