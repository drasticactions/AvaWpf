// Ported from WPF $W/PresentationFramework/System/Windows/Controls/ListViewItem.cs (MIT, see NOTICE.md).
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Automation.Peers;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Controls;

/// <summary>
/// An item of a <see cref="ListView"/>, with <c>:gridview</c> in a <see cref="GridView"/> and <c>:selectionactive</c>
/// while the list has the keyboard focus.
/// </summary>
[PseudoClasses(PcGridView, PcSelectionActive)]
public class ListViewItem : ListBoxItem
{
    private const string PcGridView = ":gridview";
    private const string PcSelectionActive = ":selectionactive";

    private bool _isGridView;
    private bool _isSelectionActive;

    /// <summary>Whether the item is shown by a <see cref="GridView"/> (a row of cells).</summary>
    internal bool IsGridView => _isGridView;

    /// <summary>Sets whether the item is shown by a <see cref="GridView"/>.</summary>
    /// <param name="value">True for a grid view row.</param>
    internal void SetIsGridView(bool value)
    {
        _isGridView = value;
        UpdatePseudoClasses();
    }

    /// <summary>Sets whether the list holding the item has the keyboard focus.</summary>
    /// <param name="value">True while the list has the focus.</param>
    internal void SetIsSelectionActive(bool value)
    {
        _isSelectionActive = value;
        UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(PcGridView, _isGridView);
        PseudoClasses.Set(PcSelectionActive, _isSelectionActive);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new ListViewItemAutomationPeer(this);
}
