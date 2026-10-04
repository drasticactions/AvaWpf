// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonGroupAutomationPeer.cs and RibbonGroupDataAutomationPeer.cs
// (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonGroup"/> to UI Automation as a group named by its header, with its controls as children.
/// A collapsed group is a drop-down: its expand/collapse pattern opens it (a leaf while the group is not collapsed).
/// </summary>
public class RibbonGroupAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonGroupAutomationPeer"/> class.</summary>
    /// <param name="owner">The group.</param>
    public RibbonGroupAutomationPeer(RibbonGroup owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == RibbonGroup.IsDropDownOpenProperty || e.Property == RibbonGroup.IsCollapsedProperty)
            {
                RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, null, ExpandCollapseState);
            }
        };
    }

    /// <summary>The group.</summary>
    public new RibbonGroup Owner => (RibbonGroup)base.Owner;

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => !Owner.IsCollapsed
        ? ExpandCollapseState.LeafNode
        : Owner.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public bool ShowsMenu => false;

    /// <inheritdoc/>
    public void Expand()
    {
        EnsureEnabled();
        if (Owner.IsCollapsed)
        {
            Owner.IsDropDownOpen = true;
        }
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        EnsureEnabled();
        Owner.IsDropDownOpen = false;
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonGroup";

    /// <inheritdoc/>
    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name) ? Owner.Header?.ToString() : name;
    }

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => AutomationChildren.ForItems(Owner, this);
}
