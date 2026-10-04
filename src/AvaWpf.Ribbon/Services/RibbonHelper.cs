// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonHelper.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaWpf.Ribbon;

/// <summary>Behavior shared by the Ribbon controls: size variants, rich tool tips, Quick Access Toolbar identity.</summary>
internal static class RibbonHelper
{
    /// <summary>The Quick Access Toolbar size of a control that names none: a small image without a label.</summary>
    public static readonly RibbonControlSizeDefinition QuickAccessToolBarDefault = new() { ImageSize = RibbonImageSize.Small, IsLabelVisible = false };

    // WPF's frozen defaults, by image size (Collapsed, Small, Large) and whether there is a label.
    private static readonly RibbonControlSizeDefinition[,] s_defaults =
    {
        { new() { ImageSize = RibbonImageSize.Collapsed, IsLabelVisible = false }, new() { ImageSize = RibbonImageSize.Collapsed, IsLabelVisible = true } },
        { new() { ImageSize = RibbonImageSize.Small, IsLabelVisible = false }, new() { ImageSize = RibbonImageSize.Small, IsLabelVisible = true } },
        { new() { ImageSize = RibbonImageSize.Large, IsLabelVisible = false }, new() { ImageSize = RibbonImageSize.Large, IsLabelVisible = true } },
    };

    /// <summary>The pseudo-classes a size variant sets on a control.</summary>
    public const string Large = ":large";

    /// <summary>A small (16 px) image.</summary>
    public const string Small = ":small";

    /// <summary>No image.</summary>
    public const string NoImage = ":noimage";

    /// <summary>The label is hidden.</summary>
    public const string NoLabel = ":nolabel";

    /// <summary>The control is collapsed by its size definition.</summary>
    public const string Collapsed = ":collapsed";

    /// <summary>The control is in the Quick Access Toolbar.</summary>
    public const string InQat = ":inqat";

    /// <summary>The control is an item of a RibbonControlGroup.</summary>
    public const string InControlGroup = ":incontrolgroup";

    /// <summary>The control has a large image.</summary>
    public const string HasLargeImage = ":haslargeimage";

    /// <summary>
    /// The size variant a control shows when no group assigns one, WPF's <c>DefaultControlSizeDefinition</c>: large
    /// when it has a large image, small when it has only a small image, and no image at all when it has neither; the
    /// label shows when there is one.
    /// </summary>
    public static RibbonControlSizeDefinition DefaultControlSizeDefinition(AvaloniaObject control)
    {
        if (control is RibbonControlGroup group)
        {
            return DefaultControlGroupSizeDefinition(group);
        }

        var image = control.GetValue(RibbonControlService.LargeImageSourceProperty) is not null ? 2
            : control.GetValue(RibbonControlService.SmallImageSourceProperty) is not null ? 1
            : 0;
        var label = !string.IsNullOrEmpty(control.GetValue(RibbonControlService.LabelProperty)) ? 1 : 0;
        return s_defaults[image, label];
    }

    // WPF's RibbonControlGroup default: the largest image and any label among its visible items.
    private static RibbonControlSizeDefinition DefaultControlGroupSizeDefinition(RibbonControlGroup group)
    {
        var image = 0;
        var label = 0;
        foreach (var item in group.Items)
        {
            if (item is Control { IsVisible: true } control)
            {
                var size = DefaultControlSizeDefinition(control);
                image = Math.Max(image, size.ImageSize switch { RibbonImageSize.Large => 2, RibbonImageSize.Small => 1, _ => 0 });
                label |= size.IsLabelVisible ? 1 : 0;
            }
        }

        return s_defaults[image, label];
    }

    /// <summary>The size variant in effect: the Quick Access Toolbar size in the toolbar, else the assigned or default size.</summary>
    public static RibbonControlSizeDefinition EffectiveControlSizeDefinition(AvaloniaObject control)
    {
        if (control.GetValue(RibbonControlService.IsInQuickAccessToolBarProperty))
        {
            return control.GetValue(RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty) ?? QuickAccessToolBarDefault;
        }

        return control.GetValue(RibbonControlService.ControlSizeDefinitionProperty) ?? DefaultControlSizeDefinition(control);
    }

    /// <summary>True when a property change can change the size variant pseudo-classes.</summary>
    public static bool AffectsSize(AvaloniaProperty property) =>
        property == RibbonControlService.ControlSizeDefinitionProperty ||
        property == RibbonControlService.QuickAccessToolBarControlSizeDefinitionProperty ||
        property == RibbonControlService.IsInQuickAccessToolBarProperty ||
        property == RibbonControlService.IsInControlGroupProperty ||
        property == RibbonControlService.LargeImageSourceProperty ||
        property == RibbonControlService.SmallImageSourceProperty ||
        property == RibbonControlService.LabelProperty;

