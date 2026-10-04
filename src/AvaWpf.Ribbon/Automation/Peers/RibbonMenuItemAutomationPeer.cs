// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonMenuItemAutomationPeer.cs (MIT, see NOTICE.md).
using Avalonia.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonMenuItem"/> to UI Automation as a menu item (Avalonia's menu item peer), with its KeyTip
/// as the access key and its tool tip description as help text.
/// </summary>
public class RibbonMenuItemAutomationPeer : MenuItemAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="RibbonMenuItemAutomationPeer"/> class.</summary>
    /// <param name="owner">The menu item.</param>
    public RibbonMenuItemAutomationPeer(RibbonMenuItem owner)
        : base(owner)
    {
    }

    /// <summary>The menu item.</summary>
    public new RibbonMenuItem Owner => (RibbonMenuItem)base.Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => Owner.GetType().Name;

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());
}
