// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/InRibbonGallery.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using AvaWpf.Ribbon.Primitives;

namespace AvaWpf.Ribbon;

/// <summary>
/// A gallery shown in the Ribbon itself: one row of items with up and down scroll buttons, and an arrow that opens the
/// whole gallery in a drop-down. Its item is a <see cref="RibbonGallery"/>.
/// </summary>
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
[TemplatePart("PART_InRibbonHost", typeof(RibbonScrollHost))]
[TemplatePart("PART_PopupHost", typeof(Decorator))]
[TemplatePart("PART_LineUpButton", typeof(Button))]
[TemplatePart("PART_LineDownButton", typeof(Button))]
public class InRibbonGallery : RibbonMenuButton
{
    /// <summary>Defines the <see cref="MinColumnCount"/> property.</summary>
    public static readonly StyledProperty<int> MinColumnCountProperty =
        AvaloniaProperty.Register<InRibbonGallery, int>(nameof(MinColumnCount), 1);

    /// <summary>Defines the <see cref="MaxColumnCount"/> property.</summary>
    public static readonly StyledProperty<int> MaxColumnCountProperty =
        AvaloniaProperty.Register<InRibbonGallery, int>(nameof(MaxColumnCount), 6);

    private ItemsPresenter? _itemsPresenter;
    private RibbonScrollHost? _inRibbonHost;
    private Decorator? _popupHost;
    private Button? _lineUp;
    private Button? _lineDown;

    /// <summary>The fewest columns in the Ribbon. Default 1.</summary>
    public int MinColumnCount
    {
        get => GetValue(MinColumnCountProperty);
        set => SetValue(MinColumnCountProperty, value);
    }

    /// <summary>The most columns in the Ribbon. Default 6.</summary>
    public int MaxColumnCount
    {
        get => GetValue(MaxColumnCountProperty);
        set => SetValue(MaxColumnCountProperty, value);
    }

    /// <summary>The gallery (the first item that is a <see cref="RibbonGallery"/>).</summary>
    public RibbonGallery? Gallery
    {
        get
        {
            foreach (var item in Items)
            {
                if (item is RibbonGallery gallery)
                {
                    return gallery;
                }
            }

            return null;
        }
    }

    /// <summary>Scrolls the in-ribbon row up by one row.</summary>
    public void LineUp() => _inRibbonHost?.ScrollBy(-RowHeight());

    /// <summary>Scrolls the in-ribbon row down by one row.</summary>
    public void LineDown() => _inRibbonHost?.ScrollBy(RowHeight());

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_lineUp is not null)
        {
            _lineUp.Click -= OnLineUp;
        }

        if (_lineDown is not null)
        {
            _lineDown.Click -= OnLineDown;
        }

        base.OnApplyTemplate(e);
        _itemsPresenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
        _inRibbonHost = e.NameScope.Find<RibbonScrollHost>("PART_InRibbonHost");
        _popupHost = e.NameScope.Find<Decorator>("PART_PopupHost");
        _lineUp = e.NameScope.Find<Button>("PART_LineUpButton");
        _lineDown = e.NameScope.Find<Button>("PART_LineDownButton");
        if (_lineUp is not null)
        {
            _lineUp.Click += OnLineUp;
        }

        if (_lineDown is not null)
        {
            _lineDown.Click += OnLineDown;
        }

        MovePresenter();
    }

    /// <inheritdoc/>
    protected override void OnIsDropDownOpenChanged(bool isOpen)
    {
        base.OnIsDropDownOpenChanged(isOpen);
        MovePresenter();
        if (!isOpen)
        {
            Gallery?.EndPreview();
        }
    }

    /// <summary>Puts the gallery in the drop-down while it is open, and in the Ribbon row otherwise.</summary>
    private void MovePresenter()
    {
        if (_itemsPresenter is null || _inRibbonHost is null || _popupHost is null)
        {
            return;
        }

        Decorator target = IsDropDownOpen ? _popupHost : _inRibbonHost;
        Decorator other = IsDropDownOpen ? _inRibbonHost : _popupHost;
        if (target.Child == _itemsPresenter)
        {
            return;
        }

        other.Child = null;
        target.Child = _itemsPresenter;
    }

    private double RowHeight()
    {
        if (Gallery is { } gallery)
        {
            foreach (var item in gallery.AllItemContainers())
            {
                if (item.Bounds.Height > 0)
                {
                    return item.Bounds.Height;
                }
            }
        }

        return 20;
    }

    private void OnLineUp(object? sender, RoutedEventArgs e) => LineUp();

    private void OnLineDown(object? sender, RoutedEventArgs e) => LineDown();
}
