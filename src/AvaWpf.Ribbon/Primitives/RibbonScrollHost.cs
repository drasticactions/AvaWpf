using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.Ribbon.Primitives;

/// <summary>Shows one row of its child at a time and scrolls it, for the in-ribbon part of an <see cref="InRibbonGallery"/>.</summary>
public class RibbonScrollHost : Decorator
{
    /// <summary>Defines the <see cref="VerticalOffset"/> property.</summary>
    public static readonly StyledProperty<double> VerticalOffsetProperty =
        AvaloniaProperty.Register<RibbonScrollHost, double>(nameof(VerticalOffset));

    static RibbonScrollHost()
    {
        ClipToBoundsProperty.OverrideDefaultValue<RibbonScrollHost>(true);
        AffectsArrange<RibbonScrollHost>(VerticalOffsetProperty);
    }

    /// <summary>How far the child is scrolled up, in pixels.</summary>
    public double VerticalOffset
    {
        get => GetValue(VerticalOffsetProperty);
        set => SetValue(VerticalOffsetProperty, value);
    }

    /// <summary>The height of the child.</summary>
    public double ExtentHeight => Child?.DesiredSize.Height ?? 0;

    /// <summary>Scrolls by <paramref name="delta"/> pixels, kept within the child.</summary>
    /// <param name="delta">The distance; positive scrolls down.</param>
    public void ScrollBy(double delta)
    {
        var max = Math.Max(0, ExtentHeight - Bounds.Height);
        VerticalOffset = Math.Clamp(VerticalOffset + delta, 0, max);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
        {
            return default;
        }

        child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        var height = double.IsInfinity(availableSize.Height) ? child.DesiredSize.Height : Math.Min(availableSize.Height, child.DesiredSize.Height);
        return new Size(child.DesiredSize.Width, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(0, -VerticalOffset, finalSize.Width, Math.Max(finalSize.Height, Child.DesiredSize.Height)));
        return finalSize;
    }
}
