using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataGrid = Avalonia.Controls.DataGrid;

namespace AvaWpf.DataGrid;

/// <summary>Attached state that the DataGrid templates select on.</summary>
public static class DataGridState
{
    /// <summary>
    /// Defines the inherited <c>IsSelectionActive</c> attached property, true while the <see cref="AvaloniaDataGrid"/> has
    /// the keyboard focus within (WPF <c>Selector.IsSelectionActive</c>).
    /// </summary>
    public static readonly AttachedProperty<bool> IsSelectionActiveProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("IsSelectionActive", typeof(DataGridState), inherits: true);

    /// <summary>
    /// Defines the inherited <c>IsRowSelected</c> attached property, set from <see cref="DataGridRow.IsSelected"/>;
    /// Avalonia updates a cell's <c>:selected</c> only on re-template or when it becomes current.
    /// </summary>
    public static readonly AttachedProperty<bool> IsRowSelectedProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaObject, bool>("IsRowSelected", typeof(DataGridState), inherits: true);

    /// <summary>
    /// Defines the <c>Points</c> attached property, which lets a <see cref="Polygon"/> take its points from a string
    /// resource.
    /// </summary>
    public static readonly AttachedProperty<string?> PointsProperty =
        AvaloniaProperty.RegisterAttached<Polygon, string?>("Points", typeof(DataGridState));

    private static bool s_registered;

    static DataGridState() => EnsureRegistered();

    /// <summary>Gets <see cref="IsSelectionActiveProperty"/>.</summary>
    /// <param name="element">The element.</param>
    public static bool GetIsSelectionActive(AvaloniaObject element) => element.GetValue(IsSelectionActiveProperty);

    /// <summary>Sets <see cref="IsSelectionActiveProperty"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetIsSelectionActive(AvaloniaObject element, bool value) => element.SetValue(IsSelectionActiveProperty, value);

    /// <summary>Gets <see cref="IsRowSelectedProperty"/>.</summary>
    /// <param name="element">The element.</param>
    public static bool GetIsRowSelected(AvaloniaObject element) => element.GetValue(IsRowSelectedProperty);

    /// <summary>Sets <see cref="IsRowSelectedProperty"/>.</summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The value.</param>
    public static void SetIsRowSelected(AvaloniaObject element, bool value) => element.SetValue(IsRowSelectedProperty, value);

    /// <summary>Gets <see cref="PointsProperty"/>.</summary>
    /// <param name="element">The polygon.</param>
    public static string? GetPoints(Polygon element) => element.GetValue(PointsProperty);

    /// <summary>Sets <see cref="PointsProperty"/>.</summary>
    /// <param name="element">The polygon.</param>
    /// <param name="value">The points, as <c>"x,y x,y …"</c>.</param>
    public static void SetPoints(Polygon element, string? value) => element.SetValue(PointsProperty, value);

    /// <summary>Registers the class handlers once.</summary>
    internal static void EnsureRegistered()
    {
        if (s_registered)
        {
            return;
        }

        s_registered = true;
        InputElement.IsKeyboardFocusWithinProperty.Changed.AddClassHandler<AvaloniaDataGrid>(
            (grid, _) => grid.SetValue(IsSelectionActiveProperty, grid.IsKeyboardFocusWithin));
        DataGridRow.IsSelectedProperty.Changed.AddClassHandler<DataGridRow>(
            (row, _) => row.SetValue(IsRowSelectedProperty, row.IsSelected));
        PointsProperty.Changed.AddClassHandler<Polygon>((polygon, e) => polygon.Points = Parse(e.NewValue as string));

        // Avalonia's DataGridCheckBoxColumn disables all but the current cell's check box, which greys them out; WPF keeps
        // them enabled. A disabled DataGrid still disables the check box through its parent.
        InputElement.IsEnabledProperty.Changed.AddClassHandler<CheckBox>((box, _) => KeepCellCheckBoxEnabled(box));
        StyledElement.ParentProperty.Changed.AddClassHandler<CheckBox>((box, _) => KeepCellCheckBoxEnabled(box));

        TemplatedControl.TemplateProperty.Changed.AddClassHandler<AvaloniaDataGrid>((grid, _) => GuardReattach(grid));
    }

    // Avalonia's DataGridRowGroupHeader drops its grid when it leaves the logical tree. If it is the current element,
    // the grid updates it while re-attaching and dereferences that grid, but only with row headers shown. So the row
    // headers are hidden while such a grid is detached and come back once it is attached.
    private static readonly AttachedProperty<bool> s_guardedProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaDataGrid, bool>("ReattachGuarded", typeof(DataGridState));

