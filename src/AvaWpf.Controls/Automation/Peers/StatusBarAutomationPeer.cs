// Ported from WPF $W/PresentationFramework/System/Windows/Automation/Peers/StatusBarAutomationPeer.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>
/// Exposes a <see cref="StatusBar"/> to UI Automation; as in WPF, separators and text items are children and any other
/// item is replaced by the controls inside it.
/// </summary>
public class StatusBarAutomationPeer : ControlAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="StatusBarAutomationPeer"/> class.</summary>
    /// <param name="owner">The status bar.</param>
    public StatusBarAutomationPeer(StatusBar owner)
        : base(owner)
    {
        ((INotifyCollectionChanged)owner.ItemsView).CollectionChanged += (_, _) => InvalidateChildren();
    }

    /// <summary>The status bar.</summary>
    public new StatusBar Owner => (StatusBar)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.StatusBar;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "StatusBar";

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        var list = new List<AutomationPeer>();
        for (var i = 0; i < Owner.ItemCount; i++)
        {
            var item = Owner.ItemsView[i];
            if (item is Separator separator)
            {
                list.Add(GetOrCreate(separator));
            }
            else if (Owner.ContainerFromIndex(i) is StatusBarItem container)
            {
                if (item is string or TextBlock || container.Content is string)
                {
                    list.Add(GetOrCreate(container));
                }
                else
                {
                    AddDescendantPeers(container, list);
                }
            }
        }

        return list;
    }

    /// <summary>The peers of the nearest controls under <paramref name="parent"/> (WPF's <c>iterate</c>).</summary>
    private void AddDescendantPeers(Visual parent, List<AutomationPeer> list)
    {
        foreach (var child in parent.GetVisualChildren())
        {
            if (child is Control { IsVisible: true } control && GetOrCreate(control) is { } peer && (peer.IsControlElement() || peer.IsContentElement()))
            {
                list.Add(peer);
            }
            else
            {
                AddDescendantPeers(child, list);
            }
        }
    }
}
