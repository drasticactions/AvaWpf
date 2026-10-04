using System;
using Avalonia.Media;
using AvaWpf.Samples;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaWpf.Explorer.ViewModels;

/// <summary>A row of the file list.</summary>
public sealed partial class FileItem : ObservableObject
{
    private readonly Action<FileItem> _renamed;

    /// <summary>Initializes a row.</summary>
    /// <param name="node">The file or folder.</param>
    /// <param name="renamed">Called after a rename was committed.</param>
    public FileItem(FileNode node, Action<FileItem> renamed)
    {
        Node = node;
        _renamed = renamed;
    }

    /// <summary>The file or folder.</summary>
    public FileNode Node { get; }

    /// <summary>The name.</summary>
    public string Name => Node.Name;

    /// <summary>The size column text.</summary>
    public string SizeText => Node.SizeText;

    /// <summary>The type column text.</summary>
    public string TypeText => Node.TypeText;

    /// <summary>The date modified column text.</summary>
    public string ModifiedText => Node.ModifiedText;

    /// <summary>The 16 px icon (Details and List views).</summary>
    public IImage SmallIcon => Icons.Get(Node.IconName);

    /// <summary>The 32 px icon (Icons view).</summary>
    public IImage LargeIcon => Icons.Get(Node.IconName, 32);

    /// <summary>True while the name is edited in place.</summary>
    [ObservableProperty]
    private bool _isRenaming;

    /// <summary>True while the item is cut to the clipboard (drawn faded, as Explorer does).</summary>
    [ObservableProperty]
    private bool _isCut;

    /// <summary>The name being typed while <see cref="IsRenaming"/>.</summary>
    [ObservableProperty]
    private string _editName = string.Empty;

    /// <summary>Starts editing the name.</summary>
    public void BeginRename()
    {
        EditName = Name;
        IsRenaming = true;
    }

    /// <summary>Applies the typed name.</summary>
    [RelayCommand]
    public void CommitRename()
    {
        if (!IsRenaming)
        {
            return;
        }

        IsRenaming = false;
        var name = EditName.Trim();
        if (name.Length == 0 || name == Name || name.IndexOfAny(['\\', '/', ':', '*', '?', '"', '<', '>', '|']) >= 0)
        {
            return;
        }

        Node.Name = name;
        OnPropertyChanged(nameof(Name));
        _renamed(this);
    }

    /// <summary>Drops the typed name.</summary>
    [RelayCommand]
    public void CancelRename() => IsRenaming = false;
}
