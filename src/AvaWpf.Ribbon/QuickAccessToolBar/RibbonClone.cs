// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonHelper.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaWpf.Ribbon;

/// <summary>
/// Makes the default Quick Access Toolbar copy of a Ribbon control (WPF <c>RibbonHelper.CreateClone</c>).
/// Each Ribbon type is handled explicitly, without reflection; check state and text stay in sync both ways.
/// </summary>
internal static class RibbonClone
{
    private static readonly AvaloniaProperty[] s_ribbonProperties =
    [
        RibbonControlService.LabelProperty,
        RibbonControlService.LargeImageSourceProperty,
        RibbonControlService.SmallImageSourceProperty,
        RibbonControlService.ToolTipTitleProperty,
        RibbonControlService.ToolTipDescriptionProperty,
        RibbonControlService.ToolTipImageSourceProperty,
        RibbonControlService.ToolTipFooterTitleProperty,
        RibbonControlService.ToolTipFooterDescriptionProperty,
        RibbonControlService.ToolTipFooterImageSourceProperty,
        RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty,
        ToolTip.TipProperty,
    ];

    /// <summary>Creates the copy, or null when the control's type cannot be copied.</summary>
    public static Control? Create(Control original)
    {
        Control? clone = original switch
        {
            RibbonSplitButton split => CloneMenu(split, new RibbonSplitButton
            {
                Command = split.Command,
                CommandParameter = split.CommandParameter,
                IsCheckable = split.IsCheckable,
            }),
            RibbonComboBox => null,
            InRibbonGallery => null,
            RibbonApplicationMenu => null,
            RibbonFilterMenuButton => null,
            RibbonMenuButton menu => CloneMenu(menu, new RibbonMenuButton()),
            RibbonButton button => new RibbonButton { Command = button.Command, CommandParameter = button.CommandParameter },
            RibbonRadioButton radio => new RibbonRadioButton { Command = radio.Command, CommandParameter = radio.CommandParameter, GroupName = radio.GroupName is null ? null : radio.GroupName + ".QAT" },
            RibbonCheckBox check => new RibbonCheckBox { Command = check.Command, CommandParameter = check.CommandParameter, IsThreeState = check.IsThreeState },
            RibbonToggleButton toggle => new RibbonToggleButton { Command = toggle.Command, CommandParameter = toggle.CommandParameter, IsThreeState = toggle.IsThreeState },
            RibbonTextBox text => new RibbonTextBox { Command = text.Command, CommandParameter = text.CommandParameter, TextBoxWidth = text.TextBoxWidth },
            RibbonMenuItem item => CloneMenuItem(item),
            _ => null,
        };

        if (clone is null)
        {
            return null;
        }

        if (original is not RibbonMenuItem)
        {
            foreach (var property in s_ribbonProperties)
            {
                Transfer(original, clone, property);
            }

            if (clone is ContentControl content && original is ContentControl originalContent && originalContent.Content is string text)
            {
                content.Content = text;
            }
        }

        clone.SetValue(RibbonControlService.QuickAccessToolBarIdProperty, RibbonHelper.QuickAccessToolBarId(original));
        switch (original)
        {
            case ToggleButton toggle when clone is ToggleButton cloneToggle:
                Sync(toggle, cloneToggle, ToggleButton.IsCheckedProperty);
                break;
            case RibbonSplitButton split when clone is RibbonSplitButton cloneSplit:
                Sync(split, cloneSplit, RibbonSplitButton.IsCheckedProperty);
                break;
            case RibbonTextBox text when clone is RibbonTextBox cloneText:
                Sync(text, cloneText, TextBox.TextProperty);
                break;
            case RibbonMenuItem { ToggleType: MenuItemToggleType.CheckBox } item when clone is ToggleButton cloneToggle:
                cloneToggle.IsChecked = item.IsChecked;
                item.PropertyChanged += (_, e) =>
                {
                    if (e.Property == MenuItem.IsCheckedProperty)
                    {
                        cloneToggle.IsChecked = item.IsChecked;
                    }
                };
                cloneToggle.PropertyChanged += (_, e) =>
                {
                    if (e.Property == ToggleButton.IsCheckedProperty && cloneToggle.IsChecked is bool b && b != item.IsChecked)
                    {
                        item.IsChecked = b;
                    }
                };
                break;
        }

        return clone;
    }

