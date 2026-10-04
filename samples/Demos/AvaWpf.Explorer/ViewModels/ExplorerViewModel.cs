using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using AvaWpf.Samples;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaWpf.Explorer.ViewModels;

/// <summary>The Explorer window: the current folder, its items, the navigation history and the clipboard.</summary>
public sealed partial class ExplorerViewModel : ObservableObject
{
    private readonly Stack<FileNode> _back = new();
    private readonly Stack<FileNode> _forward = new();
    private readonly List<FileNode> _clipboard = new();
    private bool _clipboardIsCut;
    private bool _navigating;
    private IReadOnlyList<FileItem> _selection = Array.Empty<FileItem>();

    /// <summary>Initializes the view model over a fresh <see cref="SampleFileSystem"/>, showing My Documents.</summary>
    public ExplorerViewModel()
    {
        Root = SampleFileSystem.Create();
        RootFolder = new FolderItem(Root, null) { IsExpanded = true };
        Folders = new ObservableCollection<FolderItem> { RootFolder };
        Headers = [new SortHeader("Name", "Name"), new SortHeader("Size", "Size"), new SortHeader("Type", "Type"), new SortHeader("Modified", "Date Modified")];
        Headers[0].IsAscending = true;
        _current = Root;
        Navigate(Find("Desktop\\My Documents") ?? Root, addToHistory: false);
    }

    /// <summary>The root of the fake file system ("Desktop").</summary>
    public FileNode Root { get; }

    /// <summary>The root item of the Folders pane.</summary>
    public FolderItem RootFolder { get; }

    /// <summary>The roots of the Folders pane (one: Desktop).</summary>
    public ObservableCollection<FolderItem> Folders { get; }

    /// <summary>The items of the current folder, sorted.</summary>
    public ObservableCollection<FileItem> Items { get; } = new();

    /// <summary>The paths visited, for the address drop-down.</summary>
    public ObservableCollection<string> AddressHistory { get; } = new();

    /// <summary>The GridView column headers: Name, Size, Type, Date Modified.</summary>
    public SortHeader[] Headers { get; }

    /// <summary>The folder shown.</summary>
    [ObservableProperty]
    private FileNode _current;

    /// <summary>The address text (the current path, or what the user typed).</summary>
    [ObservableProperty]
    private string _addressText = string.Empty;

    /// <summary>The folder selected in the Folders pane.</summary>
    [ObservableProperty]
    private FolderItem? _selectedFolder;

    /// <summary>The first status bar panel ("12 objects").</summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>The second status bar panel (the total size).</summary>
    [ObservableProperty]
    private string _statusSize = string.Empty;

    /// <summary>The window title (the current folder name).</summary>
    [ObservableProperty]
    private string _title = string.Empty;

    /// <summary>View → Toolbars → Standard Buttons.</summary>
    [ObservableProperty]
    private bool _showStandardButtons = true;

    /// <summary>View → Toolbars → Address Bar.</summary>
    [ObservableProperty]
    private bool _showAddressBar = true;

    /// <summary>View → Status Bar.</summary>
    [ObservableProperty]
    private bool _showStatusBar = true;

    /// <summary>The Folders pane (the Folders toolbar button).</summary>
    [ObservableProperty]
    private bool _showFolders = true;

    /// <summary>How the list shows its items.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIconsView), nameof(IsListView), nameof(IsDetailsView))]
    private ViewMode _viewMode = ViewMode.Details;

    /// <summary>The dialog shown over the shell (Properties, About), or null.</summary>
    [ObservableProperty]
    private DialogInfo? _dialog;

    /// <summary>The item whose name is being edited, so the view can focus its text box.</summary>
    public event EventHandler<FileItem>? RenameStarted;

    /// <summary>View → Icons.</summary>
    public bool IsIconsView
    {
        get => ViewMode == ViewMode.Icons;
        set => SetViewIf(value, ViewMode.Icons);
    }

    /// <summary>View → List.</summary>
    public bool IsListView
    {
        get => ViewMode == ViewMode.List;
        set => SetViewIf(value, ViewMode.List);
    }

    /// <summary>View → Details.</summary>
    public bool IsDetailsView
    {
        get => ViewMode == ViewMode.Details;
        set => SetViewIf(value, ViewMode.Details);
    }

