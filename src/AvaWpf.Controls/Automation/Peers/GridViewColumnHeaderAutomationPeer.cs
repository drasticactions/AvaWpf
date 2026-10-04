// Ported from WPF $W/PresentationFramework/System/Windows/Automation/Peers/GridViewColumnHeaderAutomationPeer.cs (MIT,
// see NOTICE.md).
using Avalonia.Automation.Peers;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>
/// Exposes a <see cref="GridViewColumnHeader"/> to UI Automation as an invokable header item; WPF's transform pattern has
/// no Avalonia counterpart.
/// </summary>
public class GridViewColumnHeaderAutomationPeer : ButtonAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="GridViewColumnHeaderAutomationPeer"/> class.</summary>
    /// <param name="owner">The column header.</param>
    public GridViewColumnHeaderAutomationPeer(GridViewColumnHeader owner)
        : base(owner)
    {
    }

    /// <summary>The column header.</summary>
    public new GridViewColumnHeader Owner => (GridViewColumnHeader)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.HeaderItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "GridViewColumnHeader";

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => false;
}