    private static Control CloneMenuItem(RibbonMenuItem item)
    {
        if (item.ItemCount > 0)
        {
            return CloneMenu(item, new RibbonMenuButton { Label = item.Header as string, SmallImageSource = item.QuickAccessToolBarImageSource ?? item.ImageSource });
        }

        Button button = item.ToggleType == MenuItemToggleType.CheckBox ? new RibbonToggleButton() : new RibbonButton();
        button.Command = item.Command;
        button.CommandParameter = item.CommandParameter;
        button.SetValue(RibbonControlService.LabelProperty, item.Header as string);
        button.SetValue(RibbonControlService.SmallImageSourceProperty, item.QuickAccessToolBarImageSource ?? item.ImageSource);
        Transfer(item, button, RibbonControlService.ToolTipTitleProperty);
        Transfer(item, button, RibbonControlService.ToolTipDescriptionProperty);
        if (item.Command is null)
        {
            // A leaf item without a command clicks the original.
            button.Click += (_, _) => item.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        }

        return button;
    }

    /// <summary>Copies the items of a menu: the same ItemsSource, or copies of the menu items.</summary>
    private static T CloneMenu<T>(ItemsControl original, T clone)
        where T : ItemsControl
    {
        if (original.ItemsSource is { } source)
        {
            clone.ItemsSource = source;
            clone.ItemTemplate = original.ItemTemplate;
            return clone;
        }

        foreach (var item in original.Items)
        {
            switch (item)
            {
                case RibbonMenuItem menuItem:
                    clone.Items.Add(CloneSubmenuItem(menuItem));
                    break;
                case RibbonSeparator separator:
                    clone.Items.Add(new RibbonSeparator { Label = separator.Label });
                    break;
                case Control:
                    break;
                default:
                    clone.Items.Add(item);
                    break;
            }
        }

        return clone;
    }

    private static RibbonMenuItem CloneSubmenuItem(RibbonMenuItem original)
    {
        var clone = CloneMenu(original, new RibbonMenuItem
        {
            Header = original.Header is string s ? s : original.Header?.ToString(),
            ImageSource = original.ImageSource,
            Command = original.Command,
            CommandParameter = original.CommandParameter,
            ToggleType = original.ToggleType,
            IsChecked = original.IsChecked,
            KeyTip = original.KeyTip,
        });
        if (original.Command is null && original.ItemCount == 0)
        {
            clone.Click += (_, _) => original.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
        }

        return clone;
    }

    private static void Transfer(AvaloniaObject from, AvaloniaObject to, AvaloniaProperty property)
    {
        var value = from.GetValue(property);
        if (value is not null)
        {
            if (property == ToolTip.TipProperty && value is RibbonToolTip { IsGenerated: true })
            {
                return;
            }

            to.SetValue(property, value);
        }
    }

    /// <summary>Keeps a property equal on two objects, both ways.</summary>
    private static void Sync<T>(AvaloniaObject a, AvaloniaObject b, StyledProperty<T> property)
    {
        b.SetValue(property, a.GetValue(property));
        var updating = false;
        void Copy(AvaloniaObject from, AvaloniaObject to, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != property || updating)
            {
                return;
            }

            updating = true;
            try
            {
                to.SetValue(property, from.GetValue(property));
            }
            finally
            {
                updating = false;
            }
        }

        a.PropertyChanged += (_, e) => Copy(a, b, e);
        b.PropertyChanged += (_, e) => Copy(b, a, e);
    }

    /// <summary>Whether <see cref="Create"/> can copy the control.</summary>
    public static bool CanClone(Control control) =>
        control is RibbonButton or RibbonToggleButton or RibbonCheckBox or RibbonRadioButton or RibbonTextBox or RibbonMenuItem ||
        (control is RibbonMenuButton && control is not (RibbonComboBox or InRibbonGallery or RibbonApplicationMenu or RibbonFilterMenuButton));
}
