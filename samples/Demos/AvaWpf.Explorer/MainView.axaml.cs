using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaWpf.Controls;
using AvaWpf.Explorer.ViewModels;

namespace AvaWpf.Explorer;

/// <summary>The Explorer shell: menu, toolbars, Folders pane, file list and status bar.</summary>
public partial class MainView : UserControl
{
    private readonly GridView _details;
    private readonly ContextMenu _itemMenu;
    private readonly ContextMenu _backgroundMenu;
    private GridLength _folderWidth = new(220);

    public MainView()
    {
        InitializeComponent();
        ViewModel = new ExplorerViewModel();
        DataContext = ViewModel;

        _details = (GridView)Resources["DetailsView"]!;
        for (var i = 0; i < _details.Columns.Count; i++)
        {
            _details.Columns[i].Header = ViewModel.Headers[i];
        }

        _itemMenu = (ContextMenu)Resources["ItemMenu"]!;
        _backgroundMenu = (ContextMenu)Resources["BackgroundMenu"]!;
        _itemMenu.DataContext = ViewModel;
        _backgroundMenu.DataContext = ViewModel;

        FileList.SelectionChanged += (_, _) => ViewModel.SetSelection(FileList.SelectedItems?.OfType<FileItem>() ?? []);
        FileList.DoubleTapped += OnListDoubleTapped;
        FileList.ContextRequested += OnListContextRequested;
        FileList.AddHandler(Button.ClickEvent, OnColumnHeaderClick);
        FileList.AddHandler(LostFocusEvent, OnRenameLostFocus);
        AddressBox.DropDownClosed += (_, _) =>
        {
            if (AddressBox.SelectedItem is string path)
            {
                ViewModel.NavigateToCommand.Execute(path);
            }
        };
        DialogFrame.CaptionButtonInvoked += (_, _) => ViewModel.CloseDialogCommand.Execute(null);

        CloseItem.Click += (_, _) => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        SelectAllItem.Click += (_, _) => FileList.SelectAll();
        InvertSelectionItem.Click += (_, _) => InvertSelection();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.RenameStarted += OnRenameStarted;
        BuildThemeMenu();
        ApplyViewMode();
    }

