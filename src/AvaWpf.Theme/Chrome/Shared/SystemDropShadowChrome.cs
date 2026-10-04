// Ported from WPF $W/Themes/Shared/Microsoft/Windows/Themes/SystemDropShadowChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaWpf.Chrome;

/// <summary>
/// The drop shadow under menus, tooltips and popups, as WPF's <c>SystemDropShadowChrome</c>. Nothing is drawn while
/// the <c>SystemParameters.DropShadow</c> resource is <see langword="false"/>.
/// </summary>
public sealed class SystemDropShadowChrome : Decorator
{
    /// <summary>The token name of the default shadow color (<c>&lt;Family&gt;.Chrome.SystemDropShadowChrome.Color</c>).</summary>
    private const string ColorToken = "SystemDropShadowChrome.Color";

    /// <summary>The resource key of the <c>SystemParameters.DropShadow</c> setting.</summary>
    private const string DropShadowKey = "SystemParameters.DropShadow";

    /// <summary>The shadow's offset and blur width, in DIPs (WPF's <c>ShadowDepth</c>).</summary>
    private const double ShadowDepth = 5;

    private const int TopLeft = 0;
    private const int Top = 1;
    private const int TopRight = 2;
    private const int Left = 3;
    private const int Center = 4;
    private const int Right = 5;
    private const int BottomLeft = 6;
    private const int Bottom = 7;
    private const int BottomRight = 8;

    /// <summary>The default shadow color, WPF's <c>#71000000</c>.</summary>
    private static readonly Color s_defaultColor = Color.FromArgb(0x71, 0x00, 0x00, 0x00);

    /// <summary>Defines the <see cref="Color"/> property.</summary>
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<SystemDropShadowChrome, Color>(nameof(Color), s_defaultColor);

    /// <summary>Defines the <see cref="CornerRadius"/> property.</summary>
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<SystemDropShadowChrome, CornerRadius>(nameof(CornerRadius), validate: IsCornerRadiusValid);

    // The nine brushes: 0 TopLeft, 1 Top, 2 TopRight, 3 Left, 4 Center, 5 Right, 6 BottomLeft, 7 Bottom, 8 BottomRight.
    private IBrush[]? _brushes;
    private Color _brushesColor;
    private CornerRadius _brushesCornerRadius;

    static SystemDropShadowChrome()
    {
        AffectsRender<SystemDropShadowChrome>(ColorProperty, CornerRadiusProperty);
    }

