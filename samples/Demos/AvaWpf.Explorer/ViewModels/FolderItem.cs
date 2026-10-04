using System.Collections.ObjectModel;
using Avalonia.Media;
using AvaWpf.Samples;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaWpf.Explorer.ViewModels;

/// <summary>A folder of the Folders pane. Its child folders are created the first time the tree asks for them.</summary>
public sealed partial class FolderItem : ObservableObject
{
    private ObservableCollection<FolderItem>? _children;

    /// <summary>Initializes a folder item.</summary>
    public FolderItem(FileNode node, FolderItem? parent)
    {
        Node = node;
        Parent = parent;
    }

    /// <summary>The folder.</summary>
    public FileNode Node { get; }

    /// <summary>The parent item, or null for the root.</summary>
    public FolderItem? Parent { get; }

    /// <summary>The folder name.</summary>
    public string Name => Node.Name;

    /// <summary>The 16 px icon.</summary>
    public IImage Icon => Icons.Get(Node.IconName);

    /// <summary>Whether the tree item is expanded.</summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>The child folders, created on first use.</summary>
    public ObservableCollection<FolderItem> Children
    {
        get
        {
            if (_children is null)
            {
                _children = new ObservableCollection<FolderItem>();
                foreach (var f in Node.Folders)
                {
                    _children.Add(new FolderItem(f, this));
                }
            }

            return _children;
        }
    }

    /// <summary>Finds the item of <paramref name="node"/> in this subtree, creating children on the way.</summary>
    public FolderItem? Find(FileNode node)
    {
        if (ReferenceEquals(node, Node))
        {
            return this;
        }

        if (!Node.Contains(node))
        {
            return null;
        }

        foreach (var c in Children)
        {
            if (c.Find(node) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Re-reads the child folders (after a new folder, a delete, a paste or a rename).</summary>
    public void Reload()
    {
        OnPropertyChanged(nameof(Name));
        if (_children is null)
        {
            return;
        }

        var index = 0;
        foreach (var f in Node.Folders)
        {
            var existing = index < _children.Count && ReferenceEquals(_children[index].Node, f) ? _children[index] : null;
            if (existing is null)
            {
                _children.Insert(index, new FolderItem(f, this));
            }
            else
            {
                existing.Reload();
            }

            index++;
        }

        while (_children.Count > index)
        {
            _children.RemoveAt(_children.Count - 1);
        }
    }
}