    private static readonly AttachedProperty<DataGridHeadersVisibility?> s_hiddenRowHeadersProperty =
        AvaloniaProperty.RegisterAttached<AvaloniaDataGrid, DataGridHeadersVisibility?>("HiddenRowHeaders", typeof(DataGridState));

    // Avalonia sizes the top-left corner to the row headers when the template is applied with rows, or when the row
    // header width grows. A grid re-templated by a theme switch has no rows yet and its row header width doesn't grow
    // again, so the corner would size to its own content and push the column headers off their columns.
    private static void SizeTopLeftCorner(AvaloniaDataGrid grid)
    {
        void OnLayoutUpdated(object? sender, System.EventArgs e)
        {
            var corner = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().FirstOrDefault(h => h.Name == "PART_TopLeftCornerHeader");
            if (corner is null || !double.IsNaN(corner.Width) || !corner.IsVisible)
            {
                grid.LayoutUpdated -= OnLayoutUpdated;
                return;
            }

            if (grid.GetVisualDescendants().OfType<DataGridRowHeader>().FirstOrDefault(h => h.Bounds.Width > 0) is { } rowHeader)
            {
                grid.LayoutUpdated -= OnLayoutUpdated;
                corner.Width = rowHeader.Bounds.Width;
            }
        }

        grid.LayoutUpdated += OnLayoutUpdated;
    }

    // A re-templated grid lays out its horizontal scroll bar before a new vertical scroll bar takes its width, and keeps
    // that stale extent; one more measure after the first layout settles it.
    private static void RemeasureOnce(AvaloniaDataGrid grid)
    {
        void OnLayoutUpdated(object? sender, System.EventArgs e)
        {
            grid.LayoutUpdated -= OnLayoutUpdated;
            grid.InvalidateMeasure();
        }

        grid.LayoutUpdated += OnLayoutUpdated;
    }

    private static void GuardReattach(AvaloniaDataGrid grid)
    {
        if (grid.GetValue(s_guardedProperty))
        {
            return;
        }

        grid.SetValue(s_guardedProperty, true);
        grid.TemplateApplied += (_, _) =>
        {
            SizeTopLeftCorner(grid);
            RemeasureOnce(grid);
        };
        grid.DetachedFromVisualTree += (_, _) =>
        {
            var headers = grid.HeadersVisibility;
            if (headers.HasFlag(DataGridHeadersVisibility.Row) &&
                grid.GetVisualDescendants().OfType<DataGridRowGroupHeader>().Any(h => h.Classes.Contains(":current")))
            {
                // A local value is put back as it was; a style value is cleared so the new theme's style applies.
                grid.SetValue(s_hiddenRowHeadersProperty, grid.IsSet(AvaloniaDataGrid.HeadersVisibilityProperty) ? headers : DataGridHeadersVisibility.None);
                grid.HeadersVisibility = headers & ~DataGridHeadersVisibility.Row;
            }
        };
        grid.AttachedToVisualTree += (_, _) =>
        {
            if (grid.GetValue(s_hiddenRowHeadersProperty) is { } hidden)
            {
                grid.ClearValue(s_hiddenRowHeadersProperty);
                if (hidden == DataGridHeadersVisibility.None)
                {
                    grid.ClearValue(AvaloniaDataGrid.HeadersVisibilityProperty);
                }
                else
                {
                    grid.HeadersVisibility = hidden;
                }
            }
        };
    }

    private static void KeepCellCheckBoxEnabled(CheckBox box)
    {
        if (!box.IsEnabled && !box.IsHitTestVisible && box.Parent is DataGridCell)
        {
            box.Focusable = false;
            box.IsEnabled = true;
        }
    }

    /// <summary>Parses <c>"x,y x,y …"</c> (commas or spaces between numbers) into points.</summary>
    /// <param name="text">The text.</param>
    internal static Points Parse(string? text)
    {
        var points = new Points();
        if (string.IsNullOrWhiteSpace(text))
        {
            return points;
        }

        var numbers = text.Split([' ', ',', '\t', '\n', '\r'], System.StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i + 1 < numbers.Length; i += 2)
        {
            points.Add(new Point(
                double.Parse(numbers[i], CultureInfo.InvariantCulture),
                double.Parse(numbers[i + 1], CultureInfo.InvariantCulture)));
        }

        return points;
    }
}
