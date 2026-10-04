// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonGallery.cs (MIT, see NOTICE.md).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>
/// A gallery of <see cref="RibbonGalleryCategory"/>s of <see cref="RibbonGalleryItem"/>s in columns, with one selected item.
/// </summary>
/// <remarks>
/// Hovering an item raises <see cref="PreviewRequestedEvent"/>; the application does the preview. Choosing an item sets
/// <see cref="SelectedItem"/>, raises <see cref="SelectionChangedEvent"/>, runs <see cref="Command"/> and closes the popup.
/// </remarks>
public class RibbonGallery : ItemsControl
{
    /// <summary>Defines the <see cref="SelectedItem"/> property.</summary>
    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<RibbonGallery, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="HighlightedItem"/> property.</summary>
    public static readonly DirectProperty<RibbonGallery, object?> HighlightedItemProperty =
        AvaloniaProperty.RegisterDirect<RibbonGallery, object?>(nameof(HighlightedItem), o => o.HighlightedItem);

    /// <summary>Defines the <see cref="Command"/> property.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<RibbonGallery, ICommand?>(nameof(Command));

    /// <summary>Defines the <see cref="CommandParameter"/> property.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<RibbonGallery, object?>(nameof(CommandParameter));

    /// <summary>Defines the <see cref="CanUserFilter"/> property.</summary>
    public static readonly StyledProperty<bool> CanUserFilterProperty =
        AvaloniaProperty.Register<RibbonGallery, bool>(nameof(CanUserFilter));

    /// <summary>Defines the <see cref="FilterCategory"/> property.</summary>
    public static readonly StyledProperty<RibbonGalleryCategory?> FilterCategoryProperty =
        AvaloniaProperty.Register<RibbonGallery, RibbonGalleryCategory?>(nameof(FilterCategory));

    /// <summary>Defines the <see cref="MinColumnCount"/> property.</summary>
    public static readonly StyledProperty<int> MinColumnCountProperty =
        AvaloniaProperty.Register<RibbonGallery, int>(nameof(MinColumnCount), 1);

    /// <summary>Defines the <see cref="MaxColumnCount"/> property.</summary>
    public static readonly StyledProperty<int> MaxColumnCountProperty =
        AvaloniaProperty.Register<RibbonGallery, int>(nameof(MaxColumnCount), int.MaxValue);

    /// <summary>Defines the <see cref="GalleryItemTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> GalleryItemTemplateProperty =
        AvaloniaProperty.Register<RibbonGallery, IDataTemplate?>(nameof(GalleryItemTemplate));

    /// <summary>Defines the <see cref="HorizontalContentAlignment"/> property.</summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<RibbonGallery>(new StyledPropertyMetadata<HorizontalAlignment>(HorizontalAlignment.Stretch));

    /// <summary>Defines the <see cref="VerticalContentAlignment"/> property.</summary>
    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<RibbonGallery>(new StyledPropertyMetadata<VerticalAlignment>(VerticalAlignment.Center));

    /// <summary>Defines the <see cref="CanAddToQuickAccessToolBarDirectly"/> property.</summary>
    public static readonly AttachedProperty<bool> CanAddToQuickAccessToolBarDirectlyProperty = RibbonControlService.CanAddToQuickAccessToolBarDirectlyProperty.AddOwner<RibbonGallery>();

    /// <summary>Defines the <see cref="QuickAccessToolBarId"/> property.</summary>
    public static readonly AttachedProperty<object?> QuickAccessToolBarIdProperty = RibbonControlService.QuickAccessToolBarIdProperty.AddOwner<RibbonGallery>();

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonGallery>();

    /// <summary>Raised when <see cref="SelectedItem"/> changes.</summary>
    public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<RibbonGallery, SelectionChangedEventArgs>(nameof(SelectionChanged), RoutingStrategies.Bubble);

    /// <summary>Raised when an item should be previewed, and when the preview should be reverted.</summary>
    public static readonly RoutedEvent<RibbonGalleryPreviewEventArgs> PreviewRequestedEvent =
        RoutedEvent.Register<RibbonGallery, RibbonGalleryPreviewEventArgs>(nameof(PreviewRequested), RoutingStrategies.Bubble);

    private object? _highlightedItem;

    /// <summary>Raised when <see cref="SelectedItem"/> changes.</summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    /// <summary>Raised when an item should be previewed, and when the preview should be reverted.</summary>
    public event EventHandler<RibbonGalleryPreviewEventArgs>? PreviewRequested
    {
        add => AddHandler(PreviewRequestedEvent, value);
        remove => RemoveHandler(PreviewRequestedEvent, value);
    }

    /// <summary>The selected item: a data item, or a <see cref="RibbonGalleryItem"/> declared directly.</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>The item under the pointer.</summary>
    public object? HighlightedItem
    {
        get => _highlightedItem;
        private set => SetAndRaise(HighlightedItemProperty, ref _highlightedItem, value);
    }

    /// <summary>The command run when the user chooses an item. Its parameter is <see cref="CommandParameter"/>, or the item.</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The parameter of <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Whether a filter button lets the user show one category at a time.</summary>
    public bool CanUserFilter
    {
        get => GetValue(CanUserFilterProperty);
        set => SetValue(CanUserFilterProperty, value);
    }

    /// <summary>The only category shown. Null shows all.</summary>
    public RibbonGalleryCategory? FilterCategory
    {
        get => GetValue(FilterCategoryProperty);
        set => SetValue(FilterCategoryProperty, value);
    }

