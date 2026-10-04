using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Controls.Tests;

/// <summary>GridView column resize, auto-size and reorder, and shared widths across rows.</summary>
public class GridViewTests
{
    public sealed class Person(string name, string city)
    {
        public string Name { get; } = name;

        public string City { get; } = city;
    }

    private static (Window Window, ListView List, GridView View) Show(double nameWidth = 100)
    {
        var view = new GridView
        {
            Columns =
            {
                new GridViewColumn { Header = "Name", Width = nameWidth, DisplayMemberBinding = new ReflectionBinding(nameof(Person.Name)) },
                new GridViewColumn { Header = "City", DisplayMemberBinding = new ReflectionBinding(nameof(Person.City)) },
            },
        };
        var list = new ListView
        {
            View = view,
            ItemsSource = new List<Person>
            {
                new("Ann", "Oslo"),
                new("Bob", "Rio"),
                new("Carmen", "Kuala Lumpur and beyond"),
            },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Width = 600,
            Height = 300,
        };
        var window = TestHelpers.Show(list, 800, 400);
        return (window, list, view);
    }

    private static GridViewHeaderRowPresenter HeaderRow(ListView list) => TestHelpers.Find<GridViewHeaderRowPresenter>(list);

    private static List<GridViewRowPresenter> Rows(ListView list) => TestHelpers.All<GridViewRowPresenter>(list).ToList();

    private static void AssertRowsFollowColumns(ListView list, GridView view)
    {
        var header = HeaderRow(list);
        var rows = Rows(list);
        Assert.Equal(3, rows.Count);
        for (var i = 0; i < view.Columns.Count; i++)
        {
            var column = view.Columns[i];
            var headerCell = header.Headers[i];
            Assert.Same(column, headerCell.Column);
            Assert.Equal(column.ActualWidth, headerCell.Bounds.Width, 3);
            foreach (var row in rows)
            {
                // The cell's layout slot is the column: its X and width match the header's.
                var slot = LayoutInformation.GetPreviousArrangeBounds(row.Cells[i])!.Value;
                Assert.Equal(column.ActualWidth, slot.Width, 3);
                Assert.Equal(headerCell.Bounds.X, slot.X, 3);
                Assert.Equal(headerCell.Bounds.X + 6, row.Cells[i].Bounds.X, 3);
            }
        }
    }

    [AvaloniaFact]
    public void Rows_Share_The_Column_Widths()
    {
        var (window, list, view) = Show();
        Assert.Equal(100, view.Columns[0].ActualWidth);
        AssertRowsFollowColumns(list, view);
        window.Close();
    }

    [AvaloniaFact]
    public void Auto_Column_Sizes_To_The_Widest_Cell()
    {
        var (window, list, view) = Show();
        var city = view.Columns[1];
        Assert.True(double.IsNaN(city.Width));
        var widest = Rows(list).Max(r => r.Cells[1].DesiredSize.Width);
        var headerWidth = HeaderRow(list).Headers[1].DesiredSize.Width;
        Assert.Equal(System.Math.Max(widest, headerWidth), city.ActualWidth, 3);
        Assert.True(city.ActualWidth > 100, $"{city.ActualWidth}");
        window.Close();
    }

    [AvaloniaFact]
    public void Gripper_Drag_Resizes_The_Column_In_Every_Row()
    {
        var (window, list, view) = Show();
        var header = HeaderRow(list).Headers[0];
        var gripper = TestHelpers.Find<Thumb>(header, "PART_HeaderGripper");
        var start = TestHelpers.Center(gripper, window);

        TestHelpers.Drag(window, start, new Point(start.X + 40, start.Y));

        Assert.Equal(140, view.Columns[0].Width, 1);
        AssertRowsFollowColumns(list, view);

        TestHelpers.Drag(window, TestHelpers.Center(gripper, window), new Point(start.X - 30, start.Y));
        Assert.Equal(70, view.Columns[0].Width, 1);
        AssertRowsFollowColumns(list, view);
        window.Close();
    }

    [AvaloniaFact]
    public void Gripper_Double_Click_Auto_Sizes_The_Column()
    {
        var (window, list, view) = Show(nameWidth: 300);
        var name = view.Columns[0];
        Assert.Equal(300, name.ActualWidth);
        var header = HeaderRow(list).Headers[0];
        var p = TestHelpers.Center(TestHelpers.Find<Thumb>(header, "PART_HeaderGripper"), window);

        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        TestHelpers.Layout(window);

        Assert.True(double.IsNaN(name.Width));
        var widest = System.Math.Max(Rows(list).Max(r => r.Cells[0].DesiredSize.Width), HeaderRow(list).Headers[0].DesiredSize.Width);
        Assert.Equal(widest, name.ActualWidth, 3);
        Assert.True(name.ActualWidth < 300);
        AssertRowsFollowColumns(list, view);
        window.Close();
    }