    private SortHeader SortedBy => Headers.First(h => h.IsAscending || h.IsDescending);

    private bool HasSelection => _selection.Count > 0;

    private bool HasSingleSelection => _selection.Count == 1;

    private bool CanGoBack => _back.Count > 0;

    private bool CanGoForward => _forward.Count > 0;

    private bool CanGoUp => Current.Parent is not null;

    private bool CanPaste => _clipboard.Count > 0;

    /// <summary>Formats a byte count as Explorer's status bar does ("27.0 KB").</summary>
    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} bytes",
        < 1024 * 1024 => (bytes / 1024.0).ToString("0.0#", CultureInfo.CurrentCulture) + " KB",
        _ => (bytes / (1024.0 * 1024.0)).ToString("0.0#", CultureInfo.CurrentCulture) + " MB",
    };

    /// <summary>Finds a node by its backslash path ("Desktop\My Documents"), or by a path below Desktop.</summary>
    public FileNode? Find(string path)
    {
        var parts = path.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var start = string.Equals(parts[0], Root.Name, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var node = Root;
        for (var i = start; i < parts.Length && node is not null; i++)
        {
            node = node.Children.FirstOrDefault(c => string.Equals(c.Name, parts[i], StringComparison.OrdinalIgnoreCase));
        }

        return node;
    }

    /// <summary>Called by the view when the list selection changes.</summary>
    public void SetSelection(IEnumerable<FileItem> items)
    {
        _selection = items.ToList();
        UpdateStatus();
        NotifyCommands();
    }

    /// <summary>Shows a folder.</summary>
    public void Navigate(FileNode folder, bool addToHistory = true)
    {
        if (!folder.IsContainer)
        {
            return;
        }

        if (addToHistory && !ReferenceEquals(folder, Current))
        {
            _back.Push(Current);
            _forward.Clear();
        }

        Show(folder);
    }

    /// <summary>Sorts by a column: the same column toggles the direction, another column sorts ascending.</summary>
    [RelayCommand]
    public void SortBy(string key)
    {
        var header = Headers.First(h => h.Key == key);
        var ascending = !(header.IsAscending);
        foreach (var h in Headers)
        {
            h.IsAscending = false;
            h.IsDescending = false;
        }

        header.IsAscending = ascending;
        header.IsDescending = !ascending;
        Reload();
    }

    /// <summary>Opens an item: a folder is shown, a file shows its properties.</summary>
    [RelayCommand]
    public void Open(FileItem? item)
    {
        item ??= _selection.FirstOrDefault();
        if (item is null)
        {
            return;
        }

        if (item.Node.IsContainer)
        {
            Navigate(item.Node);
        }
        else
        {
            ShowProperties(item.Node);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        _forward.Push(Current);
        Show(_back.Pop());
    }

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private void GoForward()
    {
        _back.Push(Current);
        Show(_forward.Pop());
    }

    [RelayCommand(CanExecute = nameof(CanGoUp))]
    private void GoUp()
    {
        if (Current.Parent is { } parent)
        {
            Navigate(parent);
        }
    }

    [RelayCommand]
    private void Go()
    {
        if (Find(AddressText) is { IsContainer: true } folder)
        {
            Navigate(folder);
        }
        else
        {
            Dialog = new DialogInfo("Address Bar", Icons.Get("folder", 32), $"Cannot find '{AddressText}'.",
                [new DialogRow("Hint:", "Check the spelling and try again.")]);
            AddressText = Current.Path;
        }
    }

    [RelayCommand]
    private void NavigateTo(string path)
    {
        if (Find(path) is { } folder)
        {
            Navigate(folder);
        }
    }

    [RelayCommand]
    private void Refresh() => Reload();

    [RelayCommand]
    private void SetView(ViewMode mode) => ViewMode = mode;

    [RelayCommand]
    private void ToggleFolders() => ShowFolders = !ShowFolders;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Cut() => ToClipboard(cut: true);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Copy() => ToClipboard(cut: false);

    [RelayCommand(CanExecute = nameof(CanPaste))]
    private void Paste()
    {
        foreach (var node in _clipboard)
        {
            if (_clipboardIsCut)
            {
                if (node.Contains(Current) || ReferenceEquals(node.Parent, Current))
                {
                    continue;
                }

                var oldParent = node.Parent;
                Current.Add(node);
                if (oldParent is not null)
                {
                    RootFolder.Find(oldParent)?.Reload();
                }
            }
            else
            {
                var copy = node.Clone();
                copy.Name = UniqueName(ReferenceEquals(node.Parent, Current) ? "Copy of " + node.Name : node.Name);
                Current.Add(copy);
            }
        }

        if (_clipboardIsCut)
        {
            _clipboard.Clear();
        }

        RootFolder.Find(Current)?.Reload();
        Reload();
        NotifyCommands();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        foreach (var item in _selection.ToList())
        {
            item.Node.Remove();
            _clipboard.Remove(item.Node);
        }

        RootFolder.Find(Current)?.Reload();
        Reload();
    }

    [RelayCommand(CanExecute = nameof(HasSingleSelection))]
    private void Rename() => StartRename(_selection[0]);

    [RelayCommand]
    private void Properties()
    {
        ShowProperties(_selection.Count > 0 ? _selection[0].Node : Current);
    }

    [RelayCommand]
    private void NewFolder() => CreateNew(new FileNode(UniqueName("New Folder"), FileKind.Folder, 0, DateTime.Now));

    [RelayCommand]
    private void NewTextDocument() => CreateNew(new FileNode(UniqueName("New Text Document.txt"), FileKind.Text, 0, DateTime.Now));

    [RelayCommand]
    private void About()
    {
        Dialog = new DialogInfo("About AvaWpf Explorer", Icons.Get("computer", 32), "AvaWpf Explorer",
        [
            new DialogRow("Theme:", AvaWpfTheme.Find()?.ActualTheme.ToString() ?? string.Empty),
            new DialogRow("Scheme:", AvaWpfTheme.Find()?.ActualColorScheme ?? string.Empty),
            new DialogRow("About:", "A Windows Explorer look-alike built with Avalonia and AvaWpf, over a fake file system."),
        ]);
    }

    [RelayCommand]
    private void CloseDialog() => Dialog = null;

    [RelayCommand]
    private void Search()
    {
        var found = new List<DialogRow>();
        Collect(Current, found);
        if (found.Count == 0)
        {
            found.Add(new DialogRow(string.Empty, "No files were found."));
        }

        Dialog = new DialogInfo("Search Results", Icons.Get("find", 32), $"Files in {Current.Name}", found.Take(10).ToList());

        static void Collect(FileNode folder, List<DialogRow> rows)
        {
            foreach (var c in folder.Children)
            {
                if (c.IsContainer)
                {
                    Collect(c, rows);
                }
                else
                {
                    rows.Add(new DialogRow(c.Name, c.Parent?.Path ?? string.Empty));
                }
            }
        }
    }

    partial void OnSelectedFolderChanged(FolderItem? value)
    {
        if (!_navigating && value is not null && !ReferenceEquals(value.Node, Current))
        {
            Navigate(value.Node);
        }
    }

    private void SetViewIf(bool value, ViewMode mode)
    {
        if (value)
        {
            ViewMode = mode;
        }
    }

    private void Show(FileNode folder)
    {
        _navigating = true;
        try
        {
            Current = folder;
            Title = folder.Name;
            AddressText = folder.Path;
            if (!AddressHistory.Contains(folder.Path))
            {
                AddressHistory.Add(folder.Path);
            }

            // Expand the path in the Folders pane and select the folder.
            for (var p = folder.Parent; p is not null; p = p.Parent)
            {
                if (RootFolder.Find(p) is { } item)
                {
                    item.IsExpanded = true;
                }
            }

            SelectedFolder = RootFolder.Find(folder);
            Reload();
        }
        finally
        {
            _navigating = false;
        }

        NotifyCommands();
    }

    private void Reload()
    {
        var header = SortedBy;
        IEnumerable<FileNode> files = header.Key switch
        {
            "Size" => Current.Children.OrderBy(n => n.Size).ThenBy(n => n.Name, StringComparer.CurrentCultureIgnoreCase),
            "Type" => Current.Children.OrderBy(n => n.TypeText, StringComparer.CurrentCultureIgnoreCase).ThenBy(n => n.Name, StringComparer.CurrentCultureIgnoreCase),
            "Modified" => Current.Children.OrderBy(n => n.Modified),
            _ => Current.Children.OrderBy(n => n.Name, StringComparer.CurrentCultureIgnoreCase),
        };
        if (header.IsDescending)
        {
            files = files.Reverse();
        }

        Items.Clear();

        // Explorer keeps folders before files in both directions.
        foreach (var node in files.OrderBy(n => n.IsContainer ? 0 : 1))
        {
            Items.Add(new FileItem(node, OnRenamed) { IsCut = _clipboardIsCut && _clipboard.Contains(node) });
        }

        _selection = Array.Empty<FileItem>();
        UpdateStatus();
        NotifyCommands();
    }

    private void UpdateStatus()
    {
        var shown = _selection.Count > 0 ? _selection : (IReadOnlyList<FileItem>)Items;
        var count = shown.Count;
        StatusText = _selection.Count > 0
            ? $"{count} object{(count == 1 ? string.Empty : "s")} selected"
            : $"{count} object{(count == 1 ? string.Empty : "s")}";
        var bytes = shown.Where(i => !i.Node.IsContainer).Sum(i => i.Node.Size);
        StatusSize = FormatSize(bytes);
    }

    private void NotifyCommands()
    {
        GoBackCommand.NotifyCanExecuteChanged();
        GoForwardCommand.NotifyCanExecuteChanged();
        GoUpCommand.NotifyCanExecuteChanged();
        CutCommand.NotifyCanExecuteChanged();
        CopyCommand.NotifyCanExecuteChanged();
        PasteCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
    }

    private void ToClipboard(bool cut)
    {
        _clipboard.Clear();
        _clipboard.AddRange(_selection.Select(i => i.Node));
        _clipboardIsCut = cut;
        foreach (var item in Items)
        {
            item.IsCut = cut && _clipboard.Contains(item.Node);
        }

        NotifyCommands();
    }

    private void CreateNew(FileNode node)
    {
        Current.Add(node);
        RootFolder.Find(Current)?.Reload();
        Reload();
        if (Items.FirstOrDefault(i => ReferenceEquals(i.Node, node)) is { } item)
        {
            StartRename(item);
        }
    }

    private void StartRename(FileItem item)
    {
        item.BeginRename();
        RenameStarted?.Invoke(this, item);
    }

    private void OnRenamed(FileItem item)
    {
        if (item.Node.IsContainer)
        {
            RootFolder.Find(Current)?.Reload();
        }

        Reload();
    }

    private string UniqueName(string name)
    {
        var stem = System.IO.Path.GetFileNameWithoutExtension(name);
        var ext = System.IO.Path.GetExtension(name);
        var candidate = name;
        for (var i = 2; Current.Children.Any(c => string.Equals(c.Name, candidate, StringComparison.OrdinalIgnoreCase)); i++)
        {
            candidate = $"{stem} ({i}){ext}";
        }

        return candidate;
    }

    private void ShowProperties(FileNode node)
    {
        var rows = new List<DialogRow>
        {
            new("Type:", node.TypeText),
            new("Location:", node.Parent?.Path ?? string.Empty),
        };
        if (node.IsContainer)
        {
            var (files, folders, bytes) = Measure(node);
            rows.Add(new("Size:", FormatSize(bytes)));
            rows.Add(new("Contains:", $"{files} Files, {folders} Folders"));
        }
        else
        {
            rows.Add(new("Size:", $"{FormatSize(node.Size)} ({node.Size:N0} bytes)"));
        }

        rows.Add(new("Modified:", node.Modified.ToString("D", CultureInfo.CurrentCulture) + ", " + node.Modified.ToString("t", CultureInfo.CurrentCulture)));
        Dialog = new DialogInfo($"{node.Name} Properties", Icons.Get(node.IconName, 32), node.Name, rows);
    }

    private static (int Files, int Folders, long Bytes) Measure(FileNode node)
    {
        int files = 0, folders = 0;
        long bytes = 0;
        foreach (var c in node.Children)
        {
            if (c.IsContainer)
            {
                var (f, d, b) = Measure(c);
                files += f;
                folders += d + 1;
                bytes += b;
            }
            else
            {
                files++;
                bytes += c.Size;
            }
        }

        return (files, folders, bytes);
    }
}