    /// <summary>Initializes a new instance of the <see cref="SystemDropShadowChrome"/> class.</summary>
    public SystemDropShadowChrome()
    {
        ResourcesChanged += (_, _) => InvalidateVisual();
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    /// <summary>
    /// The token names this chrome reads, relative to <c>&lt;Family&gt;.Chrome.</c> (see <see cref="ChromeResources"/>).
    /// </summary>
    public static IReadOnlyList<string> TokenSet { get; } = [ColorToken];

    /// <summary>
    /// The color of the shadow's dark center. When the property is not set, the family's
    /// <c>SystemDropShadowChrome.Color</c> token is used, falling back to WPF's default <c>#71000000</c>.
    /// </summary>
    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>The corner radius of the element casting the shadow; each radius is clamped to half the shadow's center.</summary>
    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>Draws the nine-region shadow, as WPF's <c>SystemDropShadowChrome.OnRender</c>.</summary>
    /// <param name="context">The drawing context.</param>
    public override void Render(DrawingContext context)
    {
        if (ChromeResources.Resource(this, DropShadowKey) is false)
        {
            return;
        }

        var size = Bounds.Size;
        var cornerRadius = CornerRadius;
        var color = ResolveColor();

        double sbLeft = ShadowDepth, sbTop = ShadowDepth;
        double sbRight = ShadowDepth + size.Width, sbBottom = ShadowDepth + size.Height;

        if (size.Width <= 0 || size.Height <= 0 || color.A == 0)
        {
            return;
        }

        // The shadow has a dark center the size of the shadow bounds deflated by ShadowDepth on each side.
        var centerWidth = sbRight - sbLeft - 2 * ShadowDepth;
        var centerHeight = sbBottom - sbTop - 2 * ShadowDepth;

        // Clamp the corner radii to half the side of the center; a negative center is clamped at zero.
        var maxRadius = Math.Max(0.0, Math.Min(centerWidth * 0.5, centerHeight * 0.5));
        var tl = Math.Min(cornerRadius.TopLeft, maxRadius);
        var tr = Math.Min(cornerRadius.TopRight, maxRadius);
        var bl = Math.Min(cornerRadius.BottomLeft, maxRadius);
        var br = Math.Min(cornerRadius.BottomRight, maxRadius);

        var brushes = GetBrushes(color, new CornerRadius(tl, tr, br, bl));

        // Round every edge to device pixels in place of WPF's guideline set.
        var scale = PixelSnap.Scale(this);
        double S(double v) => PixelSnap.Round(v, scale);

        var centerTop = sbTop + ShadowDepth;
        var centerLeft = sbLeft + ShadowDepth;
        var centerRight = sbRight - ShadowDepth;
        var centerBottom = sbBottom - ShadowDepth;

        double[] gx =
        [
            S(centerLeft), S(centerLeft + tl), S(centerRight - tr), S(centerLeft + bl), S(centerRight - br), S(centerRight),
        ];
        double[] gy =
        [
            S(centerTop), S(centerTop + tl), S(centerTop + tr), S(centerBottom - bl), S(centerBottom - br), S(centerBottom),
        ];
        double left = S(sbLeft), top = S(sbTop), right = S(sbRight), bottom = S(sbBottom);

        // Top row. The corner squares are ShadowDepth larger than the radius to hold the blur.
        Fill(context, brushes[TopLeft], left, top, gx[1], gy[1]);
        if (gx[2] - gx[1] > 0)
        {
            Fill(context, brushes[Top], gx[1], top, gx[2], gy[0]);
        }

        Fill(context, brushes[TopRight], gx[2], top, right, gy[2]);

        // Middle row.
        if (gy[3] - gy[1] > 0)
        {
            Fill(context, brushes[Left], left, gy[1], gx[0], gy[3]);
        }

        if (gy[4] - gy[2] > 0)
        {
            Fill(context, brushes[Right], gx[5], gy[2], right, gy[4]);
        }

        // Bottom row.
        Fill(context, brushes[BottomLeft], left, gy[3], gx[3], bottom);
        if (gx[4] - gx[3] > 0)
        {
            Fill(context, brushes[Bottom], gx[3], gy[5], gx[4], bottom);
        }

        Fill(context, brushes[BottomRight], gx[4], gy[4], right, bottom);

        // Fill the center. With square corners it is one rectangle; otherwise it is a polygon whose missing corners
        // are covered by the radial corner brushes drawn above.
        if (tl == 0 && tr == 0 && bl == 0 && br == 0)
        {
            Fill(context, brushes[Center], gx[0], gy[0], gx[5], gy[5]);
            return;
        }

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            // Defined counter-clockwise.
            if (tl > 0)
            {
                g.BeginFigure(new Point(gx[1], gy[0]), true);
                g.LineTo(new Point(gx[1], gy[1]));
                g.LineTo(new Point(gx[0], gy[1]));
            }
            else
            {
                g.BeginFigure(new Point(gx[0], gy[0]), true);
            }

            if (bl > 0)
            {
                g.LineTo(new Point(gx[0], gy[3]));
                g.LineTo(new Point(gx[3], gy[3]));
                g.LineTo(new Point(gx[3], gy[5]));
            }
            else
            {
                g.LineTo(new Point(gx[0], gy[5]));
            }

            if (br > 0)
            {
                g.LineTo(new Point(gx[4], gy[5]));
                g.LineTo(new Point(gx[4], gy[4]));
                g.LineTo(new Point(gx[5], gy[4]));
            }
            else
            {
                g.LineTo(new Point(gx[5], gy[5]));
            }

            if (tr > 0)
            {
                g.LineTo(new Point(gx[5], gy[2]));
                g.LineTo(new Point(gx[2], gy[2]));
                g.LineTo(new Point(gx[2], gy[0]));
            }
            else
            {
                g.LineTo(new Point(gx[5], gy[0]));
            }

            g.EndFigure(true);
        }

        context.DrawGeometry(brushes[Center], null, geometry);
    }

    private static bool IsCornerRadiusValid(CornerRadius cr) =>
        !(cr.TopLeft < 0.0 || cr.TopRight < 0.0 || cr.BottomLeft < 0.0 || cr.BottomRight < 0.0 ||
          double.IsNaN(cr.TopLeft) || double.IsNaN(cr.TopRight) || double.IsNaN(cr.BottomLeft) || double.IsNaN(cr.BottomRight) ||
          double.IsInfinity(cr.TopLeft) || double.IsInfinity(cr.TopRight) || double.IsInfinity(cr.BottomLeft) || double.IsInfinity(cr.BottomRight));

    private static void Fill(DrawingContext context, IBrush brush, double left, double top, double right, double bottom)
    {
        if (right > left && bottom > top)
        {
            context.DrawRectangle(brush, null, new Rect(left, top, right - left, bottom - top));
        }
    }