    /// <summary>The view model.</summary>
    public ExplorerViewModel ViewModel { get; }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ExplorerViewModel.ViewMode):
                ApplyViewMode();
                break;
            case nameof(ExplorerViewModel.ShowFolders):
                var column = Panes.ColumnDefinitions[0];
                if (ViewModel.ShowFolders)
                {
                    column.Width = _folderWidth;
                }
                else
                {
                    _folderWidth = column.Width;
                    column.Width = new GridLength(0);
                }

                break;
        }
    }

    /// <summary>Details uses the GridView; Icons and List are a plain ListView with a wrapping panel.</summary>
    private void ApplyViewMode()
    {
        switch (ViewModel.ViewMode)
        {
            case ViewMode.Details:
                FileList.ItemTemplate = null;
                FileList.ItemsPanel = (ITemplate<Panel?>)Resources["DetailsPanel"]!;
                FileList.View = _details;
                ScrollViewer.SetHorizontalScrollBarVisibility(FileList, ScrollBarVisibility.Auto);
                ScrollViewer.SetVerticalScrollBarVisibility(FileList, ScrollBarVisibility.Auto);
                break;
            case ViewMode.Icons:
                FileList.View = null;
                FileList.ItemTemplate = (IDataTemplate)Resources["IconsTemplate"]!;
                FileList.ItemsPanel = (ITemplate<Panel?>)Resources["IconsPanel"]!;
                ScrollViewer.SetHorizontalScrollBarVisibility(FileList, ScrollBarVisibility.Disabled);
                ScrollViewer.SetVerticalScrollBarVisibility(FileList, ScrollBarVisibility.Auto);
                break;
            case ViewMode.List:
                FileList.View = null;
                FileList.ItemTemplate = (IDataTemplate)Resources["ListTemplate"]!;
                FileList.ItemsPanel = (ITemplate<Panel?>)Resources["ListPanel"]!;
                ScrollViewer.SetHorizontalScrollBarVisibility(FileList, ScrollBarVisibility.Auto);
                ScrollViewer.SetVerticalScrollBarVisibility(FileList, ScrollBarVisibility.Disabled);
                break;
        }
    }

    private static FileItem? ItemAt(RoutedEventArgs e) =>
        (e.Source as Visual)?.FindAncestorOfType<ListViewItem>(includeSelf: true)?.DataContext as FileItem;

    private void OnListDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (ItemAt(e) is { IsRenaming: false } item)
        {
            ViewModel.OpenCommand.Execute(item);
        }
    }

    /// <summary>An item gets the item menu (selecting it first); the empty area gets the background menu.</summary>
    private void OnListContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if ((e.Source as Visual)?.FindAncestorOfType<GridViewColumnHeader>(includeSelf: true) is not null)
        {
            return;
        }

        var item = ItemAt(e);
        if (item is not null && FileList.SelectedItems?.Contains(item) != true)
        {
            FileList.SelectedItem = item;
        }
        else if (item is null)
        {
            FileList.SelectedItem = null;
        }

        (item is null ? _backgroundMenu : _itemMenu).Open(FileList);
        e.Handled = true;
    }

    /// <summary>A click on a column header sorts by that column (again: the other direction), as Explorer does.</summary>
    private void OnColumnHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is GridViewColumnHeader { Role: GridViewColumnHeaderRole.Normal, Column.Header: SortHeader header })
        {
            ViewModel.SortByCommand.Execute(header.Key);
        }
    }

    private void OnRenameStarted(object? sender, FileItem item)
    {
        FileList.ScrollIntoView(item);
        Dispatcher.UIThread.Post(() =>
        {
            var box = FileList.ContainerFromItem(item)?.GetVisualDescendants().OfType<TextBox>()
                .FirstOrDefault(t => t.Classes.Contains("rename") && t.IsVisible);
            if (box is null)
            {
                return;
            }

            box.Focus();

            // Explorer selects the name without the extension.
            box.SelectionStart = 0;
            box.SelectionEnd = item.Node.IsContainer ? item.EditName.Length : Path.GetFileNameWithoutExtension(item.EditName).Length;
        }, DispatcherPriority.Loaded);
    }

    private void OnRenameLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox box && box.Classes.Contains("rename") && box.DataContext is FileItem item)
        {
            item.CommitRename();
        }
    }

    private void InvertSelection()
    {
        var selected = FileList.SelectedItems?.OfType<FileItem>().ToHashSet() ?? [];
        FileList.SelectedItems?.Clear();
        foreach (var item in ViewModel.Items.Where(i => !selected.Contains(i)))
        {
            FileList.SelectedItems?.Add(item);
        }
    }

    /// <summary>View → Theme: every family (with its color schemes) and the Light, Dark and High Contrast variants.</summary>
    private void BuildThemeMenu()
    {
        foreach (var family in Enum.GetValues<ThemeFamily>())
        {
            var schemes = ColorSchemes.For(family).Where(s => !ColorSchemes.IsHighContrast(s)).ToList();
            var item = new MenuItem { Header = family.ToString(), ToggleType = MenuItemToggleType.Radio, GroupName = "ThemeFamily", Tag = family };
            if (schemes.Count > 1)
            {
                foreach (var scheme in schemes)
                {
                    var schemeItem = new MenuItem { Header = scheme, ToggleType = MenuItemToggleType.Radio, GroupName = "ThemeScheme", Tag = scheme };
                    schemeItem.Click += (_, e) =>
                    {
                        SetTheme(family, scheme);
                        e.Handled = true;
                    };
                    item.Items.Add(schemeItem);
                }
            }
            else
            {
                item.Click += (_, _) => SetTheme(family, null);
            }

            ThemeMenu.Items.Add(item);
        }

        ThemeMenu.Items.Add(new Separator());
        foreach (var (name, variant) in new[] { ("Light", ThemeVariant.Light), ("Dark", ThemeVariant.Dark), ("High Contrast", WpfThemeVariants.HighContrast) })
        {
            var item = new MenuItem { Header = name, ToggleType = MenuItemToggleType.Radio, GroupName = "ThemeVariant", Tag = variant };
            item.Click += (_, _) =>
            {
                Application.Current!.RequestedThemeVariant = variant;
                SyncThemeMenu();
            };
            ThemeMenu.Items.Add(item);
        }

        ThemeMenu.SubmenuOpened += (_, _) => SyncThemeMenu();
        SyncThemeMenu();
    }

    private static void SetTheme(ThemeFamily family, string? scheme)
    {
        var theme = AvaWpfTheme.Find();
        if (theme is null)
        {
            return;
        }

        theme.ColorScheme = null;
        theme.Theme = family;
        theme.ColorScheme = scheme;
    }

    private void SyncThemeMenu()
    {
        if (AvaWpfTheme.Find() is not { } theme)
        {
            return;
        }

        var variant = Application.Current!.ActualThemeVariant;
        var variantKey = WpfThemeVariants.IsHighContrast(variant) ? WpfThemeVariants.HighContrast : WpfThemeVariants.IsDark(variant) ? ThemeVariant.Dark : ThemeVariant.Light;
        foreach (var item in ThemeMenu.Items.OfType<MenuItem>())
        {
            switch (item.Tag)
            {
                case ThemeFamily family:
                    item.IsChecked = family == theme.Theme;
                    foreach (var schemeItem in item.Items.OfType<MenuItem>())
                    {
                        schemeItem.IsChecked = family == theme.Theme && Equals(schemeItem.Tag, theme.ActualColorScheme);
                    }

                    break;
                case ThemeVariant v:
                    item.IsChecked = Equals(v, variantKey);
                    break;
            }
        }
    }
}
