// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonAutomationPeer.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="Ribbon"/> to UI Automation as a tab control, as WPF does: its children are the application menu,
/// the Quick Access Toolbar and the tabs; the selected tab is its selection; minimizing the Ribbon is its
/// expand/collapse pattern (collapsed while minimized).
/// </summary>
public class RibbonAutomationPeer : SelectingItemsControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonAutomationPeer"/> class.</summary>
    /// <param name="owner">The Ribbon.</param>
    public RibbonAutomationPeer(Ribbon owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == Ribbon.IsMinimizedProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    e.GetOldValue<bool>() ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded,
                    e.GetNewValue<bool>() ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded);
            }
            else if (e.Property == Ribbon.ApplicationMenuProperty || e.Property == Ribbon.QuickAccessToolBarProperty)
            {
                InvalidateChildren();
            }
        };
    }

    /// <summary>The Ribbon.</summary>
    public new Ribbon Owner => (Ribbon)base.Owner;

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => Owner.IsMinimized ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded;

    /// <inheritdoc/>
    public bool ShowsMenu => false;

    /// <inheritdoc/>
    public void Expand()
    {
        EnsureEnabled();
        Owner.IsMinimized = false;
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        EnsureEnabled();
        Owner.IsMinimized = true;
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "Ribbon";

    /// <inheritdoc/>
    protected override bool IsOffscreenCore() => Owner.IsCollapsed || base.IsOffscreenCore();

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        var children = new List<AutomationPeer>();
        if (Owner.ApplicationMenu is { IsVisible: true } menu)
        {
            children.Add(GetOrCreate(menu));
        }

        if (Owner.QuickAccessToolBar is { IsVisible: true } qat)
        {
            children.Add(GetOrCreate(qat));
        }

        for (var i = 0; i < Owner.ItemCount; i++)
        {
            if ((Owner.ContainerFromIndex(i) ?? Owner.ItemsView[i] as Control) is RibbonTab { IsTabVisible: true } tab)
            {
                children.Add(GetOrCreate(tab));
            }
        }

        return children;
    }
}
