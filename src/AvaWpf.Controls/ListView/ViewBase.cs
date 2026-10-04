// Ported from WPF $W/PresentationFramework/System/Windows/Controls/ViewBase.cs (MIT, see NOTICE.md).
using Avalonia;

namespace AvaWpf.Controls;

/// <summary>The base of a <see cref="ListView.View"/>, which prepares each <see cref="ListViewItem"/>.</summary>
public abstract class ViewBase : AvaloniaObject
{
    /// <summary>Whether a <see cref="ListView"/> uses the view.</summary>
    internal bool IsUsed { get; set; }

    /// <summary>Prepares an item container of the list for this view.</summary>
    /// <param name="item">The container.</param>
    protected internal virtual void PrepareItem(ListViewItem item)
    {
    }

    /// <summary>Undoes <see cref="PrepareItem"/> on an item container.</summary>
    /// <param name="item">The container.</param>
    protected internal virtual void ClearItem(ListViewItem item)
    {
    }
}
