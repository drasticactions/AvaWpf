// Ported from WPF $W/PresentationFramework/System/Windows/Controls/GridViewColumnCollection.cs (MIT, see NOTICE.md).
using System;
using System.Collections.ObjectModel;

namespace AvaWpf.Controls;

/// <summary>
/// The columns of a <see cref="GridView"/>; a column belongs to one collection at a time.
/// </summary>
public class GridViewColumnCollection : ObservableCollection<GridViewColumn>
{
    private bool _isImmutable;

    /// <summary>Raised when a property of a column changes, with the property name.</summary>
    internal event Action<GridViewColumn, string>? ColumnPropertyChanged;

    /// <summary>Makes the collection read-only, while a header is dragged.</summary>
    internal void BlockWrite() => _isImmutable = true;

    /// <summary>Ends <see cref="BlockWrite"/>.</summary>
    internal void UnblockWrite() => _isImmutable = false;

    /// <inheritdoc/>
    protected override void ClearItems()
    {
        VerifyAccess();
        foreach (var c in this)
        {
            Detach(c);
        }

        base.ClearItems();
    }

    /// <inheritdoc/>
    protected override void RemoveItem(int index)
    {
        VerifyAccess();
        Detach(this[index]);
        base.RemoveItem(index);
    }

    /// <inheritdoc/>
    protected override void InsertItem(int index, GridViewColumn item)
    {
        VerifyAccess();
        Attach(item);
        base.InsertItem(index, item);
    }

    /// <inheritdoc/>
    protected override void SetItem(int index, GridViewColumn item)
    {
        VerifyAccess();
        var old = this[index];
        if (ReferenceEquals(old, item))
        {
            return;
        }

        Detach(old);
        Attach(item);
        base.SetItem(index, item);
    }

    /// <inheritdoc/>
    protected override void MoveItem(int oldIndex, int newIndex)
    {
        if (oldIndex != newIndex)
        {
            VerifyAccess();
            base.MoveItem(oldIndex, newIndex);
        }
    }

    private void Attach(GridViewColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (column.Collection is not null)
        {
            throw new InvalidOperationException("A GridViewColumn can belong to one GridViewColumnCollection only.");
        }

        column.Collection = this;
        column.ColumnChanged += OnColumnChanged;
    }

    private void Detach(GridViewColumn column)
    {
        column.ColumnChanged -= OnColumnChanged;
        column.Collection = null;
        column.ResetPrivateData();
    }

    private void OnColumnChanged(GridViewColumn column, string propertyName) => ColumnPropertyChanged?.Invoke(column, propertyName);

    private void VerifyAccess()
    {
        if (_isImmutable)
        {
            throw new InvalidOperationException("The GridViewColumnCollection cannot change while a column header is dragged.");
        }

        CheckReentrancy();
    }
}
