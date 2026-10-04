// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonContextualTabGroup.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Metadata;

namespace AvaWpf.Ribbon;

/// <summary>
/// A contextual tab group: a colored title over the tabs whose <see cref="RibbonTab.ContextualTabGroupHeader"/>
/// equals its <see cref="Header"/>. Hiding the group hides its tabs; its <c>Background</c> tints its title and tab headers.
/// </summary>
[PseudoClasses(":incaption")]
public class RibbonContextualTabGroup : TemplatedControl
{
    /// <summary>Defines the <see cref="Header"/> property.</summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<RibbonContextualTabGroup, object?>(nameof(Header));

    /// <summary>Defines the <see cref="HeaderTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<RibbonContextualTabGroup, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonContextualTabGroup>();

    /// <summary>The title, matched against the tabs' <see cref="RibbonTab.ContextualTabGroupHeader"/>.</summary>
    [Content]
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>The template of <see cref="Header"/>.</summary>
    public IDataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>The Ribbon the group belongs to.</summary>
    public Ribbon? Ribbon => GetValue(RibbonProperty);

    /// <summary>The left edge of the group's tab headers, relative to the tab row, from the last layout pass.</summary>
    internal double TabsLeft { get; set; }

    /// <summary>The width the title needs untrimmed, from the last measure pass.</summary>
    internal double IdealWidth { get; set; }

    /// <summary>The width of the group's tab headers, from the last layout pass (0 when no tab shows).</summary>
    internal double TabsWidth { get; set; }

    /// <summary>Sets the <c>:incaption</c> pseudo-class.</summary>
    internal void SetIsInCaption(bool value) => PseudoClasses.Set(":incaption", value);

    /// <summary>Selects the group's first tab when the title is clicked.</summary>
    /// <param name="e">The event data.</param>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && Ribbon is { } ribbon)
        {
            ribbon.SelectFirstTabOf(this);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == HeaderProperty || change.Property == IsVisibleProperty || change.Property == BackgroundProperty)
        {
            Ribbon?.UpdateContextualTabGroups();
        }
    }
}
