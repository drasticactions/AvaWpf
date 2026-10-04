// Ported from WPF $W/PresentationFramework/System/Windows/Controls/Primitives/GridViewRowPresenterBase.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaWpf.Controls;

/// <summary>The base of the presenters that lay out one element per <see cref="GridViewColumn"/>.</summary>
public abstract class GridViewRowPresenterBase : Control
{
    /// <summary>Defines the <see cref="Columns"/> property.</summary>
    public static readonly StyledProperty<GridViewColumnCollection?> ColumnsProperty =
        AvaloniaProperty.Register<GridViewRowPresenterBase, GridViewColumnCollection?>(nameof(Columns));

    /// <summary>The minimum width of the padding after the last column.</summary>
    internal const double PaddingHeaderMinWidth = 2.0;

    private GridViewColumnCollection? _subscribed;

    static GridViewRowPresenterBase()
    {
        AffectsMeasure<GridViewRowPresenterBase>(ColumnsProperty);
    }

    /// <summary>The columns to lay out.</summary>
    public GridViewColumnCollection? Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>The auto-size width of each column as this presenter last saw it.</summary>
    internal Dictionary<GridViewColumn, double> DesiredWidthList { get; } = new();

    /// <summary>Whether the children must be rebuilt from <see cref="Columns"/> before the next measure.</summary>
    internal bool NeedUpdateVisualTree { get; set; } = true;

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} Columns.Count:{Columns?.Count ?? 0}";

    /// <summary>Rebuilds the children when <see cref="NeedUpdateVisualTree"/> is set, before measuring.</summary>
    public override void ApplyTemplate()
    {
        base.ApplyTemplate();
        if (NeedUpdateVisualTree)
        {
            BuildChildren();
            NeedUpdateVisualTree = false;
        }
    }

    /// <summary>Builds the children for the current <see cref="Columns"/>.</summary>
    internal abstract void BuildChildren();

    /// <summary>Handles a change of a column property.</summary>
    /// <param name="column">The column.</param>
    /// <param name="propertyName">The property name.</param>
    internal abstract void OnColumnPropertyChanged(GridViewColumn column, string propertyName);

    /// <summary>Handles a change of the column collection.</summary>
    /// <param name="e">The change.</param>
    internal virtual void OnColumnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (GridViewColumn c in e.OldItems)
            {
                DesiredWidthList.Remove(c);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            DesiredWidthList.Clear();
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ColumnsProperty)
        {
            if (Avalonia.VisualTree.VisualExtensions.IsAttachedToVisualTree(this))
            {
                Subscribe(Columns);
            }

            DesiredWidthList.Clear();
            NeedUpdateVisualTree = true;
            InvalidateMeasure();
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!ReferenceEquals(_subscribed, Columns))
        {
            NeedUpdateVisualTree = true;
            InvalidateMeasure();
        }

        Subscribe(Columns);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // Keep the collection subscribed only while attached, so a column collection does not hold detached rows.
        Unsubscribe();
        NeedUpdateVisualTree = true;
    }

    private void Subscribe(GridViewColumnCollection? columns)
    {
        if (ReferenceEquals(_subscribed, columns))
        {
            return;
        }

        Unsubscribe();
        if (columns is not null)
        {
            columns.CollectionChanged += OnCollectionChanged;
            columns.ColumnPropertyChanged += OnColumnChanged;
            _subscribed = columns;
        }
    }

    private void Unsubscribe()
    {
        if (_subscribed is { } old)
        {
            old.CollectionChanged -= OnCollectionChanged;
            old.ColumnPropertyChanged -= OnColumnChanged;
            _subscribed = null;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Process the change only once the children reflect the collection.
        if (!NeedUpdateVisualTree)
        {
            OnColumnCollectionChanged(e);
        }
    }

    private void OnColumnChanged(GridViewColumn column, string propertyName)
    {
        if (!NeedUpdateVisualTree)
        {
            OnColumnPropertyChanged(column, propertyName);
        }
    }
}

