using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AvaWpf.WordPad;

/// <summary>The sample picture the <c>--picture</c> launch option inserts into the document.</summary>
internal static class Pictures
{
    /// <summary>A small landscape, as PNG bytes.</summary>
    public static byte[] Sample()
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(240, 150));
        using (var context = bitmap.CreateDrawingContext())
        {
            var sky = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse("#3E8EDE"), 0), new GradientStop(Color.Parse("#BFE3FF"), 1) },
            };
            context.FillRectangle(sky, new Rect(0, 0, 240, 150));
            context.DrawEllipse(new SolidColorBrush(Color.Parse("#FFD54A")), null, new Point(186, 40), 18, 18);
            context.DrawGeometry(new SolidColorBrush(Color.Parse("#6DAA45")), null,
                Geometry.Parse("M0,110 C50,80 90,85 130,105 C170,125 205,90 240,95 L240,150 L0,150 Z"));
            context.DrawGeometry(new SolidColorBrush(Color.Parse("#4E8A34")), null,
                Geometry.Parse("M0,130 C60,115 120,125 170,135 C200,141 225,132 240,128 L240,150 L0,150 Z"));
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }
}
