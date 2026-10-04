// Ported from WPF $W/PresentationFramework/System/Windows/Automation/Peers/GridViewItemAutomationPeer.cs (MIT, see
// NOTICE.md).
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>
/// Exposes a <see cref="ListViewItem"/> to UI Automation: in a <see cref="GridView"/> a data item whose children are its
/// cells and whose default name is its first cell, otherwise a list item.
/// </summary>
public class ListViewItemAutomationPeer : ListItemAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="ListViewItemAutomationPeer"/> class.</summary>
    /// <param name="owner">The list view item.</param>
    public ListViewItemAutomationPeer(ListViewItem owner)
        : base(owner)
    {
    }

    /// <summary>The list view item.</summary>
    public new ListViewItem Owner => (ListViewItem)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() =>
        Owner.IsGridView ? AutomationControlType.DataItem : AutomationControlType.ListItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "ListViewItem";

    /// <inheritdoc/>
    protected override string? GetNameCore()
    {
        var name = Avalonia.Automation.AutomationProperties.GetName(Owner);
        if (string.IsNullOrWhiteSpace(name) && Owner.IsGridView && Cells() is { Count: > 0 } cells)
        {
            name = GetOrCreate(cells[0]).GetName();
        }

        return string.IsNullOrWhiteSpace(name) ? base.GetNameCore() : name;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        if (!Owner.IsGridView || Cells() is not { } cells)
        {
            return base.GetChildrenCore();
        }

        var children = new List<AutomationPeer>(cells.Count);
        foreach (var cell in cells)
        {
            children.Add(GetOrCreate(cell));
        }

        return children;
    }

    private IReadOnlyList<Control>? Cells() => Owner.GetVisualDescendants().OfType<GridViewRowPresenter>().FirstOrDefault()?.Cells;
}