    /// <summary>True when a property change changes the rich tool tip.</summary>
    public static bool AffectsToolTip(AvaloniaProperty property) =>
        property == RibbonControlService.ToolTipTitleProperty ||
        property == RibbonControlService.ToolTipDescriptionProperty ||
        property == RibbonControlService.ToolTipImageSourceProperty ||
        property == RibbonControlService.ToolTipFooterTitleProperty ||
        property == RibbonControlService.ToolTipFooterDescriptionProperty ||
        property == RibbonControlService.ToolTipFooterImageSourceProperty;

    /// <summary>Sets the size variant pseudo-classes (<c>:large</c>, <c>:small</c>, <c>:noimage</c>, <c>:nolabel</c>, …).</summary>
    public static void UpdateSizePseudoClasses(Control control, IPseudoClasses classes)
    {
        var definition = EffectiveControlSizeDefinition(control);
        classes.Set(Large, definition.ImageSize == RibbonImageSize.Large);
        classes.Set(Small, definition.ImageSize == RibbonImageSize.Small);
        classes.Set(NoImage, definition.ImageSize == RibbonImageSize.Collapsed);
        // WPF collapses the label when the size definition hides it, and its templates also when there is no Label:
        // an empty label would still take its margins beside the image.
        classes.Set(NoLabel, !definition.IsLabelVisible || string.IsNullOrEmpty(control.GetValue(RibbonControlService.LabelProperty)));
        classes.Set(Collapsed, definition.IsCollapsed);
        classes.Set(InQat, control.GetValue(RibbonControlService.IsInQuickAccessToolBarProperty));
        classes.Set(InControlGroup, control.GetValue(RibbonControlService.IsInControlGroupProperty));
        classes.Set(HasLargeImage, control.GetValue(RibbonControlService.LargeImageSourceProperty) is not null);
    }

    /// <summary>
    /// Shows the rich <see cref="RibbonToolTip"/> built from the ToolTip* properties as the control's tool tip, unless
    /// the application set its own <see cref="ToolTip.TipProperty"/>.
    /// </summary>
    public static void UpdateToolTip(Control control)
    {
        var current = ToolTip.GetTip(control);
        if (current is not null && current is not RibbonToolTip { IsGenerated: true })
        {
            return;
        }

        var title = control.GetValue(RibbonControlService.ToolTipTitleProperty);
        var description = control.GetValue(RibbonControlService.ToolTipDescriptionProperty);
        var image = control.GetValue(RibbonControlService.ToolTipImageSourceProperty);
        var footerTitle = control.GetValue(RibbonControlService.ToolTipFooterTitleProperty);
        var footerDescription = control.GetValue(RibbonControlService.ToolTipFooterDescriptionProperty);
        var footerImage = control.GetValue(RibbonControlService.ToolTipFooterImageSourceProperty);
        if (title is null && description is null && image is null && footerTitle is null && footerDescription is null && footerImage is null)
        {
            if (current is not null)
            {
                control.ClearValue(ToolTip.TipProperty);
            }

            return;
        }

        var tip = current as RibbonToolTip ?? new RibbonToolTip { IsGenerated = true };
        tip.Title = title;
        tip.Description = description;
        tip.ImageSource = image;
        tip.FooterTitle = footerTitle;
        tip.FooterDescription = footerDescription;
        tip.FooterImageSource = footerImage;
        if (current is null)
        {
            ToolTip.SetTip(control, tip);
        }
    }

    /// <summary>The Quick Access Toolbar identity of a control: its QuickAccessToolBarId, or its command.</summary>
    public static object? QuickAccessToolBarId(AvaloniaObject control) =>
        control.GetValue(RibbonControlService.QuickAccessToolBarIdProperty) ?? (control as ICommandSource)?.Command;

    /// <summary>
    /// Invalidates the measure of every element from <paramref name="element"/> up to (and including) the nearest
    /// ancestor of type <typeparamref name="T"/>, as WPF's <c>TreeHelper.InvalidateMeasureForVisualAncestorPath</c>:
    /// a size change of a control deep inside a group must reach the group, whose measure is otherwise cached.
    /// </summary>
    public static void InvalidateMeasureToAncestor<T>(Visual element)
        where T : Layoutable
    {
        for (var v = element; v is not null; v = v.GetVisualParent())
        {
            if (v is Layoutable layoutable)
            {
                layoutable.InvalidateMeasure();
            }

            if (v is T)
            {
                return;
            }
        }
    }

    /// <summary>Invalidates the measure of an element and all its visual descendants.</summary>
    public static void InvalidateMeasureDeep(Visual element)
    {
        if (element is Layoutable layoutable)
        {
            layoutable.InvalidateMeasure();
        }

        // Indexed: a foreach over GetVisualChildren() boxes an enumerator per element, every resize step.
        if (element.GetVisualChildren() is IReadOnlyList<Visual> children)
        {
            for (var i = 0; i < children.Count; i++)
            {
                InvalidateMeasureDeep(children[i]);
            }
        }
        else
        {
            foreach (var child in element.GetVisualChildren())
            {
                InvalidateMeasureDeep(child);
            }
        }
    }
}
