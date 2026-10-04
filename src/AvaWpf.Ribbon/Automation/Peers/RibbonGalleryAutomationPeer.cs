// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonGalleryAutomationPeer.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonGallery"/> to UI Automation as a list with single selection. Its children are the gallery
/// items of all its categories, in order (the categories are layout, so the list is flat).
/// </summary>
public class RibbonGalleryAutomationPeer : ControlAutomationPeer, ISelectionProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonGalleryAutomationPeer"/> class.</summary>
    /// <param name="owner">The gallery.</param>
    public RibbonGalleryAutomationPeer(RibbonGallery owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == RibbonGallery.SelectedItemProperty)
            {
                RaisePropertyChangedEvent(SelectionPatternIdentifiers.SelectionProperty, null, null);
            }
        };
    }

    /// <summary>The gallery.</summary>
    public new RibbonGallery Owner => (RibbonGallery)base.Owner;

    /// <inheritdoc/>
    public bool CanSelectMultiple => false;

    /// <inheritdoc/>
    public bool IsSelectionRequired => false;

    /// <inheritdoc/>
    public IReadOnlyList<AutomationPeer> GetSelection()
    {
        foreach (var item in Items())
        {
            if (item.IsSelected)
            {
                return [GetOrCreate(item)];
            }
        }

        return Array.Empty<AutomationPeer>();
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonGallery";

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        var children = new List<AutomationPeer>();
        foreach (var item in Items())
        {
            if (item.IsVisible)
            {
                children.Add(GetOrCreate(item));
            }
        }

        return children;
    }

    private IEnumerable<RibbonGalleryItem> Items()
    {
        for (var i = 0; i < Owner.ItemCount; i++)
        {
            if ((Owner.ContainerFromIndex(i) ?? Owner.ItemsView[i] as Control) is not RibbonGalleryCategory { IsVisible: true } category)
            {
                continue;
            }

            for (var j = 0; j < category.ItemCount; j++)
            {
                if ((category.ContainerFromIndex(j) ?? category.ItemsView[j] as Control) is RibbonGalleryItem item)
                {
                    yield return item;
                }
            }
        }
    }
}
