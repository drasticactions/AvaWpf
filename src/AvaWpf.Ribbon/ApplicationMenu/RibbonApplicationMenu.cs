// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonApplicationMenu.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace AvaWpf.Ribbon;

/// <summary>
/// The application menu: the button at the left of the tab row. Its drop-down lists
/// <see cref="RibbonApplicationMenuItem"/>s on the left, the <see cref="AuxiliaryPaneContent"/> (recent files, say) on
/// the right, and the <see cref="FooterPaneContent"/> (Options, Exit) along the bottom.
/// </summary>
public class RibbonApplicationMenu : RibbonMenuButton
{
    /// <summary>Defines the <see cref="AuxiliaryPaneContent"/> property.</summary>
    public static readonly StyledProperty<object?> AuxiliaryPaneContentProperty =
        AvaloniaProperty.Register<RibbonApplicationMenu, object?>(nameof(AuxiliaryPaneContent));

    /// <summary>Defines the <see cref="AuxiliaryPaneContentTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> AuxiliaryPaneContentTemplateProperty =
        AvaloniaProperty.Register<RibbonApplicationMenu, IDataTemplate?>(nameof(AuxiliaryPaneContentTemplate));

    /// <summary>Defines the <see cref="FooterPaneContent"/> property.</summary>
    public static readonly StyledProperty<object?> FooterPaneContentProperty =
        AvaloniaProperty.Register<RibbonApplicationMenu, object?>(nameof(FooterPaneContent));

    /// <summary>Defines the <see cref="FooterPaneContentTemplate"/> property.</summary>
    public static readonly StyledProperty<IDataTemplate?> FooterPaneContentTemplateProperty =
        AvaloniaProperty.Register<RibbonApplicationMenu, IDataTemplate?>(nameof(FooterPaneContentTemplate));

    /// <summary>The content of the right-hand pane.</summary>
    public object? AuxiliaryPaneContent
    {
        get => GetValue(AuxiliaryPaneContentProperty);
        set => SetValue(AuxiliaryPaneContentProperty, value);
    }

    /// <summary>The template of <see cref="AuxiliaryPaneContent"/>.</summary>
    public IDataTemplate? AuxiliaryPaneContentTemplate
    {
        get => GetValue(AuxiliaryPaneContentTemplateProperty);
        set => SetValue(AuxiliaryPaneContentTemplateProperty, value);
    }

    /// <summary>The content of the bottom pane.</summary>
    public object? FooterPaneContent
    {
        get => GetValue(FooterPaneContentProperty);
        set => SetValue(FooterPaneContentProperty, value);
    }

    /// <summary>The template of <see cref="FooterPaneContent"/>.</summary>
    public IDataTemplate? FooterPaneContentTemplate
    {
        get => GetValue(FooterPaneContentTemplateProperty);
        set => SetValue(FooterPaneContentTemplateProperty, value);
    }

    /// <summary>Sets the level of an application menu item container.</summary>
    internal static void SetLevel(Control container, RibbonApplicationMenuItemLevel level)
    {
        switch (container)
        {
            case RibbonApplicationMenuItem item:
                item.Level = level;
                break;
            case RibbonApplicationSplitMenuItem split:
                split.Level = level;
                break;
        }
    }

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new RibbonApplicationMenuItem();

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is Control)
        {
            recycleKey = null;
            return false;
        }

        return NeedsContainer<RibbonApplicationMenuItem>(item, out recycleKey);
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        SetLevel(container, RibbonApplicationMenuItemLevel.Top);
    }
}
