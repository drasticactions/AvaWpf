// Ported from WPF $W/PresentationFramework/System/Windows/Automation/Peers/ToolBarAutomationPeer.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>
/// Exposes a <see cref="ToolBar"/> to UI Automation; its children are all items, overflow included, and the overflow is an
/// expand/collapse pattern.
/// </summary>
public class ToolBarAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Initializes a new instance of the <see cref="ToolBarAutomationPeer"/> class.</summary>
    /// <param name="owner">The tool bar.</param>
    public ToolBarAutomationPeer(ToolBar owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == ToolBar.IsOverflowOpenProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    ToState(e.GetOldValue<bool>()),
                    ToState(e.GetNewValue<bool>()));
                InvalidateChildren();
            }
        };
    }

    /// <summary>The tool bar.</summary>
    public new ToolBar Owner => (ToolBar)base.Owner;

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState =>
        !Owner.HasOverflowItems ? ExpandCollapseState.LeafNode : ToState(Owner.IsOverflowOpen);

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
    protected override string GetClassNameCore() => "ToolBar";

    /// <inheritdoc/>
    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name) ? Owner.Header as string : name;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => AutomationChildren.ForItems(Owner, this);

    private static ExpandCollapseState ToState(bool open) => open ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
}
