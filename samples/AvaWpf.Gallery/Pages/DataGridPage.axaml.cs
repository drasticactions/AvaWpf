using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls;

namespace AvaWpf.Gallery.Pages;

/// <summary>A row of the DataGrid page.</summary>
public sealed class Employee
{
    /// <summary>The name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The job title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The department (the grouping key).</summary>
    public string Department { get; init; } = string.Empty;

    /// <summary>The age.</summary>
    public int Age { get; init; }

    /// <summary>Whether the employee works remotely.</summary>
    public bool IsRemote { get; set; }

    /// <summary>A rating from 0 to 5.</summary>
    public double Rating { get; init; }

    /// <summary>The width of the filled stars: whole stars on a 14 px pitch, then part of the 12 px star.</summary>
    public double RatingWidth => (Math.Floor(Rating) * 14) + ((Rating - Math.Floor(Rating)) * 12);

    /// <summary>The row details text.</summary>
    public string Details => $"{Name} works in {Department} as {Title}" + (IsRemote ? ", remotely." : ".");
}

/// <summary>Groups employees by department without reflection.</summary>
internal sealed class DepartmentGroupDescription : DataGridGroupDescription
{
    public override string PropertyName => nameof(Employee.Department);

    public override object GroupKeyFromItem(object item, int level, CultureInfo culture) => ((Employee)item).Department;
}

public partial class DataGridPage : UserControl
{
    // Comparers, not property paths: path sorting uses reflection.
    private static readonly Dictionary<string, IComparer> s_comparers = new()
    {
        ["Name"] = By(e => e.Name),
        ["Title"] = By(e => e.Title),
        ["Age"] = By(e => e.Age),
        ["Remote"] = By(e => e.IsRemote),
        ["Rating"] = By(e => e.Rating),
    };

    public DataGridPage()
    {
        InitializeComponent();

        var employees = new DataGridCollectionView(Create());
        employees.SortDescriptions.Add(DataGridSortDescription.FromComparer(s_comparers["Name"]));
        Employees.ItemsSource = employees;
        Employees.SelectedIndex = 1;
        UseComparers(Employees);

        var grouped = new DataGridCollectionView(Create());
        grouped.GroupDescriptions.Add(new DepartmentGroupDescription());
        Grouped.ItemsSource = grouped;
        UseComparers(Grouped);

        Disabled.ItemsSource = Create().GetRange(0, 2);
    }

    private static IComparer By<T>(Func<Employee, T> key) =>
        Comparer<object>.Create((x, y) => Comparer<T>.Default.Compare(key((Employee)x), key((Employee)y)));

    private static void UseComparers(Avalonia.Controls.DataGrid grid)
    {
        foreach (var column in grid.Columns)
        {
            // The default looks up the property type by reflection.
            column.CanUserSort = true;
            column.CustomSortComparer = s_comparers[(string)column.Header!];
        }
    }

    private static List<Employee> Create() =>
    [
        new() { Name = "Ada Lovelace", Title = "Analyst", Department = "Research", Age = 36, IsRemote = true, Rating = 4.5 },
        new() { Name = "Charles Babbage", Title = "Engineer", Department = "Research", Age = 48, Rating = 4 },
        new() { Name = "Grace Hopper", Title = "Compiler lead", Department = "Development", Age = 41, IsRemote = true, Rating = 5 },
        new() { Name = "Alan Turing", Title = "Architect", Department = "Development", Age = 39, Rating = 4.8 },
        new() { Name = "Edsger Dijkstra", Title = "Reviewer", Department = "Development", Age = 45, IsRemote = true, Rating = 3.9 },
        new() { Name = "Barbara Liskov", Title = "Designer", Department = "Design", Age = 37, Rating = 4.6 },
        new() { Name = "Donald Knuth", Title = "Writer", Department = "Design", Age = 52, Rating = 4.2 },
    ];
}
