// Ported from WPF $W/PresentationFramework/System/Windows/Controls/GridViewRowPresenter.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Layout;

namespace AvaWpf.Controls;

/// <summary>
/// Lays out the cells of one <see cref="ListView"/> row to the widths of the shared <see cref="GridViewColumn"/>s.
/// </summary>
/// <remarks>
/// Unless set locally, <see cref="GridViewRowPresenterBase.Columns"/> and <see cref="Content"/> come from the item.
/// Cells of removed columns are reused for added columns.
/// </remarks>
public class GridViewRowPresenter : GridViewRowPresenterBase
{
    /// <summary>Defines the <see cref="Content"/> property.</summary>
    public static readonly StyledProperty<object?> ContentProperty =
        ContentControl.ContentProperty.AddOwner<GridViewRowPresenter>();

    private static readonly Thickness s_cellMargin = new(6, 0, 6, 0);

    private readonly List<Control> _cells = new();
    private readonly Stack<TextBlock> _recycledText = new();
    private readonly Stack<ContentPresenter> _recycledPresenters = new();
    private IDisposable? _columnsBinding;
    private IDisposable? _contentBinding;
    private bool _layoutUpdatedHooked;

    static GridViewRowPresenter()
    {
        AffectsMeasure<GridViewRowPresenter>(ContentProperty);
    }

