namespace AvaWpf.Explorer.ViewModels;

/// <summary>How the file list shows its items (View menu).</summary>
public enum ViewMode
{
    /// <summary>Large icons in rows.</summary>
    Icons,

    /// <summary>Small icons in columns.</summary>
    List,

    /// <summary>A GridView with Name, Size, Type and Date Modified columns.</summary>
    Details,
}
