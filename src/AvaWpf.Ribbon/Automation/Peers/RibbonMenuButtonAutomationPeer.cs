// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonMenuButtonAutomationPeer.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonMenuButton"/> to UI Automation as a menu item that opens a menu: named by its label, with
/// its KeyTip as the access key, the drop-down as the expand/collapse pattern and the menu entries as children.
/// </summary>
public class RibbonMenuButtonAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonMenuButtonAutomationPeer"/> class.</summary>
    /// <param name="owner">The menu button.</param>
    public RibbonMenuButtonAutomationPeer(RibbonMenuButton owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == RibbonMenuButton.IsDropDownOpenProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    e.GetOldValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                    e.GetNewValue<bool>() ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
            }
        };
    }

    /// <summary>The menu button.</summary>
    public new RibbonMenuButton Owner => (RibbonMenuButton)base.Owner;

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => Owner.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public bool ShowsMenu => true;

    /// <inheritdoc/>
    public void Expand()
    {
        EnsureEnabled();
        Owner.IsDropDownOpen = true;
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        EnsureEnabled();
        Owner.IsDropDownOpen = false;
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.MenuItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => Owner.GetType().Name;

    /// <inheritdoc/>
    protected override string? GetNameCore() => RibbonAutomation.Name(Owner, base.GetNameCore());

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => AutomationChildren.ForItems(Owner, this);
}