    /// <summary>The row item, the data of every cell.</summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>The cells, in column order.</summary>
    internal IReadOnlyList<Control> Cells => _cells;

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} Content:{Content} Columns.Count:{Columns?.Count ?? 0}";

    /// <inheritdoc/>
    internal override void BuildChildren()
    {
        while (_cells.Count > 0)
        {
            RemoveCellAt(_cells.Count - 1);
        }

        if (Columns is { } columns)
        {
            foreach (var column in columns)
            {
                InsertCell(_cells.Count, column);
            }
        }
    }

    /// <inheritdoc/>
    internal override void OnColumnPropertyChanged(GridViewColumn column, string propertyName)
    {
        var index = Columns?.IndexOf(column) ?? -1;
        if (index < 0 || index >= _cells.Count)
        {
            return;
        }

        if (propertyName == nameof(GridViewColumn.Width) || propertyName == nameof(GridViewColumn.ActualWidth))
        {
            // Every row follows a column resize at once.
            InvalidateMeasure();
        }
        else if (propertyName == nameof(GridViewColumn.DisplayMemberBinding))
        {
            if (column.DisplayMemberBinding is { } binding && _cells[index] is TextBlock text)
            {
                text.Bind(TextBlock.TextProperty, binding);
            }
            else
            {
                RemoveCellAt(index);
                InsertCell(index, column);
            }
        }
        else if (propertyName is nameof(GridViewColumn.CellTemplate) or nameof(GridViewColumn.CellTemplateSelector))
        {
            if (_cells[index] is ContentPresenter cp)
            {
                cp.ContentTemplate = column.CellTemplate ?? column.CellTemplateSelector;
            }
        }
    }

    /// <inheritdoc/>
    internal override void OnColumnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnColumnCollectionChanged(e);
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Move:
                var moved = _cells[e.OldStartingIndex];
                _cells.RemoveAt(e.OldStartingIndex);
                _cells.Insert(e.NewStartingIndex, moved);
                InvalidateArrange();
                return;
            case NotifyCollectionChangedAction.Add:
                InsertCell(e.NewStartingIndex, (GridViewColumn)e.NewItems![0]!);
                break;
            case NotifyCollectionChangedAction.Remove:
                RemoveCellAt(e.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                RemoveCellAt(e.OldStartingIndex);
                InsertCell(e.NewStartingIndex, (GridViewColumn)e.NewItems![0]!);
                break;
            case NotifyCollectionChangedAction.Reset:
                BuildChildren();
                break;
        }

        InvalidateMeasure();
    }

    /// <summary>
    /// Measures the cells: a cell of an unfixed auto-size column widens the column, the others get the column width.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Columns is not { } columns)
        {
            return default;
        }

        double maxHeight = 0.0;
        double accumulatedWidth = 0.0;
        for (var i = 0; i < columns.Count && i < _cells.Count; i++)
        {
            var column = columns[i];
            var child = _cells[i];
            var childConstraintWidth = Math.Max(0.0, availableSize.Width - accumulatedWidth);
            if (column.State is ColumnMeasureState.Init or ColumnMeasureState.Headered)
            {
                HookLayoutUpdated();
                child.Measure(new Size(childConstraintWidth, availableSize.Height));
                column.EnsureWidth(child.DesiredSize.Width);
                DesiredWidthList[column] = column.DesiredWidth;
                accumulatedWidth += column.DesiredWidth;
            }
            else if (column.State == ColumnMeasureState.Data)
            {
                child.Measure(new Size(Math.Min(childConstraintWidth, column.DesiredWidth), availableSize.Height));
                accumulatedWidth += column.DesiredWidth;
            }
            else
            {
                child.Measure(new Size(Math.Min(childConstraintWidth, column.Width), availableSize.Height));
                accumulatedWidth += column.Width;
            }

            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        accumulatedWidth += PaddingHeaderMinWidth;
        return new Size(accumulatedWidth, maxHeight);
    }

    /// <summary>Arranges the cells side by side at the column widths, clipped to the row.</summary>
    /// <param name="finalSize">The final size.</param>
    /// <returns>The size used.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Columns is not { } columns)
        {
            return finalSize;
        }

        double accumulatedWidth = 0.0;
        var remainingWidth = finalSize.Width;
        for (var i = 0; i < columns.Count && i < _cells.Count; i++)
        {
            var column = columns[i];
            var width = Math.Min(remainingWidth, column.State == ColumnMeasureState.SpecificWidth ? column.Width : column.DesiredWidth);
            width = Math.Max(0.0, width);
            _cells[i].Arrange(new Rect(accumulatedWidth, 0, width, finalSize.Height));
            remainingWidth -= width;
            accumulatedWidth += width;
        }

        return finalSize;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
        {
            UpdateCells();
        }
        else if (change.Property == TemplatedParentProperty)
        {
            // In an item template, follow the item's columns and content unless they are set otherwise.
            _columnsBinding?.Dispose();
            _contentBinding?.Dispose();
            _columnsBinding = null;
            _contentBinding = null;
            if (change.NewValue is Control parent)
            {
                _columnsBinding = Bind(ColumnsProperty, parent.GetObservable(GridView.ColumnCollectionProperty), BindingPriority.Template);
                if (parent is ContentControl cc)
                {
                    _contentBinding = Bind(ContentProperty, cc.GetObservable(ContentControl.ContentProperty), BindingPriority.Template);
                }
            }
        }
    }

    private void HookLayoutUpdated()
    {
        if (!_layoutUpdatedHooked)
        {
            _layoutUpdatedHooked = true;
            LayoutUpdated += OnLayoutUpdated;
        }
    }

    /// <summary>
    /// After layout, fixes the auto-size columns this row measured and remeasures when another row widened one.
    /// </summary>
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        LayoutUpdated -= OnLayoutUpdated;
        _layoutUpdatedHooked = false;
        var desiredWidthChanged = false;
        if (Columns is { } columns)
        {
            foreach (var column in columns)
            {
                if (column.State == ColumnMeasureState.SpecificWidth)
                {
                    continue;
                }

                column.State = ColumnMeasureState.Data;
                if (!DesiredWidthList.TryGetValue(column, out var seen) || !AreClose(seen, column.DesiredWidth))
                {
                    DesiredWidthList[column] = column.DesiredWidth;
                    desiredWidthChanged = true;
                }
            }
        }

        if (desiredWidthChanged)
        {
            InvalidateMeasure();
        }
    }

    private void InsertCell(int index, GridViewColumn column)
    {
        Control cell;
        if (column.DisplayMemberBinding is { } binding)
        {
            var text = _recycledText.Count > 0 ? _recycledText.Pop() : new TextBlock();
            text.DataContext = Content;
            text.Bind(TextBlock.TextProperty, binding);
            cell = text;
        }
        else
        {
            var cp = _recycledPresenters.Count > 0 ? _recycledPresenters.Pop() : new ContentPresenter();
            SetCellContent(cp, column);
            cell = cp;
        }

        if (TemplatedParent is ContentControl parent)
        {
            cell.VerticalAlignment = parent.VerticalContentAlignment;
            cell.HorizontalAlignment = parent.HorizontalContentAlignment;
        }

        cell.Margin = s_cellMargin;
        _cells.Insert(index, cell);
        LogicalChildren.Add(cell);
        VisualChildren.Add(cell);
    }

    private void RemoveCellAt(int index)
    {
        var cell = _cells[index];
        _cells.RemoveAt(index);
        VisualChildren.Remove(cell);
        LogicalChildren.Remove(cell);
        switch (cell)
        {
            case TextBlock text:
                text.ClearValue(TextBlock.TextProperty);
                _recycledText.Push(text);
                break;
            case ContentPresenter cp:
                cp.ContentTemplate = null;
                cp.Content = null;
                _recycledPresenters.Push(cp);
                break;
        }
    }

    private void UpdateCells()
    {
        var parent = TemplatedParent as ContentControl;
        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            if (cell is ContentPresenter cp)
            {
                SetCellContent(cp, Columns is { } columns && i < columns.Count ? columns[i] : null);
            }
            else
            {
                cell.DataContext = Content;
            }

            if (parent is not null)
            {
                cell.VerticalAlignment = parent.VerticalContentAlignment;
                cell.HorizontalAlignment = parent.HorizontalContentAlignment;
            }
        }
    }

    /// <summary>
    /// Gives a template cell the row's item; the template is applied only while there is an item, because Avalonia's
    /// ContentPresenter builds its template even for null content.
    /// </summary>
    private void SetCellContent(ContentPresenter cp, GridViewColumn? column)
    {
        if (Content is null || column is null)
        {
            cp.ContentTemplate = null;
            cp.Content = null;
        }
        else
        {
            cp.Content = Content;
            cp.ContentTemplate = column.CellTemplate ?? column.CellTemplateSelector;
        }
    }

    private static bool AreClose(double a, double b) => Math.Abs(a - b) < 1e-6;
}
