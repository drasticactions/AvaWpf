using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using AvaWpf.Controls;
using AvaWpf.Samples;

namespace AvaWpf.Gallery.Pages;

/// <summary>ListView with a GridView (sortable, resizable, reorderable columns) and a plain ListView.</summary>
public class ListViewPage : UserControl
{
    private readonly ObservableCollection<FileNode> _files;
    private string? _sortedBy;
    private bool _descending;

    public ListViewPage()
    {
        _files = new ObservableCollection<FileNode>(Flatten(SampleFileSystem.Create()));
        var view = new GridView
        {
            Columns =
            {
                new GridViewColumn { Header = "Name", Width = 190, CellTemplate = NameTemplate() },
                new GridViewColumn { Header = "Size", Width = 80, DisplayMemberBinding = CompiledBinding.Create<FileNode, string>(f => f.SizeText) },
                new GridViewColumn { Header = "Type", Width = 120, DisplayMemberBinding = CompiledBinding.Create<FileNode, string>(f => f.TypeText) },
                new GridViewColumn { Header = "Modified", DisplayMemberBinding = CompiledBinding.Create<FileNode, string>(f => f.ModifiedText) },
            },
        };

        var list = new ListView
        {
            View = view,
            ItemsSource = _files,
            SelectedIndex = 1,
            Height = 260,
            Width = 560,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        list.AddHandler(Button.ClickEvent, OnHeaderClick);

        var plain = new ListView
        {
            ItemsSource = new[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo" },
            SelectedIndex = 2,
            Height = 110,
            Width = 220,
            HorizontalAlignment = HorizontalAlignment.Left,
        };

        Content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Classes = { "Subtitle" }, Text = "GridView (click a header to sort, drag its edge to resize, drag it to reorder)" },
                list,
                new TextBlock { Classes = { "Subtitle" }, Text = "ListView without a view" },
                plain,
                new TextBlock { Classes = { "Subtitle" }, Text = "Disabled" },
                new ListView
                {
                    View = new GridView { Columns = { new GridViewColumn { Header = "Name", Width = 150, DisplayMemberBinding = CompiledBinding.Create<FileNode, string>(f => f.Name) } } },
                    ItemsSource = _files.Take(3).ToList(),
                    IsEnabled = false,
                    Height = 90,
                    Width = 220,
                    HorizontalAlignment = HorizontalAlignment.Left,
                },
            },
        };
    }

    private static IEnumerable<FileNode> Flatten(FileNode root)
    {
        foreach (var child in root.Children)
        {
            yield return child;
            foreach (var d in Flatten(child))
            {
                yield return d;
            }
        }
    }

    private static FuncDataTemplate<FileNode> NameTemplate() => new((node, _) => new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Spacing = 4,
        Children =
        {
            new Image { Source = SampleImages.Load(node.IconName), Width = 16, Height = 16 },
            new TextBlock { Text = node.Name, VerticalAlignment = VerticalAlignment.Center },
        },
    });

    private void OnHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not GridViewColumnHeader { Column.Header: string column })
        {
            return;
        }

        _descending = column == _sortedBy && !_descending;
        _sortedBy = column;
        Func<FileNode, IComparable> key = column switch
        {
            "Size" => f => f.Size,
            "Type" => f => f.TypeText,
            "Modified" => f => f.Modified,
            _ => f => f.Name,
        };

        // Folders first, as Explorer sorts.
        var sorted = _descending
            ? _files.OrderBy(f => !f.IsContainer).ThenByDescending(key).ToList()
            : _files.OrderBy(f => !f.IsContainer).ThenBy(key).ToList();
        _files.Clear();
        foreach (var f in sorted)
        {
            _files.Add(f);
        }
    }
}
