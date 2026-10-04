namespace AvaWpf.Samples;

/// <summary>The kind of a <see cref="FileNode"/>.</summary>
public enum FileKind
{
    /// <summary>A folder.</summary>
    Folder,

    /// <summary>A disk drive.</summary>
    Drive,

    /// <summary>My Computer.</summary>
    Computer,

    /// <summary>Network Places.</summary>
    Network,

    /// <summary>The Recycle Bin.</summary>
    RecycleBin,

    /// <summary>A text document.</summary>
    Text,

    /// <summary>An image.</summary>
    Image,

    /// <summary>Any other file.</summary>
    Other,
}
