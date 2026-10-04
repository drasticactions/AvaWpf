using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace AvaWpf.Samples;

/// <summary>
/// The sample icons (Explorer file-system icons, ribbon and toolbar command images), as vector drawings on a 16-unit
/// grid, so they stay sharp at any size and scaling. No Windows image is copied.
/// </summary>
public static class SampleImages
{
    private static readonly Dictionary<string, IImage> s_cache = new();

    /// <summary>The icon names <see cref="Load"/> knows.</summary>
    public static IReadOnlyCollection<string> Names => s_draw.Keys;

    /// <summary>
    /// The icon <paramref name="name"/>. <paramref name="size"/> is the size it is shown at (16 or 32); the drawing
    /// scales to whatever size the image is given.
    /// </summary>
    public static IImage Load(string name, int size = 16)
    {
        _ = size;
        if (!s_cache.TryGetValue(name, out var image))
        {
            if (!s_draw.TryGetValue(name, out var draw))
            {
                throw new ArgumentException($"No sample icon named '{name}'.", nameof(name));
            }

            var group = new DrawingGroup { ClipGeometry = new RectangleGeometry(Grid) };
            using (var dc = group.Open())
            {
                // A transparent frame: a DrawingImage takes its size from its drawing's bounds.
                dc.DrawRectangle(Brushes.Transparent, null, Grid);
                draw(dc);
            }

            image = new DrawingImage(group);
            s_cache[name] = image;
        }

        return image;
    }

    private static readonly Rect Grid = new(0, 0, 16, 16);

    private static readonly Dictionary<string, Action<DrawingContext>> s_draw = new()
    {
        ["folder"] = dc => Folder(dc, open: false),
        ["folder-open"] = dc => Folder(dc, open: true),
        ["file"] = dc => Page(dc, null),
        ["text"] = dc => Page(dc, Lines),
        ["image"] = dc => Page(dc, Picture),
        ["picture"] = dc => Page(dc, Picture),
        ["drive"] = Drive,
        ["computer"] = Computer,
        ["network"] = Network,
        ["recycle"] = Recycle,
        ["cut"] = Cut,
        ["copy"] = Copy,
        ["paste"] = Paste,
        ["bold"] = dc => Letter(dc, "B", FontStyle.Normal, FontWeight.Bold),
        ["italic"] = dc => Letter(dc, "I", FontStyle.Italic, FontWeight.Normal),
        ["underline"] = dc => Letter(dc, "U", FontStyle.Normal, FontWeight.Normal, underline: true),
        ["align-left"] = dc => Align(dc, 0),
        ["align-center"] = dc => Align(dc, 1),
        ["align-right"] = dc => Align(dc, 2),
        ["undo"] = dc => Arrow(dc, flip: false),
        ["redo"] = dc => Arrow(dc, flip: true),
        ["find"] = Find,
        ["new"] = dc => { Page(dc, null); Spark(dc); },
        ["save"] = dc => Disk(dc, pencil: false),
        ["save-as"] = dc => Disk(dc, pencil: true),
        ["print"] = Printer,
        ["about"] = About,
        ["exit"] = Exit,
        ["replace"] = Replace,
        ["select-all"] = SelectAll,
        ["zoom-in"] = dc => Zoom(dc, plus: true),
        ["zoom-out"] = dc => Zoom(dc, plus: false),
        ["bullets"] = Bullets,
        ["wordpad"] = WordPad,
        ["theme"] = Palette,
        ["rotate"] = Rotate,
        ["crop"] = Crop,
    };

    private static SolidColorBrush Rgb(uint rgb) => new(Color.FromUInt32(0xFF000000 | rgb));

    private static Pen Stroke(uint rgb, double width, PenLineCap cap = PenLineCap.Flat, PenLineJoin join = PenLineJoin.Miter) =>
        new(Rgb(rgb), width, lineCap: cap, lineJoin: join);

    private static Geometry Poly(bool closed, params Point[] points)
    {
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            c.BeginFigure(points[0], closed);
            for (var i = 1; i < points.Length; i++)
            {
                c.LineTo(points[i]);
            }

            c.EndFigure(closed);
        }

