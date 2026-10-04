// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonRadioButtonAutomationPeer.cs (MIT, see NOTICE.md).
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonRadioButton"/> to UI Automation as a radio button (Avalonia's radio button peer), named by
/// its label, with its KeyTip as the access key.
/// </summary>
public class RibbonRadioButtonAutomationPeer : RadioButtonAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="RibbonRadioButtonAutomationPeer"/> class.</summary>
    /// <param name="owner">The radio button.</param>
    public RibbonRadioButtonAutomationPeer(RibbonRadioButton owner)
        : base(owner)
    {
    }

    /// <summary>The radio button.</summary>
    public new RibbonRadioButton Owner => (RibbonRadioButton)base.Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonRadioButton";

    /// <inheritdoc/>
    protected override string? GetNameCore() => RibbonAutomation.Name(Owner, base.GetNameCore());

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());
}
