// Ported from WPF $W/PresentationFramework/System/Windows/Controls/GridViewColumn.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;

namespace AvaWpf.Controls;

/// <summary>
/// A column of a <see cref="GridView"/>; a cell shows <see cref="DisplayMemberBinding"/> as text, or else the item through
/// <see cref="CellTemplate"/> or <see cref="CellTemplateSelector"/>.
/// </summary>
public class GridViewColumn : AvaloniaObject
{
    /// <summary>Defines the <see cref="Header"/> property.</summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<GridViewColumn, object?>(nameof(Header));

    /// <summary>Defines the <see cref="HeaderTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<GridViewColumn, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>Defines the <see cref="Width"/> property.</summary>
    public static readonly StyledProperty<double> WidthProperty =
        AvaloniaProperty.Register<GridViewColumn, double>(nameof(Width), double.NaN);

    /// <summary>Defines the <see cref="ActualWidth"/> property.</summary>
    public static readonly DirectProperty<GridViewColumn, double> ActualWidthProperty =
        AvaloniaProperty.RegisterDirect<GridViewColumn, double>(nameof(ActualWidth), o => o.ActualWidth);

    /// <summary>Defines the <see cref="DisplayMemberBinding"/> property.</summary>
    public static readonly StyledProperty<BindingBase?> DisplayMemberBindingProperty =
        AvaloniaProperty.Register<GridViewColumn, BindingBase?>(nameof(DisplayMemberBinding));

    /// <summary>Defines the <see cref="CellTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<GridViewColumn, IDataTemplate?>(nameof(CellTemplate));

    /// <summary>Defines the <see cref="CellTemplateSelector"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> CellTemplateSelectorProperty =
        AvaloniaProperty.Register<GridViewColumn, IDataTemplate?>(nameof(CellTemplateSelector));

    private double _actualWidth;
    private ColumnMeasureState _state = ColumnMeasureState.Init;

    /// <summary>Raised when a property that affects the presenters changes, with the property name.</summary>
    internal event Action<GridViewColumn, string>? ColumnChanged;

    /// <summary>The content of the column header.</summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>The template of the header content. When null, <see cref="GridView.ColumnHeaderTemplate"/> is used.</summary>
    public IDataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>
    /// The width of the column; <see cref="double.NaN"/> (default) fits the header and the cells shown at first layout.
    /// </summary>
    public double Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    /// <summary>The width the column is laid out with.</summary>
    public double ActualWidth
    {
        get => _actualWidth;
        private set => SetAndRaise(ActualWidthProperty, ref _actualWidth, value);
    }

    /// <summary>The binding, on the row item, of the text a cell shows. It takes priority over <see cref="CellTemplate"/>.</summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ListView.ItemsSource), AncestorType = typeof(ListView))]
    public BindingBase? DisplayMemberBinding
    {
        get => GetValue(DisplayMemberBindingProperty);
        set => SetValue(DisplayMemberBindingProperty, value);
    }

    /// <summary>The template of a cell; the row item is its data.</summary>
    [InheritDataTypeFromItems(nameof(ListView.ItemsSource), AncestorType = typeof(ListView))]
    public IDataTemplate? CellTemplate
    {
        get => GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    /// <summary>
    /// A template whose <see cref="IDataTemplate.Match"/> picks the cell content per item when <see cref="CellTemplate"/>
    /// is null.
    /// </summary>
    public IDataTemplate? CellTemplateSelector
    {
        get => GetValue(CellTemplateSelectorProperty);
        set => SetValue(CellTemplateSelectorProperty, value);
    }

    /// <summary>The collection that holds the column, or null.</summary>
    internal GridViewColumnCollection? Collection { get; set; }

    /// <summary>The auto-size width measured so far.</summary>
    internal double DesiredWidth { get; private set; }

    /// <summary>How far the column has been measured; changing it updates <see cref="ActualWidth"/>.</summary>
    internal ColumnMeasureState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                if (value != ColumnMeasureState.Init)
                {
                    UpdateActualWidth();
                }
                else
                {
                    DesiredWidth = 0.0;
                }
            }
            else if (value == ColumnMeasureState.SpecificWidth)
            {
                UpdateActualWidth();
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} Header:{Header}";

    /// <summary>Widens the auto-size width to at least <paramref name="width"/>.</summary>
    /// <param name="width">A measured width.</param>
    /// <returns>The auto-size width.</returns>
    internal double EnsureWidth(double width)
    {
        if (width > DesiredWidth)
        {
            DesiredWidth = width;
        }

        return DesiredWidth;
    }

    /// <summary>Forgets the measured width, when the column leaves its collection.</summary>
    internal void ResetPrivateData()
    {
        DesiredWidth = 0.0;
        _state = double.IsNaN(Width) ? ColumnMeasureState.Init : ColumnMeasureState.SpecificWidth;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WidthProperty)
        {
            State = double.IsNaN(Width) ? ColumnMeasureState.Init : ColumnMeasureState.SpecificWidth;
        }

        ColumnChanged?.Invoke(this, change.Property.Name);
    }

    private void UpdateActualWidth()
    {
        var w = State == ColumnMeasureState.SpecificWidth ? Width : DesiredWidth;
        if (!double.IsNaN(w) && !double.IsInfinity(w) && w >= 0)
        {
            ActualWidth = w;
        }
    }
}
