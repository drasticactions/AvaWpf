using System;
using System.Collections.Generic;

namespace AvaWpf.Samples;

/// <summary>A file or folder of the sample file system.</summary>
public sealed class FileNode
{
    /// <summary>Initializes a node.</summary>
    public FileNode(string name, FileKind kind, long size = 0, DateTime? modified = null)
    {
        Name = name;
        Kind = kind;
        Size = size;
        Modified = modified ?? new DateTime(2006, 8, 25, 10, 30, 0);
    }

    /// <summary>The name (settable, so the Explorer demo can rename).</summary>
    public string Name { get; set; }

    /// <summary>What the node is.</summary>
    public FileKind Kind { get; }

    /// <summary>The size in bytes (0 for folders).</summary>
    public long Size { get; }

    /// <summary>The last write time.</summary>
    public DateTime Modified { get; }

    /// <summary>The parent folder, or null for a root.</summary>
    public FileNode? Parent { get; private set; }

    /// <summary>The children of a folder.</summary>
    public List<FileNode> Children { get; } = new();

    /// <summary>True for folders, drives and the computer.</summary>
    public bool IsContainer => Kind is FileKind.Folder or FileKind.Drive or FileKind.Computer or FileKind.Network or FileKind.RecycleBin;

    /// <summary>The child folders.</summary>
    public IEnumerable<FileNode> Folders
    {
        get
        {
            foreach (var c in Children)
            {
                if (c.IsContainer)
                {
                    yield return c;
                }
            }
        }
    }

    /// <summary>The size as Explorer shows it ("12 KB"), empty for folders.</summary>
    public string SizeText => IsContainer ? string.Empty : $"{Math.Max(1, (Size + 1023) / 1024):N0} KB";

    /// <summary>The type as Explorer shows it.</summary>
    public string TypeText => Kind switch
    {
        FileKind.Folder => "File Folder",
        FileKind.Drive => "Local Disk",
        FileKind.Computer => "System Folder",
        FileKind.Network => "System Folder",
        FileKind.RecycleBin => "Recycle Bin",
        FileKind.Text => "Text Document",
        FileKind.Image => "Bitmap Image",
        _ => System.IO.Path.GetExtension(Name).TrimStart('.').ToUpperInvariant() + " File",
    };

    /// <summary>The last write time as Explorer shows it.</summary>
    public string ModifiedText => Modified.ToString("g", System.Globalization.CultureInfo.CurrentCulture);

    /// <summary>The icon name (<see cref="SampleImages.Names"/>).</summary>
    public string IconName => Kind switch
    {
        FileKind.Folder => "folder",
        FileKind.Drive => "drive",
        FileKind.Computer => "computer",
        FileKind.Network => "network",
        FileKind.RecycleBin => "recycle",
        FileKind.Text => "text",
        FileKind.Image => "image",
        _ => "file",
    };

    /// <summary>The path from the root, joined with backslashes.</summary>
    public string Path => Parent is null ? Name : Parent.Path + "\\" + Name;

    /// <summary>Adds a child and returns it.</summary>
    public FileNode Add(FileNode child)
    {
        child.Parent?.Children.Remove(child);
        child.Parent = this;
        Children.Add(child);
        return child;
    }

    /// <summary>Removes this node from its parent.</summary>
    public void Remove()
    {
        Parent?.Children.Remove(this);
        Parent = null;
    }

    /// <summary>Returns a deep copy of this node, without a parent.</summary>
    public FileNode Clone()
    {
        var copy = new FileNode(Name, Kind, Size, Modified);
        foreach (var c in Children)
        {
            copy.Add(c.Clone());
        }

        return copy;
    }

    /// <summary>True when <paramref name="node"/> is this node or one of its descendants.</summary>
    public bool Contains(FileNode node)
    {
        for (var n = node; n is not null; n = n.Parent)
        {
            if (ReferenceEquals(n, this))
            {
                return true;
            }
        }

        return false;
    }
}
