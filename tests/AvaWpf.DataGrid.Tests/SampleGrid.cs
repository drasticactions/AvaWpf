using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using AvaloniaDataGrid = Avalonia.Controls.DataGrid;

namespace AvaWpf.DataGrid.Tests;

/// <summary>A row of the sample grid.</summary>
public sealed class Person
{
    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public bool IsMember { get; set; }

    public string Team { get; set; } = string.Empty;
}

/// <summary>Builds a DataGrid with text, check box and template columns, row details and grouping.</summary>
internal static class SampleGrid
{
    public static List<Person> People() =>
    [
        new() { Name = "Ada", Age = 36, IsMember = true, Team = "Red" },
        new() { Name = "Brook", Age = 28, IsMember = false, Team = "Red" },
        new() { Name = "Cyril", Age = 45, IsMember = true, Team = "Blue" },
        new() { Name = "Dana", Age = 31, IsMember = false, Team = "Blue" },
    ];

    public static AvaloniaDataGrid Create(bool grouped = true)
    {
        var view = new DataGridCollectionView(People());
        if (grouped)
        {
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Person.Team)));
        }

        var grid = new AvaloniaDataGrid
        {
            ItemsSource = view,
            AutoGenerateColumns = false,
            CanUserSortColumns = true,
            RowDetailsTemplate = new FuncDataTemplate<Person>((p, _) => new TextBlock { Text = "Details of " + p?.Name }),
            RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Visible,
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(Person.Name)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Age", Binding = new Binding(nameof(Person.Age)) });
        grid.Columns.Add(new DataGridCheckBoxColumn { Header = "Member", Binding = new Binding(nameof(Person.IsMember)) });
        grid.Columns.Add(new DataGridTemplateColumn
        {
            Header = "Badge",
            CellTemplate = new FuncDataTemplate<Person>((p, _) => new TextBlock { Text = p?.Name.Substring(0, 1) }),
        });
        return grid;
    }
}
