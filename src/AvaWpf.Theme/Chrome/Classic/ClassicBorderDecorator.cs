// Ported from WPF $W/Themes/PresentationFramework.Classic/Microsoft/Windows/Themes/ClassicBorderDecorator.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaWpf.Chrome.Classic;

/// <summary>The Classic 3D bevel border (WPF <c>Microsoft.Windows.Themes.ClassicBorderDecorator</c>).</summary>
/// <remarks>
/// Bevel colors are the <c>SystemColors</c> control brushes. A solid non-control <see cref="Background"/> derives
/// them with the Win32 appearance dialog's rules instead, except for the sunken styles.
/// </remarks>
public sealed class ClassicBorderDecorator : Decorator
{
    private const string ControlColorKey = "SystemColors.ControlColor";
    private const string WindowFrameColorKey = "SystemColors.WindowFrameColor";
    private const string WindowFrameBrushKey = "SystemColors.WindowFrameBrush";
    private const string ControlLightBrushKey = "SystemColors.ControlLightBrush";
    private const string ControlLightLightBrushKey = "SystemColors.ControlLightLightBrush";
    private const string ControlDarkBrushKey = "SystemColors.ControlDarkBrush";
    private const string ControlDarkDarkBrushKey = "SystemColors.ControlDarkDarkBrush";

    // A transparent brush compared by reference, as WPF's frozen empty SolidColorBrush. Must precede the properties
    // that use it as a default.
    private static readonly IBrush s_classicBorderBrush = new ImmutableSolidColorBrush(default(Color));

    /// <summary>Defines the <see cref="Background"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<ClassicBorderDecorator>();

    /// <summary>Defines the <see cref="BorderStyle"/> property.</summary>
    public static readonly StyledProperty<ClassicBorderStyle> BorderStyleProperty =
        AvaloniaProperty.Register<ClassicBorderDecorator, ClassicBorderStyle>(
            nameof(BorderStyle), ClassicBorderStyle.Raised, validate: v => Enum.IsDefined(v));

    /// <summary>Defines the <see cref="BorderBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<ClassicBorderDecorator>(new StyledPropertyMetadata<IBrush?>(new Optional<IBrush?>(s_classicBorderBrush)));

    /// <summary>Defines the <see cref="BorderThickness"/> property.</summary>
    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<ClassicBorderDecorator>();

    private static readonly Geometry s_topLeftArcGeometry = CreateArc(SweepDirection.Clockwise);
    private static readonly Geometry s_bottomRightArcGeometry = CreateArc(SweepDirection.CounterClockwise);

    // Brushes computed from a custom background: light, light-light, dark, dark-dark (null = use the system brushes).
    private IBrush?[]? _customBrushes;
    private Color _customBackground;
    private Color _customWindowFrame;

    // System brushes looked up from resources; cleared when resources or the theme variant change.
    private IBrush? _sysLight;
    private IBrush? _sysLightLight;
    private IBrush? _sysDark;
    private IBrush? _sysDarkDark;
    private IBrush? _sysWindowFrame;
    private bool _sysResolved;

    private Geometry? _borderGeometry;
    private Rect _borderGeometryBounds;
    private Thickness _borderGeometryThickness;

    // Geometries of the tab styles, for the (rotated) bounds they were made for.
    private Rect _tabBounds;
    private Geometry? _tabHighlight1;
    private Geometry? _tabShadow1;
    private Geometry? _tabHighlight2;
    private Geometry? _tabShadow2;

    private double _scale = 1.0;

    static ClassicBorderDecorator()
    {
        AffectsRender<ClassicBorderDecorator>(BackgroundProperty, BorderStyleProperty, BorderBrushProperty, BorderThicknessProperty);
        AffectsMeasure<ClassicBorderDecorator>(BorderStyleProperty, BorderThicknessProperty);
        AffectsArrange<ClassicBorderDecorator>(BorderStyleProperty, BorderThicknessProperty);
    }

    /// <summary>Initializes a new instance of the <see cref="ClassicBorderDecorator"/> class.</summary>
    public ClassicBorderDecorator()
    {
        ResourcesChanged += (_, _) => InvalidateResources();
        ActualThemeVariantChanged += (_, _) => InvalidateResources();
    }

