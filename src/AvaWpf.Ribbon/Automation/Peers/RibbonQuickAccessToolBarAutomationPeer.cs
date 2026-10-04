// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonQuickAccessToolBarAutomationPeer.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonQuickAccessToolBar"/> to UI Automation as a tool bar whose children are its controls
/// (overflow controls included). While controls overflow, the overflow is its expand/collapse pattern.
/// </summary>
public class RibbonQuickAccessToolBarAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonQuickAccessToolBarAutomationPeer"/> class.</summary>
    /// <param name="owner">The Quick Access Toolbar.</param>
    public RibbonQuickAccessToolBarAutomationPeer(RibbonQuickAccessToolBar owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == RibbonQuickAccessToolBar.IsOverflowOpenProperty || e.Property == RibbonQuickAccessToolBar.HasOverflowItemsProperty)
            {
                RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, null, ExpandCollapseState);
            }
        };
    }

    /// <summary>The Quick Access Toolbar.</summary>
    public new RibbonQuickAccessToolBar Owner => (RibbonQuickAccessToolBar)base.Owner;

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => !Owner.HasOverflowItems
        ? ExpandCollapseState.LeafNode
        : Owner.IsOverflowOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public bool ShowsMenu => true;

    /// <inheritdoc/>
    public void Expand()
    {
        EnsureEnabled();
        if (Owner.HasOverflowItems)
        {
            Owner.IsOverflowOpen = true;
        }
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        EnsureEnabled();
        Owner.IsOverflowOpen = false;
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonQuickAccessToolBar";

    /// <inheritdoc/>
    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name) ? RibbonStrings.QuickAccessToolBarName : name;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => AutomationChildren.ForItems(Owner, this);
}