    /// <summary>The fewest item columns. Default 1.</summary>
    public int MinColumnCount
    {
        get => GetValue(MinColumnCountProperty);
        set => SetValue(MinColumnCountProperty, value);
    }

    /// <summary>The most item columns. Default unlimited.</summary>
    public int MaxColumnCount
    {
        get => GetValue(MaxColumnCountProperty);
        set => SetValue(MaxColumnCountProperty, value);
    }

    /// <summary>The horizontal alignment of the item content, passed to the categories and their items. Default stretch.</summary>
    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    /// <summary>The vertical alignment of the item content, passed to the categories and their items. Default center.</summary>
    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    /// <summary>The template of the data items inside the categories.</summary>
    public IDataTemplate? GalleryItemTemplate
    {
        get => GetValue(GalleryItemTemplateProperty);
        set => SetValue(GalleryItemTemplateProperty, value);
    }

    /// <summary>Whether the context menu offers "Add to Quick Access Toolbar". Default true.</summary>
    public bool CanAddToQuickAccessToolBarDirectly
    {
        get => GetValue(CanAddToQuickAccessToolBarDirectlyProperty);
        set => SetValue(CanAddToQuickAccessToolBarDirectlyProperty, value);
    }

    /// <summary>The identity in the Quick Access Toolbar.</summary>
    public object? QuickAccessToolBarId
    {
        get => GetValue(QuickAccessToolBarIdProperty);
        set => SetValue(QuickAccessToolBarIdProperty, value);
    }

    /// <summary>The Ribbon the gallery is in.</summary>
    public Ribbon? Ribbon => GetValue(RibbonProperty);

    /// <summary>The categories (the realized category containers).</summary>
    public IEnumerable<RibbonGalleryCategory> Categories
    {
        get
        {
            foreach (var container in GetRealizedContainers())
            {
                if (container is RibbonGalleryCategory category)
                {
                    yield return category;
                }
            }
        }
    }

    /// <summary>Chooses an item as the user would: selects it, runs the command and closes the popup.</summary>
    /// <param name="item">The item container.</param>
    internal void SelectFromUser(RibbonGalleryItem item)
    {
        var value = item.Item;
        SetCurrentValue(SelectedItemProperty, value);

        EndPreview();
        var parameter = CommandParameter ?? value;
        if (Command is { } command && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }

        RaiseEvent(new RibbonDismissPopupEventArgs());
    }

    /// <summary>Tracks the highlighted item and raises the preview requests.</summary>
    internal void OnItemHighlighted(RibbonGalleryItem item, bool isHighlighted)
    {
        var value = item.Item;
        if (isHighlighted)
        {
            HighlightedItem = value;
            RaiseEvent(new RibbonGalleryPreviewEventArgs(value, true));
        }
        else if (Equals(HighlightedItem, value))
        {
            EndPreview();
        }
    }

    /// <summary>Ends the current preview, if any.</summary>
    internal void EndPreview()
    {
        if (HighlightedItem is { } previous)
        {
            HighlightedItem = null;
            RaiseEvent(new RibbonGalleryPreviewEventArgs(previous, false));
        }
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<RibbonGalleryCategory>(item, out recycleKey);

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new RibbonGalleryCategory();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is RibbonGalleryCategory category && container != item)
        {
            category.Header = item;
            if (item is IEnumerable children and not string)
            {
                category.ItemsSource = children;
            }
        }

        if (container is RibbonGalleryCategory c)
        {
            c.IsFilteredOut = FilterCategory is not null && FilterCategory != c;
            PassContentAlignment(c);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedItemProperty)
        {
            UpdateSelectedContainers();
            RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, change.OldValue is null ? Array.Empty<object>() : new[] { change.OldValue }, change.NewValue is null ? Array.Empty<object>() : new[] { change.NewValue }));
        }
        else if (change.Property == HorizontalContentAlignmentProperty || change.Property == VerticalContentAlignmentProperty)
        {
            foreach (var category in Categories)
            {
                PassContentAlignment(category);
            }
        }
        else if (change.Property == FilterCategoryProperty)
        {
            foreach (var category in Categories)
            {
                category.IsFilteredOut = FilterCategory is not null && FilterCategory != category;
            }
        }
    }

    // As the WPF setters bound to the ancestor ItemsControl: below a local value or a style on the category.
    private void PassContentAlignment(RibbonGalleryCategory category)
    {
        category.SetValue(RibbonGalleryCategory.HorizontalContentAlignmentProperty, HorizontalContentAlignment, BindingPriority.Template);
        category.SetValue(RibbonGalleryCategory.VerticalContentAlignmentProperty, VerticalContentAlignment, BindingPriority.Template);
    }

    private void UpdateSelectedContainers()
    {
        var selected = SelectedItem;
        foreach (var category in Categories)
        {
            foreach (var container in category.GetRealizedContainers())
            {
                if (container is RibbonGalleryItem galleryItem)
                {
                    galleryItem.IsSelected = selected is not null && Equals(galleryItem.Item, selected);
                }
            }
        }
    }

    /// <summary>The realized item containers of every category.</summary>
    internal IEnumerable<RibbonGalleryItem> AllItemContainers()
    {
        var result = new List<RibbonGalleryItem>();
        foreach (var category in Categories)
        {
            foreach (var container in category.GetRealizedContainers())
            {
                if (container is RibbonGalleryItem galleryItem)
                {
                    result.Add(galleryItem);
                }
            }
        }

        return result;
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonGalleryAutomationPeer(this);
}