    /// <summary>The default <see cref="BorderBrush"/>, which selects the 3D look; any other brush draws a flat border.</summary>
    public static IBrush ClassicBorderBrush => s_classicBorderBrush;

    /// <summary>The token names this chrome reads; empty, since every color comes from <c>SystemColors</c> or <see cref="Background"/>.</summary>
    public static IReadOnlyList<string> TokenSet { get; } = [];

    /// <summary>The brush that fills the area inside the border.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>The kind of border to draw. Default <see cref="ClassicBorderStyle.Raised"/>.</summary>
    public ClassicBorderStyle BorderStyle
    {
        get => GetValue(BorderStyleProperty);
        set => SetValue(BorderStyleProperty, value);
    }

    /// <summary>The border brush; <see cref="ClassicBorderBrush"/> (the default) draws the 3D style.</summary>
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    /// <summary>The border thickness around the child, split evenly between the style's bevel rings.</summary>
    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>Draws the border and background.</summary>
    /// <param name="context">The drawing context.</param>
    public override void Render(DrawingContext context)
    {
        _scale = PixelSnap.Scale(this);
        EnsureBrushes();

        var borderBrush = BorderBrush;
        var style = BorderStyle;
        var borderThickness = BorderThickness;

        var classicThickness = GetClassicBorderThickness();

        // Thickness of a single border ring.
        var singleThickness = ScaleThickness(borderThickness, 1.0 / classicThickness);

        var bounds = new Rect(Bounds.Size);

        // A 1 px window-frame border when focused or pressed (for consistency with Internet Explorer).
        if (style == ClassicBorderStyle.RaisedFocused || style == ClassicBorderStyle.RaisedPressed)
        {
            DrawBorder(_sysWindowFrame, singleThickness, context, ref bounds);
        }

        var isTabStyle = IsTabStyle(style);

        if (ReferenceEquals(borderBrush, s_classicBorderBrush) || isTabStyle || style == ClassicBorderStyle.RadioButton)
        {
            switch (style)
            {
                case ClassicBorderStyle.Raised:
                case ClassicBorderStyle.RaisedFocused:
                    // Focused already has the 1 px border drawn above.
                    DrawRaisedBorder(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.RaisedPressed:
                    DrawRaisedPressedBorder(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.Sunken:
                    DrawSunkenBorder(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.Etched:
                    DrawEtchedBorder(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.HorizontalLine:
                    DrawHorizontalLine(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.VerticalLine:
                    DrawVerticalLine(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.TabLeft:
                    DrawTabLeft(context, ref bounds);
                    break;
                case ClassicBorderStyle.TabTop:
                    DrawTabTop(context, ref bounds);
                    break;
                case ClassicBorderStyle.TabRight:
                    DrawTabRight(context, ref bounds);
                    break;
                case ClassicBorderStyle.TabBottom:
                    DrawTabBottom(context, ref bounds);
                    break;
                case ClassicBorderStyle.ThinRaised:
                    DrawThinRaisedBorder(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.ThinPressed:
                    DrawThinPressedBorder(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.AltRaised:
                    DrawAltRaisedBorder(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.AltPressed:
                    DrawAltPressedBorder(singleThickness, context, ref bounds);
                    break;
                case ClassicBorderStyle.RadioButton:
                    DrawRadioButtonBorder(context, ref bounds);
                    return;
            }
        }
        else
        {
            // A regular flat border.
            DrawBorder(borderBrush, borderThickness, context, ref bounds);
        }

        var background = Background;
        if (background != null && bounds.Width > 0.0 && bounds.Height > 0.0)
        {
            Fill(context, background, bounds);
        }
    }

    /// <summary>Measures the child inside <see cref="BorderThickness"/>.</summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        var borderSize = HelperCollapseThickness(BorderThickness);

        var child = Child;
        if (child != null)
        {
            var isWidthTooSmall = availableSize.Width < borderSize.Width;
            var isHeightTooSmall = availableSize.Height < borderSize.Height;

            var childConstraint = new Size(
                isWidthTooSmall ? 0 : availableSize.Width - borderSize.Width,
                isHeightTooSmall ? 0 : availableSize.Height - borderSize.Height);

            child.Measure(childConstraint);

            var desired = child.DesiredSize;
            return new Size(
                isWidthTooSmall ? desired.Width : desired.Width + borderSize.Width,
                isHeightTooSmall ? desired.Height : desired.Height + borderSize.Height);
        }

        return new Size(Math.Min(borderSize.Width, availableSize.Width), Math.Min(borderSize.Height, availableSize.Height));
    }

    /// <summary>Arranges the child inside <see cref="BorderThickness"/>; pressed styles shift it 1 px right and down.</summary>
    /// <param name="finalSize">The final size.</param>
    /// <returns>The size used.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var child = Child;
        if (child != null)
        {
            var border = BorderThickness;
            var borderWidth = border.Left + border.Right;
            var borderHeight = border.Top + border.Bottom;

            var childArrangeRect = default(Rect);
            if (finalSize.Width >= borderWidth && finalSize.Height >= borderHeight)
            {
                var x = border.Left;
                var y = border.Top;

                var style = BorderStyle;
                if (style == ClassicBorderStyle.RaisedPressed ||
                    style == ClassicBorderStyle.ThinPressed ||
                    style == ClassicBorderStyle.AltPressed)
                {
                    x += 1.0;
                    y += 1.0;
                }

                childArrangeRect = new Rect(x, y, finalSize.Width - borderWidth, finalSize.Height - borderHeight);
            }

            child.Arrange(childArrangeRect);
        }

        return finalSize;
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // When the style or background changes, the tab geometries are invalid (WPF's BorderBrushesChanged).
        if (change.Property == BackgroundProperty || change.Property == BorderStyleProperty)
        {
            ClearTabCache();
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // HLS color conversions, single-precision as in WPF.

    private static (float Hue, float Lightness, float Saturation) RgbToHls(float red, float green, float blue)
    {
        var min = Math.Min(red, Math.Min(green, blue));
        var max = Math.Max(red, Math.Max(green, blue));

        float hue = 0;
        var lightness = (min + max) / 2;
        float saturation = 0;

        if (min != max)
        {
            var delta = max - min;
            saturation = lightness <= 0.5f ? delta / (min + max) : delta / (2 - min - max);

            // Force saturation into 0..1 in case of numerical instability.
            saturation = Math.Max(0.0f, Math.Min(saturation, 1.0f));
            if (red == max)
            {
                hue = (green - blue) / delta;
            }
            else if (green == max)
            {
                hue = 2 + ((blue - red) / delta);
            }
            else
            {
                hue = 4 + ((red - green) / delta);
            }

            if (hue < 0)
            {
                hue += 6;
            }

            hue /= 6;
        }

        return (hue, lightness, saturation);
    }

    private static (float Red, float Green, float Blue) HlsToRgb(float hue, float lightness, float saturation)
    {
        float red, green, blue;
        if (saturation == 0.0f)
        {
            red = green = blue = lightness;
        }
        else
        {
            // Shift hue into the [0, 6) interval.
            var h = (float)((hue - Math.Floor(hue)) * 6);

            var m2 = lightness <= 0.5f ? lightness * (1 + saturation) : lightness + saturation - (lightness * saturation);
            var m1 = (2 * lightness) - m2;
            red = HlsValue(m1, m2, h + 2);
            green = HlsValue(m1, m2, h);
            blue = HlsValue(m1, m2, h - 2);
        }

        return (red, green, blue);
    }

    private static float HlsValue(float n1, float n2, float hue)
    {
        if (hue < 0)
        {
            hue += 6;
        }
        else if (hue >= 6)
        {
            hue -= 6;
        }

        if (hue < 1)
        {
            return n1 + ((n2 - n1) * hue);
        }

        if (hue < 3)
        {
            return n2;
        }

        if (hue < 4)
        {
            return n1 + ((n2 - n1) * (4 - hue));
        }

        return n1;
    }

    private static Color AdjustLightness(Color color, Func<float, float> adjust)
    {
        var (h, l, s) = RgbToHls(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f);
        var (r, g, b) = HlsToRgb(h, adjust(l), s);
        return Color.FromArgb(color.A, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    // ControlLightLightColor from the control color, as the Win32 appearance dialog: lightness = (lightness + 1) / 2.
    private static Color GetControlLightLightColor(Color controlColor) => AdjustLightness(controlColor, l => (l + 1.0f) * 0.5f);

    // ControlDarkColor from the control color, as the Win32 appearance dialog: lightness × 0.666.
    private static Color GetControlDarkColor(Color controlColor) => AdjustLightness(controlColor, l => l * 0.666f);

    // ---------------------------------------------------------------------------------------------------------------
    // Brushes.

    private IBrush? LightBrush => _customBrushes != null ? _customBrushes[0] : _sysLight;

    private IBrush? LightLightBrush => _customBrushes != null ? _customBrushes[1] : _sysLightLight;

    private IBrush? DarkBrush => _customBrushes != null ? _customBrushes[2] : _sysDark;

    private IBrush? DarkDarkBrush => _customBrushes != null ? _customBrushes[3] : _sysDarkDark;

    private void InvalidateResources()
    {
        _sysResolved = false;
        _customBrushes = null;
        InvalidateVisual();
    }

    private IBrush? FindBrush(string key) => ChromeResources.Resource(this, key) switch
    {
        IBrush b => b,
        Color c => new ImmutableSolidColorBrush(c),
        _ => null,
    };

    private Color? FindColor(string key) => ChromeResources.Resource(this, key) switch
    {
        Color c => c,
        ISolidColorBrush b => b.Color,
        _ => null,
    };

    // Resolves the system brushes, then computes the 3D highlights and shadows for a solid background that is not the
    // control color. The sunken styles (Sunken and RadioButton) always use the system colors.
    private void EnsureBrushes()
    {
        if (!_sysResolved)
        {
            _sysLight = FindBrush(ControlLightBrushKey);
            _sysLightLight = FindBrush(ControlLightLightBrushKey);
            _sysDark = FindBrush(ControlDarkBrushKey);
            _sysDarkDark = FindBrush(ControlDarkDarkBrushKey);
            _sysWindowFrame = FindBrush(WindowFrameBrushKey);
            _sysResolved = true;
        }

        var style = BorderStyle;
        if (style != ClassicBorderStyle.Sunken && style != ClassicBorderStyle.RadioButton &&
            Background is ISolidColorBrush controlBrush &&
            controlBrush.Color != FindColor(ControlColorKey) &&
            controlBrush.Color.A > 0x00)
        {
            var controlColor = controlBrush.Color;
            var windowFrame = FindColor(WindowFrameColorKey) ?? Colors.Black;

            if (_customBrushes == null || !ReferenceEquals(_customBrushes[0], controlBrush) ||
                controlColor != _customBackground || windowFrame != _customWindowFrame)
            {
                // Light = Control; LightLight luma = (luma + 1) / 2; Dark luma = luma × 0.666;
                // DarkDark = (Dark + WindowFrame) / 2.
                var darkColor = GetControlDarkColor(controlColor);
                var darkDarkColor = Color.FromArgb(
                    (byte)((darkColor.A + windowFrame.A) / 2),
                    (byte)((darkColor.R + windowFrame.R) / 2),
                    (byte)((darkColor.G + windowFrame.G) / 2),
                    (byte)((darkColor.B + windowFrame.B) / 2));

                _customBrushes =
                [
                    controlBrush,
                    new ImmutableSolidColorBrush(GetControlLightLightColor(controlColor)),
                    new ImmutableSolidColorBrush(darkColor),
                    new ImmutableSolidColorBrush(darkDarkColor),
                ];
                _customBackground = controlColor;
                _customWindowFrame = windowFrame;
            }
        }
        else
        {
            _customBrushes = null;
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Layout helpers.

    private static Size HelperCollapseThickness(Thickness th) => new(th.Left + th.Right, th.Top + th.Bottom);

    private static Rect HelperDeflateRect(Rect rt, Thickness thick) => new(
        rt.Left + thick.Left,
        rt.Top + thick.Top,
        Math.Max(0.0, rt.Width - thick.Left - thick.Right),
        Math.Max(0.0, rt.Height - thick.Top - thick.Bottom));

    private static Thickness ScaleThickness(Thickness t, double s) => new(t.Left * s, t.Top * s, t.Right * s, t.Bottom * s);

    // The number of bevel rings each style draws, used to split BorderThickness into single rings.
    private double GetClassicBorderThickness()
    {
        // A non-classic brush: 1 for the focused border and 1 for offsetting content when pressed.
        if (!ReferenceEquals(BorderBrush, s_classicBorderBrush))
        {
            return 2.0;
        }

        switch (BorderStyle)
        {
            case ClassicBorderStyle.Raised:
            case ClassicBorderStyle.RaisedPressed:
            case ClassicBorderStyle.RaisedFocused:
                return 3.0;
            case ClassicBorderStyle.Sunken:
            case ClassicBorderStyle.Etched:
                return 2.0;
            case ClassicBorderStyle.HorizontalLine:
            case ClassicBorderStyle.VerticalLine:
                return 1.0;
            case ClassicBorderStyle.TabRight:
            case ClassicBorderStyle.TabLeft:
            case ClassicBorderStyle.TabTop:
            case ClassicBorderStyle.TabBottom:
                return 2.0;
            case ClassicBorderStyle.None:
            case ClassicBorderStyle.ThinRaised:
            case ClassicBorderStyle.ThinPressed:
                return 1.0;
            case ClassicBorderStyle.AltRaised:
            case ClassicBorderStyle.AltPressed:
                return 2.0;
        }

        return 0.0;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Border drawing.

    // Fills a rectangle with its edges on device pixels (WPF's guideline set at i × ring thickness from each side).
    private void Fill(DrawingContext dc, IBrush? brush, Rect rect)
    {
        if (brush != null)
        {
            dc.DrawRectangle(brush, null, PixelSnap.Rect(rect, _scale));
        }
    }

    // True when the border can be drawn with overlapping rectangles.
    private static bool IsSimpleBorderBrush(IBrush? borderBrush) =>
        borderBrush is ISolidColorBrush solid && (solid.Color.A == 0xFF || solid.Color.A == 0x00);

    private static Geometry GenerateBorderGeometry(Rect rect, Thickness borderThickness)
    {
        var inner = HelperDeflateRect(rect, borderThickness);
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.SetFillRule(FillRule.EvenOdd);
            AddRect(g, rect);
            AddRect(g, inner);
        }

        return geometry;

        static void AddRect(StreamGeometryContext g, Rect r)
        {
            g.BeginFigure(r.TopLeft, true);
            g.LineTo(r.TopRight);
            g.LineTo(r.BottomRight);
            g.LineTo(r.BottomLeft);
            g.EndFigure(true);
        }
    }

    private Geometry GetBorder(Rect bounds, Thickness borderThickness)
    {
        if (_borderGeometry == null || _borderGeometryBounds != bounds || _borderGeometryThickness != borderThickness)
        {
            _borderGeometry = GenerateBorderGeometry(bounds, borderThickness);
            _borderGeometryBounds = bounds;
            _borderGeometryThickness = borderThickness;
        }

        return _borderGeometry;
    }

    // Draws a border around the area and deflates it; a non-solid brush is drawn as a path.
    private void DrawBorder(IBrush? borderBrush, Thickness borderThickness, DrawingContext dc, ref Rect bounds)
    {
        var borderSize = HelperCollapseThickness(borderThickness);

        if (borderSize.Width <= 0.0 && borderSize.Height <= 0.0)
        {
            return;
        }

        // Not enough space for the entire border: fill the area with the brush.
        if (borderSize.Width > bounds.Width || borderSize.Height > bounds.Height)
        {
            if (borderBrush != null && bounds.Width > 0.0 && bounds.Height > 0.0)
            {
                Fill(dc, borderBrush, bounds);
            }

            bounds = default;
            return;
        }

        if (IsSimpleBorderBrush(borderBrush))
        {
            if (borderThickness.Top > 0.0)
            {
                Fill(dc, borderBrush, new Rect(bounds.Left, bounds.Top, bounds.Width, borderThickness.Top));
            }

            if (borderThickness.Left > 0.0)
            {
                Fill(dc, borderBrush, new Rect(bounds.Left, bounds.Top, borderThickness.Left, bounds.Height));
            }

            if (borderThickness.Right > 0.0)
            {
                Fill(dc, borderBrush, new Rect(bounds.Right - borderThickness.Right, bounds.Top, borderThickness.Right, bounds.Height));
            }

            if (borderThickness.Bottom > 0.0)
            {
                Fill(dc, borderBrush, new Rect(bounds.Left, bounds.Bottom - borderThickness.Bottom, bounds.Width, borderThickness.Bottom));
            }
        }
        else if (borderBrush != null)
        {
            var outer = PixelSnap.Rect(bounds, _scale);
            var inner = PixelSnap.Rect(HelperDeflateRect(bounds, borderThickness), _scale);
            var snapped = new Thickness(inner.Left - outer.Left, inner.Top - outer.Top, outer.Right - inner.Right, outer.Bottom - inner.Bottom);
            dc.DrawGeometry(borderBrush, null, GetBorder(outer, snapped));
        }

        bounds = HelperDeflateRect(bounds, borderThickness);
    }

    // Draws a border with the top and left in the highlight and the bottom and right in the shadow.
    private void DrawBorderPair(IBrush? highlight, IBrush? shadow, Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        DrawBorder(shadow, new Thickness(0, 0, singleThickness.Right, singleThickness.Bottom), dc, ref bounds);
        DrawBorder(highlight, new Thickness(singleThickness.Left, singleThickness.Top, 0, 0), dc, ref bounds);
    }

    private void DrawRaisedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < 2.0 * (singleThickness.Left + singleThickness.Right) || bounds.Height < 2.0 * (singleThickness.Top + singleThickness.Bottom))
        {
            return;
        }

        DrawBorderPair(LightLightBrush, DarkDarkBrush, singleThickness, dc, ref bounds);
        DrawBorderPair(LightBrush, DarkBrush, singleThickness, dc, ref bounds);
    }

    private void DrawRaisedPressedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < singleThickness.Left + singleThickness.Right || bounds.Height < singleThickness.Top + singleThickness.Bottom)
        {
            return;
        }

        DrawBorder(DarkBrush, singleThickness, dc, ref bounds);
    }

    private void DrawSunkenBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < 2.0 * (singleThickness.Left + singleThickness.Right) || bounds.Height < 2.0 * (singleThickness.Top + singleThickness.Bottom))
        {
            return;
        }

        DrawBorderPair(DarkBrush, LightLightBrush, singleThickness, dc, ref bounds);
        DrawBorderPair(DarkDarkBrush, LightBrush, singleThickness, dc, ref bounds);
    }

    private void DrawEtchedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < 2.0 * (singleThickness.Left + singleThickness.Right) || bounds.Height < 2.0 * (singleThickness.Top + singleThickness.Bottom))
        {
            return;
        }

        IBrush? dark = DarkBrush, lightLight = LightLightBrush;
        DrawBorderPair(dark, lightLight, singleThickness, dc, ref bounds);
        DrawBorderPair(lightLight, dark, singleThickness, dc, ref bounds);
    }

    private void DrawHorizontalLine(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Height < singleThickness.Top + singleThickness.Bottom)
        {
            return;
        }

        Fill(dc, DarkBrush, new Rect(bounds.Left, bounds.Top, bounds.Width, singleThickness.Top));
        Fill(dc, LightLightBrush, new Rect(bounds.Left, bounds.Bottom - singleThickness.Bottom, bounds.Width, singleThickness.Bottom));

        bounds = new Rect(bounds.X, bounds.Y + singleThickness.Top, bounds.Width, bounds.Height - singleThickness.Top - singleThickness.Bottom);
    }

    private void DrawVerticalLine(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < singleThickness.Left + singleThickness.Right)
        {
            return;
        }

        Fill(dc, DarkBrush, new Rect(bounds.Left, bounds.Top, singleThickness.Left, bounds.Height));
        Fill(dc, LightLightBrush, new Rect(bounds.Right - singleThickness.Right, bounds.Top, singleThickness.Right, bounds.Height));

        bounds = new Rect(bounds.X + singleThickness.Left, bounds.Y, bounds.Width - singleThickness.Left - singleThickness.Right, bounds.Height);
    }

    private void DrawThinRaisedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < singleThickness.Left + singleThickness.Right || bounds.Height < singleThickness.Top + singleThickness.Bottom)
        {
            return;
        }

        DrawBorderPair(LightLightBrush, DarkBrush, singleThickness, dc, ref bounds);
    }

    private void DrawThinPressedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < singleThickness.Left + singleThickness.Right || bounds.Height < singleThickness.Top + singleThickness.Bottom)
        {
            return;
        }

