// Ported from WPF $W/PresentationFramework/System/Windows/Controls/GridView.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace AvaWpf.Controls;

/// <summary>
/// A <see cref="ListView"/> view that shows the items as rows of cells under a header row, with columns shared by every
/// row.
/// </summary>
public class GridView : ViewBase
{
    /// <summary>Defines the <see cref="ColumnHeaderTheme"/> property.</summary>
    public static readonly StyledProperty<ControlTheme?> ColumnHeaderThemeProperty =
        AvaloniaProperty.Register<GridView, ControlTheme?>(nameof(ColumnHeaderTheme));

    /// <summary>Defines the <see cref="ColumnHeaderTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> ColumnHeaderTemplateProperty =
        AvaloniaProperty.Register<GridView, IDataTemplate?>(nameof(ColumnHeaderTemplate));

    /// <summary>Defines the <see cref="ColumnHeaderContextMenu"/> property.</summary>
    public static readonly StyledProperty<ContextMenu?> ColumnHeaderContextMenuProperty =
        AvaloniaProperty.Register<GridView, ContextMenu?>(nameof(ColumnHeaderContextMenu));

    /// <summary>Defines the <see cref="ColumnHeaderToolTip"/> property.</summary>
    public static readonly StyledProperty<object?> ColumnHeaderToolTipProperty =
        AvaloniaProperty.Register<GridView, object?>(nameof(ColumnHeaderToolTip));

    /// <summary>Defines the <see cref="AllowsColumnReorder"/> property.</summary>
    public static readonly StyledProperty<bool> AllowsColumnReorderProperty =
        AvaloniaProperty.Register<GridView, bool>(nameof(AllowsColumnReorder), true);

    /// <summary>
    /// Defines the <c>GridView.ColumnCollection</c> attached property: the columns a <see cref="GridViewRowPresenter"/> in
    /// the item template lays out.
    /// </summary>
    public static readonly AttachedProperty<GridViewColumnCollection?> ColumnCollectionProperty =
        AvaloniaProperty.RegisterAttached<GridView, Control, GridViewColumnCollection?>("ColumnCollection");

    /// <summary>The columns.</summary>
    [Content]
    public GridViewColumnCollection Columns { get; } = new();

    /// <summary>The theme of the column headers (WPF's <c>ColumnHeaderContainerStyle</c>); null uses the family theme.</summary>
    public ControlTheme? ColumnHeaderTheme
    {
        get => GetValue(ColumnHeaderThemeProperty);
        set => SetValue(ColumnHeaderThemeProperty, value);
    }

    /// <summary>The template of the header content of the columns that have no <see cref="GridViewColumn.HeaderTemplate"/>.</summary>
    public IDataTemplate? ColumnHeaderTemplate
    {
        get => GetValue(ColumnHeaderTemplateProperty);
        set => SetValue(ColumnHeaderTemplateProperty, value);
    }

    /// <summary>The context menu of the header row.</summary>
    public ContextMenu? ColumnHeaderContextMenu
    {
        get => GetValue(ColumnHeaderContextMenuProperty);
        set => SetValue(ColumnHeaderContextMenuProperty, value);
    }

    /// <summary>The tooltip of the column headers.</summary>
    public object? ColumnHeaderToolTip
    {
        get => GetValue(ColumnHeaderToolTipProperty);
        set => SetValue(ColumnHeaderToolTipProperty, value);
    }

    /// <summary>Whether the user can move a column by dragging its header. Default true.</summary>
    public bool AllowsColumnReorder
    {
        get => GetValue(AllowsColumnReorderProperty);
        set => SetValue(AllowsColumnReorderProperty, value);
    }

    /// <summary>The header row of the list that uses the view, or null.</summary>
    internal GridViewHeaderRowPresenter? HeaderRowPresenter { get; set; }

    /// <summary>Gets the columns of an item container.</summary>
    /// <param name="element">The container.</param>
    /// <returns>The columns, or null.</returns>
    public static GridViewColumnCollection? GetColumnCollection(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(ColumnCollectionProperty);
    }

    /// <summary>Sets the columns of an item container.</summary>
    /// <param name="element">The container.</param>
    /// <param name="collection">The columns.</param>
    public static void SetColumnCollection(Control element, GridViewColumnCollection? collection)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ColumnCollectionProperty, collection);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} Columns.Count:{Columns.Count}";

    /// <summary>Gives the item container the columns of the view.</summary>
    /// <param name="item">The container.</param>
    protected internal override void PrepareItem(ListViewItem item)
    {
        base.PrepareItem(item);
        SetColumnCollection(item, Columns);
    }

    /// <summary>Removes the columns from the item container.</summary>
    /// <param name="item">The container.</param>
    protected internal override void ClearItem(ListViewItem item)
    {
        item.ClearValue(ColumnCollectionProperty);
        base.ClearItem(item);
    }
}
