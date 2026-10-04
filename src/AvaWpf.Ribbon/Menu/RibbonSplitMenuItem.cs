// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonSplitMenuItem.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace AvaWpf.Ribbon;

/// <summary>
/// A menu item split in two: the header runs the item's command and closes the menus, the arrow opens the submenu.
/// </summary>
[TemplatePart("PART_HeaderButton", typeof(Button))]
public class RibbonSplitMenuItem : RibbonMenuItem, IKeyTipSiblingProvider
{
    /// <summary>Defines the <see cref="HeaderKeyTip"/> property.</summary>
    public static readonly StyledProperty<string?> HeaderKeyTipProperty =
        AvaloniaProperty.Register<RibbonSplitMenuItem, string?>(nameof(HeaderKeyTip));

    /// <summary>Defines the <see cref="HeaderQuickAccessToolBarId"/> property.</summary>
    public static readonly StyledProperty<object?> HeaderQuickAccessToolBarIdProperty =
        AvaloniaProperty.Register<RibbonSplitMenuItem, object?>(nameof(HeaderQuickAccessToolBarId));

    /// <summary>Defines the <see cref="DropDownToolTipTitle"/> property.</summary>
    public static readonly StyledProperty<string?> DropDownToolTipTitleProperty =
        AvaloniaProperty.Register<RibbonSplitMenuItem, string?>(nameof(DropDownToolTipTitle));

    /// <summary>Defines the <see cref="DropDownToolTipDescription"/> property.</summary>
    public static readonly StyledProperty<string?> DropDownToolTipDescriptionProperty =
        AvaloniaProperty.Register<RibbonSplitMenuItem, string?>(nameof(DropDownToolTipDescription));

    private Button? _headerButton;

    /// <summary>The KeyTip of the header half; the item's own KeyTip opens the submenu.</summary>
    public string? HeaderKeyTip
    {
        get => GetValue(HeaderKeyTipProperty);
        set => SetValue(HeaderKeyTipProperty, value);
    }

    /// <summary>The Quick Access Toolbar identity of the header half.</summary>
    public object? HeaderQuickAccessToolBarId
    {
        get => GetValue(HeaderQuickAccessToolBarIdProperty);
        set => SetValue(HeaderQuickAccessToolBarIdProperty, value);
    }

    /// <summary>The tool tip title of the arrow half.</summary>
    public string? DropDownToolTipTitle
    {
        get => GetValue(DropDownToolTipTitleProperty);
        set => SetValue(DropDownToolTipTitleProperty, value);
    }

    /// <summary>The tool tip description of the arrow half.</summary>
    public string? DropDownToolTipDescription
    {
        get => GetValue(DropDownToolTipDescriptionProperty);
        set => SetValue(DropDownToolTipDescriptionProperty, value);
    }

    IEnumerable<Control> IKeyTipSiblingProvider.KeyTipSiblings
    {
        get
        {
            if (_headerButton is not null)
            {
                yield return _headerButton;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_headerButton is not null)
        {
            _headerButton.Click -= OnHeaderClick;
            _headerButton.RemoveHandler(KeyTipService.KeyTipAccessedEvent, OnHeaderKeyTipAccessed);
        }

        base.OnApplyTemplate(e);
        _headerButton = e.NameScope.Find<Button>("PART_HeaderButton");
        if (_headerButton is not null)
        {
            _headerButton.Click += OnHeaderClick;
            _headerButton.AddHandler(KeyTipService.KeyTipAccessedEvent, OnHeaderKeyTipAccessed);
        }
    }

    private void InvokeHeader()
    {
        if (ToggleType == MenuItemToggleType.CheckBox)
        {
            IsChecked = !IsChecked;
        }

        RaiseEvent(new RoutedEventArgs(ClickEvent));
        RaiseEvent(new RibbonDismissPopupEventArgs());
    }

    private void OnHeaderClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        InvokeHeader();
    }

    private void OnHeaderKeyTipAccessed(object? sender, KeyTipAccessedEventArgs e)
    {
        if (e.Source == _headerButton)
        {
            InvokeHeader();
            e.Handled = true;
        }
    }
}