        DrawBorderPair(DarkBrush, LightLightBrush, singleThickness, dc, ref bounds);
    }

    private void DrawAltRaisedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < 2.0 * (singleThickness.Left + singleThickness.Right) || bounds.Height < 2.0 * (singleThickness.Top + singleThickness.Bottom))
        {
            return;
        }

        DrawBorderPair(LightBrush, DarkDarkBrush, singleThickness, dc, ref bounds);
        DrawBorderPair(LightLightBrush, DarkBrush, singleThickness, dc, ref bounds);
    }

    private void DrawAltPressedBorder(Thickness singleThickness, DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < singleThickness.Left + singleThickness.Right || bounds.Height < singleThickness.Top + singleThickness.Bottom)
        {
            return;
        }

        DrawBorder(DarkBrush, singleThickness, dc, ref bounds);
    }

    // The 12 × 12 radio circle: a dark-dark/dark top-left arc, a light/light-light bottom-right arc, and the background
    // in a 4 px radius circle at (6, 6).
    private void DrawRadioButtonBorder(DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < 12 || bounds.Height < 12)
        {
            return;
        }

        dc.DrawGeometry(DarkDarkBrush, DarkBrush is { } dark ? new Pen(dark, 1.0) : null, s_topLeftArcGeometry);
        dc.DrawGeometry(LightBrush, LightLightBrush is { } lightLight ? new Pen(lightLight, 1.0) : null, s_bottomRightArcGeometry);

        dc.DrawEllipse(Background, null, new Point(6, 6), 4, 4);
    }

    private static Geometry CreateArc(SweepDirection direction)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(2, 10), true);
            g.ArcTo(new Point(10, 2), new Size(4, 4), 0, false, direction);
            g.EndFigure(false);
        }

        return geometry;
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Tab styles. The top tab is drawn directly; the other sides rotate (and flip, for the lighting) the top geometry.

    private static bool IsTabStyle(ClassicBorderStyle style) =>
        style == ClassicBorderStyle.TabLeft ||
        style == ClassicBorderStyle.TabTop ||
        style == ClassicBorderStyle.TabRight ||
        style == ClassicBorderStyle.TabBottom;

    // The top and left sides with rounded corners (outer radius 3, or 2 for the inner ring).
    private static Geometry GenerateTabTopHighlightGeometry(Rect bounds, bool outerBorder)
    {
        var outerRadius = outerBorder ? 3.0 : 2.0;
        var innerRadius = outerRadius - 1.0;
        Size outerCorner = new(outerRadius, outerRadius), innerCorner = new(innerRadius, innerRadius);

        double left = bounds.Left, right = bounds.Right, top = bounds.Top, bottom = bounds.Bottom - 1.0;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            // Start at the bottom left, tracing the outside clockwise.
            g.BeginFigure(new Point(left, bottom), true);
            g.LineTo(new Point(left, top + outerRadius));
            g.ArcTo(new Point(left + outerRadius, top), outerCorner, 0.0, false, SweepDirection.Clockwise);
            g.LineTo(new Point(right - outerRadius, top));
            g.ArcTo(new Point(right - (outerRadius * 0.293), top + (outerRadius * 0.293)), outerCorner, 0.0, false, SweepDirection.Clockwise);
            g.LineTo(new Point(right - 1.0 - (innerRadius * 0.293), top + 1.0 + (innerRadius * 0.293)));
            g.ArcTo(new Point(right - outerRadius, top + 1.0), innerCorner, 0.0, false, SweepDirection.CounterClockwise);
            g.LineTo(new Point(left + outerRadius, top + 1.0));
            g.ArcTo(new Point(left + 1.0, top + outerRadius), innerCorner, 0.0, false, SweepDirection.CounterClockwise);
            g.LineTo(new Point(left + 1.0, bottom));
            g.EndFigure(true);
        }

        return geometry;
    }

    // The right side with the top rounded corner.
    private static Geometry GenerateTabTopShadowGeometry(Rect bounds, bool outerBorder)
    {
        var outerRadius = outerBorder ? 3.0 : 2.0;
        var innerRadius = outerRadius - 1.0;
        Size outerCorner = new(outerRadius, outerRadius), innerCorner = new(innerRadius, innerRadius);

        double right = bounds.Right, top = bounds.Top, bottom = bounds.Bottom - 1.0;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(right - 1.0, bottom), true);
            g.LineTo(new Point(right - 1.0, top + outerRadius));
            g.ArcTo(new Point(right - 1.0 - (innerRadius * 0.293), top + 1.0 + (innerRadius * 0.293)), innerCorner, 0.0, false, SweepDirection.CounterClockwise);
            g.LineTo(new Point(right - (outerRadius * 0.293), top + (outerRadius * 0.293)));
            g.ArcTo(new Point(right, top + outerRadius), outerCorner, 0.0, false, SweepDirection.Clockwise);
            g.LineTo(new Point(right, bottom));
            g.EndFigure(true);
        }

        return geometry;
    }

    private void ClearTabCache()
    {
        _tabHighlight1 = null;
        _tabShadow1 = null;
        _tabHighlight2 = null;
        _tabShadow2 = null;
    }

    private (Geometry Highlight1, Geometry Shadow1, Geometry Highlight2, Geometry Shadow2) GetTabGeometries(Rect bounds)
    {
        if (_tabHighlight1 == null || _tabShadow1 == null || _tabHighlight2 == null || _tabShadow2 == null || _tabBounds != bounds)
        {
            var inner = HelperDeflateRect(bounds, new Thickness(1, 1, 1, 0));
            _tabHighlight1 = GenerateTabTopHighlightGeometry(bounds, true);
            _tabShadow1 = GenerateTabTopShadowGeometry(bounds, true);
            _tabHighlight2 = GenerateTabTopHighlightGeometry(inner, false);
            _tabShadow2 = GenerateTabTopShadowGeometry(inner, false);
            _tabBounds = bounds;
        }

        return (_tabHighlight1, _tabShadow1, _tabHighlight2, _tabShadow2);
    }

    private void DrawTabGeometries(DrawingContext dc, Rect geometryBounds, IBrush? highlight1, IBrush? shadow1, IBrush? highlight2, IBrush? shadow2)
    {
        var (h1, s1, h2, s2) = GetTabGeometries(geometryBounds);
        dc.DrawGeometry(highlight1, null, h1);
        dc.DrawGeometry(shadow1, null, s1);
        dc.DrawGeometry(highlight2, null, h2);
        dc.DrawGeometry(shadow2, null, s2);
    }

    private void DrawTabLeft(DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < 6.0 || bounds.Height < 6.0)
        {
            return;
        }

        // Rotated bounds; the top tab is rotated and flipped (for the lighting) into a left tab.
        var tempBounds = new Rect(0.0, 0.0, bounds.Height, bounds.Width);
        using (dc.PushTransform(new Matrix(0.0, 1.0, 1.0, 0.0, bounds.Left, bounds.Top)))
        {
            DrawTabGeometries(dc, tempBounds, LightLightBrush, DarkDarkBrush, LightBrush, DarkBrush);
        }

        bounds = HelperDeflateRect(bounds, new Thickness(2, 2, 0, 2));
    }

    private void DrawTabTop(DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < 6.0 || bounds.Height < 6.0)
        {
            return;
        }

        DrawTabGeometries(dc, bounds, LightLightBrush, DarkDarkBrush, LightBrush, DarkBrush);

        bounds = HelperDeflateRect(bounds, new Thickness(2, 2, 2, 0));
    }

    private void DrawTabRight(DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < 6.0 || bounds.Height < 6.0)
        {
            return;
        }

        var tempBounds = new Rect(0.0, 0.0, bounds.Height, bounds.Width);
        using (dc.PushTransform(new Matrix(0.0, -1.0, -1.0, 0.0, bounds.Right, bounds.Bottom)))
        {
            DrawTabGeometries(dc, tempBounds, DarkDarkBrush, LightLightBrush, DarkBrush, LightBrush);
        }

        bounds = HelperDeflateRect(bounds, new Thickness(0, 2, 2, 2));
    }

    private void DrawTabBottom(DrawingContext dc, ref Rect bounds)
    {
        if (bounds.Width < 6.0 || bounds.Height < 6.0)
        {
            return;
        }

        var tempBounds = new Rect(0.0, 0.0, bounds.Width, bounds.Height);
        using (dc.PushTransform(new Matrix(-1.0, 0.0, 0.0, -1.0, bounds.Right, bounds.Bottom)))
        {
            DrawTabGeometries(dc, tempBounds, DarkDarkBrush, LightLightBrush, DarkBrush, LightBrush);
        }

        bounds = HelperDeflateRect(bounds, new Thickness(2, 0, 2, 2));
    }
}
