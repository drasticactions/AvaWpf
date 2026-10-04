using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWpf.WordPad;

/// <summary>
/// WordPad's horizontal ruler: a white band over the page width with tick marks every eighth of an inch and the inch
/// numbers, on the control face color. Drawn with the theme's SystemColors, so it follows every family.
/// </summary>
public class Ruler : Control
{
    private const double Inch = 96;

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var face = this.FindResource("SystemColors.ControlBrush") as IBrush ?? Brushes.LightGray;
        var paper = this.FindResource("SystemColors.WindowBrush") as IBrush ?? Brushes.White;
        var ink = this.FindResource("SystemColors.WindowTextBrush") as IBrush ?? Brushes.Black;
        var dark = this.FindResource("SystemColors.ControlDarkBrush") as IBrush ?? Brushes.Gray;
        context.FillRectangle(face, bounds);

        // The page: from the left margin of the document (12 px workspace padding) to the right edge.
        var band = new Rect(12, 3, System.Math.Max(0, bounds.Width - 24), System.Math.Max(0, bounds.Height - 6));
        context.FillRectangle(paper, band);
        var pen = new Pen(dark, 1);
        context.DrawRectangle(null, pen, band);
        var tick = new Pen(ink, 1);
        var typeface = new Typeface(FontFamily.Default);
        var origin = band.X + 24;
        for (var i = 1; origin + i * Inch / 8 < band.Right; i++)
        {
            var x = System.Math.Round(origin + i * Inch / 8) + 0.5;
            if (i % 8 == 0)
            {
                var text = new FormattedText((i / 8).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, typeface, 9, ink);
                context.DrawText(text, new Point(x - text.Width / 2, band.Y + (band.Height - text.Height) / 2));
            }
            else
            {
                var h = i % 4 == 0 ? 4 : 2;
                var mid = band.Y + band.Height / 2;
                context.DrawLine(tick, new Point(x, mid - h / 2.0), new Point(x, mid + h / 2.0));
            }
        }
    }
}
