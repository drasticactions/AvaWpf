// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonFilterMenuButton.cs (MIT, see NOTICE.md).
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaWpf.Ribbon;

/// <summary>
/// The filter button of a <see cref="RibbonGallery"/> with <see cref="RibbonGallery.CanUserFilter"/>: its menu lists
/// "All" and every category; choosing one shows only that category. Its label shows the current filter.
/// </summary>
public class RibbonFilterMenuButton : RibbonMenuButton
{
    /// <summary>The text of the item that shows every category.</summary>
    public const string AllFilterText = "All";

    /// <summary>The gallery the button filters: its templated parent.</summary>
    public RibbonGallery? Gallery => TemplatedParent as RibbonGallery;

    /// <summary>Fills the menu from the gallery's categories before it opens.</summary>
    /// <param name="isOpen">The new state.</param>
    protected override void OnIsDropDownOpenChanged(bool isOpen)
    {
        if (isOpen && Gallery is { } gallery)
        {
            Items.Clear();
            Items.Add(CreateItem(AllFilterText, null, gallery.FilterCategory is null));
            foreach (var category in gallery.Categories)
            {
                Items.Add(CreateItem(category.Header, category, gallery.FilterCategory == category));
            }
        }

        base.OnIsDropDownOpenChanged(isOpen);
    }

    /// <summary>Shows the current filter in the label.</summary>
    internal void UpdateLabel()
    {
        Label = Gallery?.FilterCategory?.Header?.ToString() ?? AllFilterText;
    }

    private RibbonMenuItem CreateItem(object? header, RibbonGalleryCategory? category, bool isChecked)
    {
        var item = new RibbonMenuItem { Header = header, ToggleType = MenuItemToggleType.Radio, IsChecked = isChecked };
        item.Click += (_, e) => OnFilterChosen(category, e);
        return item;
    }

    private void OnFilterChosen(RibbonGalleryCategory? category, RoutedEventArgs e)
    {
        if (Gallery is { } gallery)
        {
            gallery.FilterCategory = category;
        }

        UpdateLabel();
        e.Handled = true;
    }
}
