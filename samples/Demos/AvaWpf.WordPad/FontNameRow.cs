using Avalonia;
using Avalonia.Controls;

namespace AvaWpf.WordPad;

/// <summary>
/// A row of the font list. It asks for no width, so the list stays as wide as the font box, and it clips a name
/// longer than the row.
/// </summary>
internal sealed class FontNameRow : Decorator
{
    public FontNameRow()
    {
        ClipToBounds = true;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize.WithWidth(double.PositiveInfinity));
        return new Size(0, Child?.DesiredSize.Height ?? 0);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // The name keeps its own width and is cut at the row's edge.
        if (Child is { } child)
        {
            var width = child.DesiredSize.Width;
            var top = (finalSize.Height - child.DesiredSize.Height) / 2;
            child.Arrange(new Rect(0, top, width, child.DesiredSize.Height));
        }

        return finalSize;
    }
}
