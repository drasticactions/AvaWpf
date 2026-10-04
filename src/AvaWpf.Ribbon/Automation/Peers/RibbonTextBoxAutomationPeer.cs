// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonTextBoxAutomationPeer.cs (MIT, see NOTICE.md).
using Avalonia.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonTextBox"/> to UI Automation as an edit control (Avalonia's text box peer), named by its
/// label, with its KeyTip as the access key.
/// </summary>
public class RibbonTextBoxAutomationPeer : TextBoxAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="RibbonTextBoxAutomationPeer"/> class.</summary>
    /// <param name="owner">The text box.</param>
    public RibbonTextBoxAutomationPeer(RibbonTextBox owner)
        : base(owner)
    {
    }

    /// <summary>The text box.</summary>
    public new RibbonTextBox Owner => (RibbonTextBox)base.Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonTextBox";

    /// <inheritdoc/>
    protected override string? GetNameCore() => RibbonAutomation.Name(Owner, base.GetNameCore());

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());
}
