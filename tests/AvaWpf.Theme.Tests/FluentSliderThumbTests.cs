using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>The Fluent slider thumb's outer circle overhangs the thumb, so nothing up to the slider clips it.</summary>
public class FluentSliderThumbTests
{
    [AvaloniaTheory]
    [InlineData(Orientation.Horizontal)]
    [InlineData(Orientation.Vertical)]
    public void Thumb_Circle_Is_Not_Clipped(Orientation orientation)
    {
        var slider = new Slider { Orientation = orientation, Width = 200, Height = 200, Maximum = 100, Value = 0 };
        using var scene = ChromeScene.Show(ThemeFamily.Fluent, null, slider, 400, 400);
        var thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
        var circle = thumb.GetVisualChildren().OfType<Border>().Single();
        Assert.True(circle.Bounds.Width > thumb.Bounds.Width);

        var clipping = new Visual[] { thumb }.Concat(thumb.GetVisualAncestors().TakeWhile(v => v != slider)).Append(slider)
            .Where(v => v.ClipToBounds)
            .Select(v => $"{v.GetType().Name} {(v as StyledElement)?.Name}");
        Assert.Empty(clipping);
    }
}
