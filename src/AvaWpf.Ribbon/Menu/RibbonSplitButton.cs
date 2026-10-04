// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonSplitButton.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Automation.Peers;
using AvaWpf.Ribbon.Automation.Peers;

namespace AvaWpf.Ribbon;

/// <summary>
/// A Ribbon split button: the header half runs <see cref="Command"/> (or toggles, when <see cref="IsCheckable"/>), the
/// arrow half opens the menu.
/// </summary>
[TemplatePart("PART_HeaderButton", typeof(Button))]
[TemplatePart("PART_ToggleButton", typeof(ToggleButton))]
[PseudoClasses(":checked", ":headerpressed")]
public class RibbonSplitButton : RibbonMenuButton, IKeyTipSiblingProvider
{
    /// <summary>Defines the <see cref="Command"/> property.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<RibbonSplitButton, ICommand?>(nameof(Command));

    /// <summary>Defines the <see cref="CommandParameter"/> property.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<RibbonSplitButton, object?>(nameof(CommandParameter));

    /// <summary>Defines the <see cref="IsCheckable"/> property.</summary>
    public static readonly StyledProperty<bool> IsCheckableProperty =
        AvaloniaProperty.Register<RibbonSplitButton, bool>(nameof(IsCheckable));

    /// <summary>Defines the <see cref="IsChecked"/> property.</summary>
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<RibbonSplitButton, bool>(nameof(IsChecked), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines the <see cref="LabelPosition"/> property.</summary>
    public static readonly StyledProperty<RibbonSplitButtonLabelPosition> LabelPositionProperty =
        AvaloniaProperty.Register<RibbonSplitButton, RibbonSplitButtonLabelPosition>(nameof(LabelPosition));

    /// <summary>Defines the <see cref="HeaderKeyTip"/> property.</summary>
    public static readonly StyledProperty<string?> HeaderKeyTipProperty =
        AvaloniaProperty.Register<RibbonSplitButton, string?>(nameof(HeaderKeyTip));

    /// <summary>Defines the <see cref="HeaderQuickAccessToolBarId"/> property.</summary>
    public static readonly StyledProperty<object?> HeaderQuickAccessToolBarIdProperty =
        AvaloniaProperty.Register<RibbonSplitButton, object?>(nameof(HeaderQuickAccessToolBarId));

    /// <summary>Defines the <see cref="DropDownToolTipTitle"/> property.</summary>
    public static readonly StyledProperty<string?> DropDownToolTipTitleProperty =
        AvaloniaProperty.Register<RibbonSplitButton, string?>(nameof(DropDownToolTipTitle));

    /// <summary>Defines the <see cref="DropDownToolTipDescription"/> property.</summary>
    public static readonly StyledProperty<string?> DropDownToolTipDescriptionProperty =
        AvaloniaProperty.Register<RibbonSplitButton, string?>(nameof(DropDownToolTipDescription));

    /// <summary>Defines the <see cref="Click"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<RibbonSplitButton, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private Button? _headerButton;

    /// <summary>Raised when the header half is clicked.</summary>
    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>The command the header half runs.</summary>
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

    /// <summary>Whether the header half toggles <see cref="IsChecked"/>.</summary>
    public bool IsCheckable
    {
        get => GetValue(IsCheckableProperty);
        set => SetValue(IsCheckableProperty, value);
    }

    /// <summary>The check state of a checkable split button.</summary>
    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Which half of the large variant shows the label. Default <see cref="RibbonSplitButtonLabelPosition.Header"/>.</summary>
    public RibbonSplitButtonLabelPosition LabelPosition
    {
        get => GetValue(LabelPositionProperty);
        set => SetValue(LabelPositionProperty, value);
    }

    /// <summary>The KeyTip of the header half; the button's own KeyTip opens the menu.</summary>
    public string? HeaderKeyTip
    {
        get => GetValue(HeaderKeyTipProperty);
        set => SetValue(HeaderKeyTipProperty, value);
    }

    /// <summary>The Quick Access Toolbar identity of the header half (added as a plain button).</summary>
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

    /// <summary>Runs the header action as a click on the header would (UI Automation's invoke and toggle).</summary>
    internal void PerformClick() => OnClick();

    /// <summary>Runs the header action: toggles a checkable button, raises <see cref="Click"/> and runs the command.</summary>
    protected virtual void OnClick()
    {
        if (IsCheckable)
        {
            SetCurrentValue(IsCheckedProperty, !IsChecked);
        }

        RaiseEvent(new RoutedEventArgs(ClickEvent));
        var parameter = CommandParameter;
        if (Command is { } command && command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }

        RaiseEvent(new RibbonDismissPopupEventArgs());
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

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty)
        {
            PseudoClasses.Set(":checked", change.GetNewValue<bool>());
        }
    }

    private void OnHeaderClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        OnClick();
    }

    private void OnHeaderKeyTipAccessed(object? sender, KeyTipAccessedEventArgs e)
    {
        if (e.Source == _headerButton)
        {
            OnClick();
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonSplitButtonAutomationPeer(this);
}
