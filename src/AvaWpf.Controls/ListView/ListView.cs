// Ported from WPF $W/PresentationFramework/System/Windows/Controls/ListView.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Automation.Peers;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Controls;

/// <summary>
/// A list whose items are laid out by a <see cref="View"/>; with a <see cref="GridView"/> it has the <c>:gridview</c>
/// pseudo-class.
/// </summary>
/// <remarks>The default <see cref="Avalonia.Controls.Primitives.SelectingItemsControl.SelectionMode"/> selects several items with Ctrl and Shift, as in WPF.</remarks>
[PseudoClasses(PcGridView)]
public class ListView : ListBox
{
    /// <summary>Defines the <see cref="View"/> property.</summary>
    public static readonly StyledProperty<ViewBase?> ViewProperty =
        AvaloniaProperty.Register<ListView, ViewBase?>(nameof(View));

    private const string PcGridView = ":gridview";

    static ListView()
    {
        SelectionModeProperty.OverrideDefaultValue<ListView>(SelectionMode.Multiple);
    }

    /// <summary>Initializes a new instance of the <see cref="ListView"/> class.</summary>
    public ListView()
    {
        UpdatePseudoClasses();
    }

    /// <summary>Raised after <see cref="View"/> changed and the item containers were prepared for the new view.</summary>
    internal event EventHandler? ViewChanged;

    /// <summary>The view that lays out the items; one list at a time can use a view.</summary>
    public ViewBase? View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<ListViewItem>(item, out recycleKey);

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new ListViewItem();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is ListViewItem lvi)
        {
            PrepareForView(lvi, View);
            lvi.SetIsSelectionActive(IsKeyboardFocusWithin);
        }
    }

    /// <inheritdoc/>
    protected override void ClearContainerForItemOverride(Control container)
    {
        if (container is ListViewItem lvi)
        {
            View?.ClearItem(lvi);
            lvi.SetIsGridView(false);
        }

        base.ClearContainerForItemOverride(container);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ViewProperty && change.GetNewValue<ViewBase?>() is { IsUsed: true } shared &&
            !ReferenceEquals(shared, change.GetOldValue<ViewBase?>()))
        {
            throw new InvalidOperationException("A ListView view can be used by one ListView at a time.");
        }

        base.OnPropertyChanged(change);
        if (change.Property == ViewProperty)
        {
            var oldView = change.GetOldValue<ViewBase?>();
            var newView = change.GetNewValue<ViewBase?>();
            if (oldView is not null)
            {
                oldView.IsUsed = false;
            }

            if (newView is not null)
            {
                newView.IsUsed = true;
            }

            foreach (var c in GetRealizedContainers())
            {
                if (c is ListViewItem lvi)
                {
                    oldView?.ClearItem(lvi);
                    PrepareForView(lvi, newView);
                }
            }

            UpdatePseudoClasses();
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (change.Property == IsKeyboardFocusWithinProperty)
        {
            // WPF's Selector.IsSelectionActive: selected items look active while the list has the focus.
            foreach (var c in GetRealizedContainers())
            {
                if (c is ListViewItem lvi)
                {
                    lvi.SetIsSelectionActive(IsKeyboardFocusWithin);
                }
            }
        }
    }

    private static void PrepareForView(ListViewItem item, ViewBase? view)
    {
        view?.PrepareItem(item);
        item.SetIsGridView(view is GridView);
    }

    private void UpdatePseudoClasses() => PseudoClasses.Set(PcGridView, View is GridView);

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new ListViewAutomationPeer(this);
}