    [AvaloniaFact]
    public void Header_Drag_Reorders_The_Columns()
    {
        var (window, list, view) = Show();
        var name = view.Columns[0];
        var city = view.Columns[1];
        var headerRow = HeaderRow(list);
        var cityHeader = headerRow.Headers[1];
        var from = TestHelpers.Center(cityHeader, window);
        var nameHeader = headerRow.Headers[0];
        var to = nameHeader.TranslatePoint(new Point(10, nameHeader.Bounds.Height / 2), window)!.Value;

        var clicks = 0;
        cityHeader.Click += (_, _) => clicks++;
        window.MouseDown(from, MouseButton.Left);
        TestHelpers.Layout(window);
        window.MouseMove(new Point(from.X - 10, from.Y), RawInputModifiers.LeftMouseButton);
        TestHelpers.Layout(window);
        Assert.True(headerRow.IsHeaderDragging);
        window.MouseMove(new Point(from.X - 20, from.Y), RawInputModifiers.LeftMouseButton);
        TestHelpers.Layout(window);
        Assert.True(headerRow.FloatingHeader!.IsVisible);
        Assert.True(headerRow.Indicator!.IsVisible);
        window.MouseMove(to, RawInputModifiers.LeftMouseButton);
        TestHelpers.Layout(window);
        window.MouseUp(to, MouseButton.Left);
        TestHelpers.Layout(window);

        Assert.Equal(0, clicks);
        Assert.Same(city, view.Columns[0]);
        Assert.Same(name, view.Columns[1]);
        Assert.Same(city, headerRow.Headers[0].Column);
        Assert.False(headerRow.FloatingHeader.IsVisible);
        foreach (var row in Rows(list))
        {
            var person = (Person)row.Content!;
            Assert.Equal(person.City, ((TextBlock)row.Cells[0]).Text);
            Assert.Equal(person.Name, ((TextBlock)row.Cells[1]).Text);
        }

        AssertRowsFollowColumns(list, view);
        window.Close();
    }

    [AvaloniaFact]
    public void Header_Click_Raises_Routed_Click()
    {
        var (window, list, _) = Show();
        var header = HeaderRow(list).Headers[1];
        GridViewColumnHeader? clicked = null;
        list.AddHandler(Button.ClickEvent, (_, e) => clicked = e.Source as GridViewColumnHeader);
        var p = TestHelpers.Center(header, window);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Assert.Same(header, clicked);
        window.Close();
    }

    [AvaloniaFact]
    public void Header_Scrolls_With_The_Rows()
    {
        var (window, list, view) = Show(nameWidth: 700);
        var sv = TestHelpers.Find<ScrollViewer>(list, "PART_ScrollViewer");
        var headerSv = TestHelpers.Find<ScrollViewer>(list, "PART_HeaderScrollViewer");
        sv.Offset = new Vector(50, 0);
        TestHelpers.Layout(window);
        Assert.Equal(50, headerSv.Offset.X, 1);
        Assert.Equal(0, headerSv.Offset.Y);

        // The header row stays above the rows when they scroll vertically.
        Assert.True(HeaderRow(list).TranslatePoint(default, list)!.Value.Y < Rows(list)[0].TranslatePoint(default, list)!.Value.Y);
        window.Close();
    }

    [AvaloniaFact]
    public void Columns_Added_And_Removed_Update_Every_Row()
    {
        var (window, list, view) = Show();
        var extra = new GridViewColumn { Header = "Extra", Width = 50, DisplayMemberBinding = new ReflectionBinding(nameof(Person.Name)) };
        view.Columns.Add(extra);
        TestHelpers.Layout(window);
        Assert.All(Rows(list), r => Assert.Equal(3, r.Cells.Count));
        Assert.Equal(3, HeaderRow(list).Headers.Count);
        AssertRowsFollowColumns(list, view);

        var removedCell = Rows(list)[0].Cells[2];
        view.Columns.Remove(extra);
        TestHelpers.Layout(window);
        Assert.All(Rows(list), r => Assert.Equal(2, r.Cells.Count));

        // The row reuses the removed cell when a column comes back.
        view.Columns.Add(new GridViewColumn { Header = "Again", Width = 40, DisplayMemberBinding = new ReflectionBinding(nameof(Person.City)) });
        TestHelpers.Layout(window);
        Assert.Same(removedCell, Rows(list)[0].Cells[2]);
        AssertRowsFollowColumns(list, view);
        window.Close();
    }

    [AvaloniaFact]
    public void GridView_Shows_One_Header_Row()
    {
        var (window, list, _) = Show();
        Assert.Single(TestHelpers.All<ScrollViewer>(list), s => s.Name == "PART_HeaderScrollViewer");
        Assert.Single(TestHelpers.All<GridViewHeaderRowPresenter>(list));
        window.Close();
    }

    [AvaloniaFact]
    public void Cell_Template_Is_Not_Applied_To_A_Recycled_Row_Without_An_Item()
    {
        // The template reads the item without a null check, as one written for WPF may: WPF never applies a cell
        // template to no item. Clearing and refilling the source (a re-sort) recycles every row.
        var people = new ObservableCollection<Person> { new("Ann", "Oslo"), new("Bob", "Rio") };
        var view = new GridView
        {
            Columns =
            {
                new GridViewColumn
                {
                    Header = "Name",
                    CellTemplate = new FuncDataTemplate<Person>((p, _) => new TextBlock { Text = p.Name.ToUpperInvariant() }),
                },
            },
        };
        var list = new ListView { View = view, ItemsSource = people, Width = 300, Height = 200 };
        var window = TestHelpers.Show(list, 400, 300);

        var sorted = people.OrderByDescending(p => p.Name).ToList();
        people.Clear();
        foreach (var p in sorted)
        {
            people.Add(p);
        }

        TestHelpers.Layout(window);
        var texts = Rows(list).Select(r => ((TextBlock)((ContentPresenter)r.Cells[0]).Child!).Text).ToList();
        Assert.Equal(new[] { "BOB", "ANN" }, texts);
        window.Close();
    }
}
