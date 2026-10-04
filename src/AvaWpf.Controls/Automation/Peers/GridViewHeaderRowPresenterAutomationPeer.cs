// Ported from WPF $W/PresentationFramework/System/Windows/Automation/Peers/GridViewHeaderRowPresenterAutomationPeer.cs
// (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.VisualTree;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>
/// Exposes a <see cref="GridViewHeaderRowPresenter"/> to UI Automation as a header whose children are the column headers,
/// left to right, without the padding and drag headers.
/// </summary>
public class GridViewHeaderRowPresenterAutomationPeer : ControlAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="GridViewHeaderRowPresenterAutomationPeer"/> class.</summary>
    /// <param name="owner">The header row presenter.</param>
    public GridViewHeaderRowPresenterAutomationPeer(GridViewHeaderRowPresenter owner)
        : base(owner)
    {
    }

    /// <summary>The header row presenter.</summary>
    public new GridViewHeaderRowPresenter Owner => (GridViewHeaderRowPresenter)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Header;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "GridViewHeaderRowPresenter";

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => false;

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        var headers = new List<GridViewColumnHeader>();
        foreach (var child in Owner.GetVisualChildren())
        {
            if (child is GridViewColumnHeader { Role: GridViewColumnHeaderRole.Normal, Column: not null, IsVisible: true } header)
            {
                headers.Add(header);
            }
        }

        // The presenter keeps its headers in z-order, not column order.
        var columns = Owner.Columns;
        headers.Sort((a, b) => (columns?.IndexOf(a.Column!) ?? 0).CompareTo(columns?.IndexOf(b.Column!) ?? 0));
        var children = new List<AutomationPeer>(headers.Count);
        foreach (var header in headers)
        {
            children.Add(GetOrCreate(header));
        }

        return children;
    }
}
