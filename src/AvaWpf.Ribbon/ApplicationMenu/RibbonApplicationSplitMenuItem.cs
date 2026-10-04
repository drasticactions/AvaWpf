// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonApplicationSplitMenuItem.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace AvaWpf.Ribbon;

/// <summary>A split item of a <see cref="RibbonApplicationMenu"/>; its <see cref="Level"/> picks its look.</summary>
[PseudoClasses(":top", ":middle", ":sub")]
public class RibbonApplicationSplitMenuItem : RibbonSplitMenuItem
{
    /// <summary>Defines the <see cref="Level"/> property.</summary>
    public static readonly DirectProperty<RibbonApplicationSplitMenuItem, RibbonApplicationMenuItemLevel> LevelProperty =
        AvaloniaProperty.RegisterDirect<RibbonApplicationSplitMenuItem, RibbonApplicationMenuItemLevel>(nameof(Level), o => o.Level);

    private RibbonApplicationMenuItemLevel _level;

    /// <summary>Initializes a new instance of the <see cref="RibbonApplicationSplitMenuItem"/> class.</summary>
    public RibbonApplicationSplitMenuItem()
    {
        PseudoClasses.Set(":top", true);
    }

    /// <summary>The nesting level, set from the parent.</summary>
    public RibbonApplicationMenuItemLevel Level
    {
        get => _level;
        internal set
        {
            SetAndRaise(LevelProperty, ref _level, value);
            PseudoClasses.Set(":top", value == RibbonApplicationMenuItemLevel.Top);
            PseudoClasses.Set(":middle", value == RibbonApplicationMenuItemLevel.Middle);
            PseudoClasses.Set(":sub", value == RibbonApplicationMenuItemLevel.Sub);
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
        RibbonApplicationMenu.SetLevel(container, RibbonApplicationMenuItem.ChildLevel(Level));
    }
}
