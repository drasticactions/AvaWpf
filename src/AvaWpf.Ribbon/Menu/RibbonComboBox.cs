// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonComboBox.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>
/// A Ribbon combo box: a selection box (editable with <see cref="IsEditable"/>) and an arrow that opens a drop-down
/// whose item is a <see cref="RibbonGallery"/>. Choosing a gallery item shows it in the box.
/// </summary>
[TemplatePart("PART_EditableTextBox", typeof(TextBox))]
[PseudoClasses(":editable")]
public class RibbonComboBox : RibbonMenuButton
{
    /// <summary>Defines the <see cref="IsEditable"/> property.</summary>
    public static readonly StyledProperty<bool> IsEditableProperty =
        AvaloniaProperty.Register<RibbonComboBox, bool>(nameof(IsEditable));

    /// <summary>Defines the <see cref="IsReadOnly"/> property.</summary>
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<RibbonComboBox, bool>(nameof(IsReadOnly));

    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<RibbonComboBox, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="SelectionBoxItem"/> property.</summary>
    public static readonly DirectProperty<RibbonComboBox, object?> SelectionBoxItemProperty =
        AvaloniaProperty.RegisterDirect<RibbonComboBox, object?>(nameof(SelectionBoxItem), o => o.SelectionBoxItem);

    /// <summary>Defines the <see cref="SelectionBoxWidth"/> property.</summary>
    public static readonly StyledProperty<double> SelectionBoxWidthProperty =
        AvaloniaProperty.Register<RibbonComboBox, double>(nameof(SelectionBoxWidth), double.NaN);

    /// <summary>Defines the <see cref="StaysOpenOnEdit"/> property.</summary>
    public static readonly StyledProperty<bool> StaysOpenOnEditProperty =
        AvaloniaProperty.Register<RibbonComboBox, bool>(nameof(StaysOpenOnEdit));

    private object? _selectionBoxItem;

    static RibbonComboBox()
    {
        RibbonGallery.SelectionChangedEvent.AddClassHandler<RibbonComboBox>((c, e) => c.OnGallerySelectionChanged(e));
    }

    /// <summary>Whether the user can type in the selection box.</summary>
    public bool IsEditable
    {
        get => GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    /// <summary>Whether the editable box is read-only.</summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>The text of the selection box.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>The item shown in the selection box: the gallery's selected item.</summary>
    public object? SelectionBoxItem
    {
        get => _selectionBoxItem;
        private set => SetAndRaise(SelectionBoxItemProperty, ref _selectionBoxItem, value);
    }

    /// <summary>The width of the selection box. NaN sizes it to the content.</summary>
    public double SelectionBoxWidth
    {
        get => GetValue(SelectionBoxWidthProperty);
        set => SetValue(SelectionBoxWidthProperty, value);
    }

    /// <summary>Whether the drop-down stays open while the user types.</summary>
    public bool StaysOpenOnEdit
    {
        get => GetValue(StaysOpenOnEditProperty);
        set => SetValue(StaysOpenOnEditProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEditableProperty)
        {
            PseudoClasses.Set(":editable", change.GetNewValue<bool>());
        }
    }

    private void OnGallerySelectionChanged(SelectionChangedEventArgs e)
    {
        if (e.Source is not RibbonGallery gallery)
        {
            return;
        }

        var item = gallery.SelectedItem;
        SelectionBoxItem = item is RibbonGalleryItem container ? container.Content : item;
        SetCurrentValue(TextProperty, SelectionBoxItem?.ToString());
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonComboBoxAutomationPeer(this);
}