        return g;
    }

    private static void Folder(DrawingContext dc, bool open)
    {
        var edge = Stroke(0xB08220, 0.8);
        dc.DrawGeometry(Rgb(0xE8B848), edge, Poly(true, new(1, 3), new(6, 3), new(7.5, 4.5), new(15, 4.5), new(15, 13.5), new(1, 13.5)));
        var body = open
            ? Poly(true, new(3, 7), new(15.5, 7), new(13.5, 13.5), new(1, 13.5))
            : new RectangleGeometry(new Rect(1, 6, 14, 7.5));
        dc.DrawGeometry(Rgb(0xF8D775), edge, body);
    }

    private static void Page(DrawingContext dc, Action<DrawingContext>? content)
    {
        var edge = Stroke(0x808080, 0.8);
        dc.DrawGeometry(Brushes.White, edge, Poly(true, new(3, 1), new(10, 1), new(13, 4), new(13, 15), new(3, 15)));
        dc.DrawGeometry(null, edge, Poly(false, new(10, 1), new(10, 4), new(13, 4)));
        content?.Invoke(dc);
    }

    private static void Lines(DrawingContext dc)
    {
        var p = Stroke(0x4060A0, 0.8);
        for (var y = 6; y <= 12; y += 2)
        {
            dc.DrawLine(p, new(5, y), new(11, y));
        }
    }

    private static void Picture(DrawingContext dc)
    {
        dc.DrawRectangle(Rgb(0x8CC8F0), null, new Rect(5, 6, 6, 6));
        dc.DrawGeometry(Rgb(0x4CA040), null, Poly(true, new(5, 12), new(8, 8.5), new(11, 12)));
    }

    private static void Drive(DrawingContext dc)
    {
        dc.DrawRectangle(Rgb(0xC8C8C8), Stroke(0x707070, 0.8), new Rect(1.5, 5, 13, 6), 1, 1);
        dc.DrawEllipse(Rgb(0x30C030), null, new Point(12, 8), 1, 1);
    }

    private static void Computer(DrawingContext dc)
    {
        var edge = Stroke(0x707070, 0.8);
        dc.DrawRectangle(Rgb(0xD8D8D0), edge, new Rect(2, 2, 12, 9));
        dc.DrawRectangle(Rgb(0x3070C0), null, new Rect(3.5, 3.5, 9, 6));
        dc.DrawRectangle(Rgb(0xD8D8D0), edge, new Rect(5, 12, 6, 2));
    }

    private static void Network(DrawingContext dc)
    {
        var line = Stroke(0xE0F0FF, 0.8);
        dc.DrawEllipse(Rgb(0x4090D0), null, new Point(8, 8), 6.5, 6.5);
        dc.DrawLine(line, new(1.5, 8), new(14.5, 8));
        dc.DrawEllipse(null, line, new Point(8, 8), 3, 6.5);
    }

    private static void Recycle(DrawingContext dc)
    {
        var edge = Stroke(0x507090, 0.8);
        dc.DrawGeometry(Rgb(0xB8D0E8), edge, Poly(true, new(3, 4), new(13, 4), new(12, 15), new(4, 15)));
        dc.DrawLine(edge, new(2, 3), new(14, 3));
    }

    private static void Cut(DrawingContext dc)
    {
        var e = Stroke(0x303030, 1);
        dc.DrawLine(e, new(5, 2), new(10, 11));
        dc.DrawLine(e, new(11, 2), new(6, 11));
        dc.DrawEllipse(null, e, new Point(5, 12.5), 2, 2);
        dc.DrawEllipse(null, e, new Point(11, 12.5), 2, 2);
    }

    private static void Copy(DrawingContext dc)
    {
        var e = Stroke(0x505050, 0.8);
        dc.DrawRectangle(Brushes.White, e, new Rect(2, 2, 8, 9));
        dc.DrawRectangle(Brushes.White, e, new Rect(6, 5, 8, 9));
    }

    private static void Paste(DrawingContext dc)
    {
        var e = Stroke(0x604020, 0.8);
        dc.DrawRectangle(Rgb(0xC89A58), e, new Rect(3, 3, 10, 12));
        dc.DrawRectangle(Brushes.White, null, new Rect(5, 5, 6, 8));
        dc.DrawRectangle(null, e, new Rect(6, 1.5, 4, 2.5));
    }

    private static readonly FontFamily s_letterFont = new("DejaVu Sans, Tahoma, Segoe UI");

    private static Geometry? Text(string text, double emSize, FontStyle style, FontWeight weight, Point baselineOrigin, out double width)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(s_letterFont, style, weight), emSize, Brushes.Black);
        width = ft.Width;
        return ft.BuildGeometry(new Point(baselineOrigin.X, baselineOrigin.Y - ft.Baseline));
    }

    private static void Letter(DrawingContext dc, string letter, FontStyle style, FontWeight weight, bool underline = false)
    {
        var ink = Rgb(0x202020);

        // Centred on the grid by its ink, baseline at 12.5.
        if (Text(letter, 12.8, style, weight, new Point(0, 12.5), out _) is { } g)
        {
            var bounds = g.Bounds;
            using (dc.PushTransform(Matrix.CreateTranslation(8 - bounds.Center.X, 0)))
            {
                dc.DrawGeometry(ink, null, g);
            }
        }

        if (underline)
        {
            dc.DrawLine(new Pen(ink, 1), new(4, 14.5), new(12, 14.5));
        }
    }

    private static void Align(DrawingContext dc, int mode)
    {
        var e = Stroke(0x303030, 1);
        for (var i = 0; i < 4; i++)
        {
            var w = i % 2 == 0 ? 12.0 : 8.0;
            var x = mode switch { 0 => 2.0, 1 => (16 - w) / 2, _ => 14 - w };
            var y = 3.5 + (i * 3);
            dc.DrawLine(e, new(x, y), new(x + w, y));
        }
    }

    private static void Arrow(DrawingContext dc, bool flip)
    {
        using var _ = dc.PushTransform(flip ? new Matrix(-1, 0, 0, 1, 16, 0) : Matrix.Identity);
        var e = Stroke(0x2060B0, 1.6, PenLineCap.Round, PenLineJoin.Round);
        var curve = new StreamGeometry();
        using (var c = curve.Open())
        {
            // Leaves the head's base horizontally, so the head points along the shaft.
            c.BeginFigure(new Point(5.5, 6), false);
            c.CubicBezierTo(new Point(10.5, 6), new Point(13, 8), new Point(12.5, 12.5));
            c.EndFigure(false);
        }

        dc.DrawGeometry(null, e, curve);
        dc.DrawGeometry(Rgb(0x2060B0), null, Poly(true, new(1.5, 6), new(6.5, 2), new(6.5, 10)));
    }

    private static void Find(DrawingContext dc)
    {
        var e = Stroke(0x303030, 1.2);
        dc.DrawEllipse(Rgb(0xC8E4F8), e, new Point(6.5, 6.5), 4, 4);
        dc.DrawLine(new Pen(Rgb(0x303030), 1.8, lineCap: PenLineCap.Round), new(9.5, 9.5), new(14, 14));
    }

    private static void Spark(DrawingContext dc)
    {
        var points = new Point[8];
        for (var i = 0; i < 8; i++)
        {
            var r = i % 2 == 0 ? 3.5 : 1.3;
            var a = (i * Math.PI / 4) - (Math.PI / 2);
            points[i] = new Point(12 + (Math.Cos(a) * r), 4 + (Math.Sin(a) * r));
        }

        dc.DrawGeometry(Rgb(0xF8C820), Stroke(0xB08010, 0.5), Poly(true, points));
    }

    private static void Disk(DrawingContext dc, bool pencil)
    {
        dc.DrawRectangle(Rgb(0x385CA8), Stroke(0x183068, 0.8), new Rect(1.5, 1.5, 13, 13));
        dc.DrawRectangle(Brushes.White, null, new Rect(4, 2, 8, 5));
        dc.DrawRectangle(Rgb(0xC0C8D0), null, new Rect(5, 10, 6, 4.5));
        if (pencil)
        {
            dc.DrawLine(Stroke(0xF0A030, 2.2), new(9, 15), new(15, 9));
            dc.DrawLine(Stroke(0x303030, 2.2), new(8, 16), new(9, 15));
        }
    }

    private static void Printer(DrawingContext dc)
    {
        var edge = Stroke(0x585858, 0.8);
        dc.DrawRectangle(Brushes.White, edge, new Rect(4, 1.5, 8, 5.5));
        dc.DrawRectangle(Rgb(0xB8B8B0), edge, new Rect(1.5, 6, 13, 6));
        dc.DrawRectangle(Brushes.White, edge, new Rect(4, 10, 8, 4.5));
    }

    private static void About(DrawingContext dc)
    {
        dc.DrawEllipse(Rgb(0x3070D0), null, new Point(8, 8), 7, 7);
        dc.DrawEllipse(Brushes.White, null, new Point(8, 4.5), 1.1, 1.1);
        dc.DrawRectangle(Brushes.White, null, new Rect(7, 6.5, 2, 6));
    }

    private static void Exit(DrawingContext dc)
    {
        dc.DrawRectangle(Rgb(0xC08040), Stroke(0x604020, 0.8), new Rect(2, 1.5, 7, 13));
        var arrow = Stroke(0xC02020, 1.4, PenLineCap.Round, PenLineJoin.Round);
        dc.DrawLine(arrow, new(7, 8), new(14, 8));
        dc.DrawGeometry(null, arrow, Poly(false, new(11.5, 5), new(14.5, 8), new(11.5, 11)));
    }

    private static void Replace(DrawingContext dc)
    {
        if (Text("a", 8, FontStyle.Normal, FontWeight.Bold, new Point(1, 7), out _) is { } a)
        {
            dc.DrawGeometry(Rgb(0x202020), null, a);
        }

        if (Text("b", 8, FontStyle.Normal, FontWeight.Bold, new Point(9, 15), out _) is { } b)
        {
            dc.DrawGeometry(Rgb(0x2060B0), null, b);
        }

        var arrow = Stroke(0xC04020, 1, PenLineCap.Flat, PenLineJoin.Miter);
        dc.DrawGeometry(null, arrow, Poly(false, new(6, 4), new(12, 4), new(12, 7)));
        dc.DrawGeometry(Rgb(0xC04020), null, Poly(true, new(10, 6.5), new(14, 6.5), new(12, 9)));
    }

    private static void SelectAll(DrawingContext dc)
    {
        dc.DrawRectangle(Rgb(0x90C0F0), new Pen(Rgb(0x204080), 0.8, new DashStyle(new[] { 1.875, 1.25 }, 0)), new Rect(1.5, 1.5, 13, 13));
        var lines = Stroke(0x404040, 1);
        for (var y = 5; y <= 11; y += 3)
        {
            dc.DrawLine(lines, new(4, y), new(12, y));
        }
    }

    private static void Zoom(DrawingContext dc, bool plus)
    {
        Find(dc);
        var sign = Stroke(0x202020, 1);
        dc.DrawLine(sign, new(4.5, 6.5), new(8.5, 6.5));
        if (plus)
        {
            dc.DrawLine(sign, new(6.5, 4.5), new(6.5, 8.5));
        }
    }

    private static void Bullets(DrawingContext dc)
    {
        var line = Stroke(0x303030, 1);
        for (var i = 0; i < 3; i++)
        {
            var y = 3.5 + (i * 4.5);
            dc.DrawEllipse(Rgb(0x2050B0), null, new Point(3, y), 1.4, 1.4);
            dc.DrawLine(line, new(6, y), new(14.5, y));
        }
    }

    private static void WordPad(DrawingContext dc)
    {
        Page(dc, Lines);
        dc.DrawLine(Stroke(0x2060C0, 2), new(8, 13), new(15, 6));
        dc.DrawLine(Stroke(0x202020, 2), new(7, 14), new(8, 13));
    }

    private static void Palette(DrawingContext dc)
    {
        dc.DrawEllipse(Rgb(0xE8D0A8), Stroke(0x907040, 0.8), new Point(8, 8), 7, 6);
        uint[] colors = [0xD03030, 0x309030, 0x3060D0, 0xF0C020];
        Point[] points = [new(5, 6), new(8.5, 4.5), new(12, 6.5), new(11.5, 10.5)];
        for (var i = 0; i < 4; i++)
        {
            dc.DrawEllipse(Rgb(colors[i]), null, points[i], 1.5, 1.5);
        }
    }

    // A clockwise circular arrow: an open ring with a solid head at its end, pointing into the gap at the top.
    private static void Rotate(DrawingContext dc)
    {
        const uint green = 0x208040;
        var center = new Point(8, 8.5);
        const double radius = 5;
        const double start = -50 * Math.PI / 180;  // the tail, right of the gap
        const double end = 220 * Math.PI / 180;    // the head, left of the gap, after a clockwise sweep
        Point At(double angle) => new(center.X + (radius * Math.Cos(angle)), center.Y + (radius * Math.Sin(angle)));

        var ring = new StreamGeometry();
        using (var c = ring.Open())
        {
            c.BeginFigure(At(start), false);
            c.ArcTo(At(end), new Size(radius, radius), 0, isLargeArc: true, SweepDirection.Clockwise);
            c.EndFigure(false);
        }

        dc.DrawGeometry(null, Stroke(green, 1.6, PenLineCap.Flat), ring);

        // The head: a triangle on the ring's end, along the clockwise tangent (-sin, cos).
        var tip = At(end);
        var tangent = new Vector(-Math.Sin(end), Math.Cos(end));
        var normal = new Vector(Math.Cos(end), Math.Sin(end));
        dc.DrawGeometry(Rgb(green), null, Poly(true, tip + (tangent * 3), tip + (normal * 2.4), tip - (normal * 2.4)));
    }

    // Crop marks: two L-shaped bars crossing at the corners of the crop frame, each running past the other.
    private static void Crop(DrawingContext dc)
    {
        var e = Stroke(0x303030, 1.5, PenLineCap.Square, PenLineJoin.Miter);
        dc.DrawGeometry(null, e, Poly(false, new(4.5, 1.5), new(4.5, 11.5), new(14.5, 11.5)));
        dc.DrawGeometry(null, e, Poly(false, new(1.5, 4.5), new(11.5, 4.5), new(11.5, 14.5)));
    }
}
