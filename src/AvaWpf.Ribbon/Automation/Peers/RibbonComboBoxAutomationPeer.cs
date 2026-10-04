// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonComboBoxAutomationPeer.cs (MIT, see NOTICE.md).
using Avalonia.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonComboBox"/> to UI Automation as a combo box: a menu button peer (label, KeyTip,
/// expand/collapse for the drop-down) with the combo box control type.
/// </summary>
public class RibbonComboBoxAutomationPeer : RibbonMenuButtonAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="RibbonComboBoxAutomationPeer"/> class.</summary>
    /// <param name="owner">The combo box.</param>
    public RibbonComboBoxAutomationPeer(RibbonComboBox owner)
        : base(owner)
    {
    }

    /// <summary>The combo box.</summary>
    public new RibbonComboBox Owner => (RibbonComboBox)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;
}
