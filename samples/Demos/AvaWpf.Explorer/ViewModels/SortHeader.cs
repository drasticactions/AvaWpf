using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaWpf.Explorer.ViewModels;

/// <summary>The header of a GridView column: its text, its sort key, and the sort arrow it shows.</summary>
public sealed partial class SortHeader : ObservableObject
{
    /// <summary>Initializes a header.</summary>
    public SortHeader(string key, string text)
    {
        Key = key;
        Text = text;
    }

    /// <summary>The sort key ("Name", "Size", "Type" or "Modified").</summary>
    public string Key { get; }

    /// <summary>The header text.</summary>
    public string Text { get; }

    /// <summary>True when the list is sorted by this column, ascending.</summary>
    [ObservableProperty]
    private bool _isAscending;

    /// <summary>True when the list is sorted by this column, descending.</summary>
    [ObservableProperty]
    private bool _isDescending;
}
