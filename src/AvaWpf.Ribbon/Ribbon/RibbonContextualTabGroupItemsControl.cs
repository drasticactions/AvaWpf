// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonContextualTabGroupItemsControl.cs (MIT, see NOTICE.md).
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace AvaWpf.Ribbon;

/// <summary>
/// The row of contextual tab group titles above the tab row; in a <see cref="RibbonWindow"/> it sits in the caption.
/// </summary>
[PseudoClasses(":incaption")]
public class RibbonContextualTabGroupItemsControl : ItemsControl
{
    /// <summary>The Ribbon the row belongs to: its templated parent, or the Ribbon that has the window caption.</summary>
    public Ribbon? Ribbon => OwnerRibbon ?? TemplatedParent as Ribbon;

    /// <summary>Whether the row is in a window caption.</summary>
    public bool IsInCaption { get; private set; }

    /// <summary>The Ribbon of a row that is not in the Ribbon's template (the caption row).</summary>
    internal Ribbon? OwnerRibbon { get; set; }

    /// <summary>Marks the row as the caption row.</summary>
    internal void SetIsInCaption(bool value)
    {
        IsInCaption = value;
        PseudoClasses.Set(":incaption", value);
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<RibbonContextualTabGroup>(item, out recycleKey);

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new RibbonContextualTabGroup();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is RibbonContextualTabGroup group)
        {
            if (container != item)
            {
                group.Header = item;
            }

            group.SetIsInCaption(IsInCaption);
        }
    }

    /// <inheritdoc/>
    protected override void ContainerForItemPreparedOverride(Control container, object? item, int index)
    {
        base.ContainerForItemPreparedOverride(container, item, index);

        // The Ribbon matches its tabs to the realized group containers.
        Ribbon?.UpdateContextualTabGroups();
    }

    /// <inheritdoc/>
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is RibbonContextualTabGroup group)
        {
            group.SetIsInCaption(false);
        }
    }
}
