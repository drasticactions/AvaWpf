// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonTabAutomationPeer.cs and RibbonTabDataAutomationPeer.cs
// (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonTab"/> to UI Automation as a tab item named by its header, with its KeyTip as the access
/// key. Selecting it selects the tab in the Ribbon; its children are its groups.
/// </summary>
public class RibbonTabAutomationPeer : ControlAutomationPeer, ISelectionItemProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonTabAutomationPeer"/> class.</summary>
    /// <param name="owner">The tab.</param>
    public RibbonTabAutomationPeer(RibbonTab owner)
        : base(owner)
    {
    }

    /// <summary>The tab.</summary>
    public new RibbonTab Owner => (RibbonTab)base.Owner;

    /// <inheritdoc/>
    public bool IsSelected => Owner.IsSelected;

    /// <inheritdoc/>
    public ISelectionProvider? SelectionContainer =>
        Owner.Ribbon is { } ribbon ? GetOrCreate(ribbon).GetProvider<ISelectionProvider>() : null;

    /// <inheritdoc/>
    public void Select()
    {
        EnsureEnabled();
        if (Owner.Ribbon is { } ribbon && ribbon.IndexFromContainer(Owner) is var index and >= 0)
        {
            ribbon.SelectedIndex = index;
        }
    }

    /// <inheritdoc/>
    public void AddToSelection() => Select();

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        // A Ribbon always has a selected tab.
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonTab";

    /// <inheritdoc/>
    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name) ? Owner.Header?.ToString() : name;
    }

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => AutomationChildren.ForItems(Owner, this);
}
