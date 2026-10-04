// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonControlGroup.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.Ribbon;

/// <summary>
/// A row of small Ribbon controls drawn as one bordered strip, such as Bold/Italic/Underline.
/// </summary>
public class RibbonControlGroup : ItemsControl
{
    /// <summary>Defines the <see cref="ControlSizeDefinition"/> property.</summary>
    public static readonly AttachedProperty<RibbonControlSizeDefinition?> ControlSizeDefinitionProperty = RibbonControlService.ControlSizeDefinitionProperty.AddOwner<RibbonControlGroup>();

    /// <summary>Defines the <see cref="Ribbon"/> property.</summary>
    public static readonly AttachedProperty<Ribbon?> RibbonProperty = RibbonControlService.RibbonProperty.AddOwner<RibbonControlGroup>();

    /// <summary>The size variant set by the owning group; the group applies it to its items.</summary>
    public RibbonControlSizeDefinition? ControlSizeDefinition
    {
        get => GetValue(ControlSizeDefinitionProperty);
        set => SetValue(ControlSizeDefinitionProperty, value);
    }

    /// <summary>The Ribbon the group is in.</summary>
    public Ribbon? Ribbon => GetValue(RibbonProperty);

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is Control)
        {
            recycleKey = null;
            return false;
        }

        return base.NeedsContainerOverride(item, index, out recycleKey);
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        container.SetValue(RibbonControlService.IsInControlGroupProperty, true);
        ApplySize(container);
    }

    /// <inheritdoc/>
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        container.ClearValue(RibbonControlService.IsInControlGroupProperty);
        container.ClearValue(RibbonControlService.ControlSizeDefinitionProperty);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ControlSizeDefinitionProperty)
        {
            foreach (var container in GetRealizedContainers())
            {
                ApplySize(container);
            }
        }
    }

    private void ApplySize(Control container)
    {
        // Items keep their images; only the label follows the group's size, so a collapsed group hides labels.
        var label = ControlSizeDefinition?.IsLabelVisible ?? false;
        container.SetValue(RibbonControlService.ControlSizeDefinitionProperty,
            new RibbonControlSizeDefinition { ImageSize = RibbonImageSize.Small, IsLabelVisible = label });
    }
}
