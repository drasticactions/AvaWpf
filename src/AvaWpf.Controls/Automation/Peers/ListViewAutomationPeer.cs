// Ported from WPF $W/PresentationFramework/System/Windows/Automation/Peers/ListViewAutomationPeer.cs and
// GridViewAutomationPeer.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.VisualTree;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>
/// Exposes a <see cref="ListView"/> to UI Automation: with a <see cref="GridView"/> a data grid (header row, then one
/// data item per realized row), otherwise a list. Avalonia has no grid pattern, so cells are children of their row.
/// </summary>
public class ListViewAutomationPeer : ListBoxAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="ListViewAutomationPeer"/> class.</summary>
    /// <param name="owner">The list view.</param>
    public ListViewAutomationPeer(ListView owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == ListView.ViewProperty)
            {
                InvalidateChildren();
            }
        };
    }

    /// <summary>The list view.</summary>
    public new ListView Owner => (ListView)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() =>
        Owner.View is GridView ? AutomationControlType.DataGrid : AutomationControlType.List;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "ListView";

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        if (Owner.View is not GridView)
        {
            return base.GetChildrenCore();
        }

        var children = new List<AutomationPeer>();
        if (Owner.GetVisualDescendants().OfType<GridViewHeaderRowPresenter>().FirstOrDefault() is { IsVisible: true } header)
        {
            children.Add(GetOrCreate(header));
        }

        children.AddRange(AutomationChildren.ForItems(Owner, this));
        return children;
    }
}
