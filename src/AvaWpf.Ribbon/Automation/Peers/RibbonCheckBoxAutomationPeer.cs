// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonCheckBoxAutomationPeer.cs (MIT, see NOTICE.md).
using Avalonia.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonCheckBox"/> to UI Automation as a check box with the toggle pattern, named by its label,
/// with its KeyTip as the access key.
/// </summary>
public class RibbonCheckBoxAutomationPeer : ToggleButtonAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="RibbonCheckBoxAutomationPeer"/> class.</summary>
    /// <param name="owner">The check box.</param>
    public RibbonCheckBoxAutomationPeer(RibbonCheckBox owner)
        : base(owner)
    {
    }

    /// <summary>The check box.</summary>
    public new RibbonCheckBox Owner => (RibbonCheckBox)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonCheckBox";

    /// <inheritdoc/>
    protected override string? GetNameCore() => RibbonAutomation.Name(Owner, base.GetNameCore());

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());
}
