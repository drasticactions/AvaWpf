using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>Builds automation children from the items of an items control instead of its visual tree.</summary>
internal static class AutomationChildren
{
    /// <summary>
    /// The peers of the items of <paramref name="owner"/> in item order, including items in a closed overflow or drop-down
    /// but not items a virtualizing panel has not realized.
    /// </summary>
    /// <param name="owner">The items control.</param>
    /// <param name="peer">The peer of <paramref name="owner"/>.</param>
    /// <returns>The children.</returns>
    public static List<AutomationPeer> ForItems(ItemsControl owner, ControlAutomationPeer peer)
    {
        var result = new List<AutomationPeer>(owner.ItemCount);
        for (var i = 0; i < owner.ItemCount; i++)
        {
            if ((owner.ContainerFromIndex(i) ?? owner.ItemsView[i] as Control) is { IsVisible: true } control)
            {
                result.Add(peer.GetOrCreate(control));
            }
        }

        return result;
    }
}