    // The common stops of the gradient brushes, scaled into 0..1 over cornerRadius + ShadowDepth, following the Win32
    // drop-shadow falloff (alpha 100 %, 74.3 %, 38.1 %, 12.4 %, 2.7 %, 0 % at 0.5 px steps from the radius).
    private static ImmutableGradientStop[] CreateStops(Color c, double cornerRadius)
    {
        var gradientScale = 1 / (cornerRadius + ShadowDepth);
        return
        [
            new((0.5 + cornerRadius) * gradientScale, c),
            new((1.5 + cornerRadius) * gradientScale, WithAlpha(c, (byte)(.74336 * c.A))),
            new((2.5 + cornerRadius) * gradientScale, WithAlpha(c, (byte)(.38053 * c.A))),
            new((3.5 + cornerRadius) * gradientScale, WithAlpha(c, (byte)(.12389 * c.A))),
            new((4.5 + cornerRadius) * gradientScale, WithAlpha(c, (byte)(.02654 * c.A))),
            new((5 + cornerRadius) * gradientScale, WithAlpha(c, 0)),
        ];
    }

    private static Color WithAlpha(Color c, byte a) => Color.FromArgb(a, c.R, c.G, c.B);

    private static IBrush Linear(ImmutableGradientStop[] stops, double x1, double y1, double x2, double y2) =>
        new ImmutableLinearGradientBrush(
            stops,
            startPoint: new RelativePoint(x1, y1, RelativeUnit.Relative),
            endPoint: new RelativePoint(x2, y2, RelativeUnit.Relative));

    private static IBrush Radial(ImmutableGradientStop[] stops, double cx, double cy)
    {
        var center = new RelativePoint(cx, cy, RelativeUnit.Relative);
        var radius = new RelativeScalar(1, RelativeUnit.Relative);
        return new ImmutableRadialGradientBrush(stops, center: center, gradientOrigin: center, radiusX: radius, radiusY: radius);
    }

    private static IBrush[] CreateBrushes(Color c, CornerRadius cornerRadius)
    {
        var brushes = new IBrush[9];
        brushes[Center] = new ImmutableSolidColorBrush(c);

        // Sides.
        var sideStops = CreateStops(c, 0);
        brushes[Top] = Linear(sideStops, 0, 1, 0, 0);
        brushes[Left] = Linear(sideStops, 1, 0, 0, 0);
        brushes[Right] = Linear(sideStops, 0, 0, 1, 0);
        brushes[Bottom] = Linear(sideStops, 0, 0, 0, 1);

        // Corners reuse the side stops when the radius is 0, or the stops of an earlier corner with the same radius.
        var topLeftStops = cornerRadius.TopLeft == 0 ? sideStops : CreateStops(c, cornerRadius.TopLeft);
        brushes[TopLeft] = Radial(topLeftStops, 1, 1);

        var topRightStops = cornerRadius.TopRight == 0 ? sideStops
            : cornerRadius.TopRight == cornerRadius.TopLeft ? topLeftStops
            : CreateStops(c, cornerRadius.TopRight);
        brushes[TopRight] = Radial(topRightStops, 0, 1);

        var bottomLeftStops = cornerRadius.BottomLeft == 0 ? sideStops
            : cornerRadius.BottomLeft == cornerRadius.TopLeft ? topLeftStops
            : cornerRadius.BottomLeft == cornerRadius.TopRight ? topRightStops
            : CreateStops(c, cornerRadius.BottomLeft);
        brushes[BottomLeft] = Radial(bottomLeftStops, 1, 0);

        var bottomRightStops = cornerRadius.BottomRight == 0 ? sideStops
            : cornerRadius.BottomRight == cornerRadius.TopLeft ? topLeftStops
            : cornerRadius.BottomRight == cornerRadius.TopRight ? topRightStops
            : cornerRadius.BottomRight == cornerRadius.BottomLeft ? bottomLeftStops
            : CreateStops(c, cornerRadius.BottomRight);
        brushes[BottomRight] = Radial(bottomRightStops, 0, 0);

        return brushes;
    }

    private Color ResolveColor()
    {
        if (IsSet(ColorProperty))
        {
            return Color;
        }

        return ChromeResources.Color(this, ColorToken) ?? Color;
    }

    // The brushes are cached per instance and rebuilt when the color or clamped radii change.
    private IBrush[] GetBrushes(Color c, CornerRadius cornerRadius)
    {
        if (_brushes is null || _brushesColor != c || _brushesCornerRadius != cornerRadius)
        {
            _brushes = CreateBrushes(c, cornerRadius);
            _brushesColor = c;
            _brushesCornerRadius = cornerRadius;
        }

        return _brushes;
    }
}
