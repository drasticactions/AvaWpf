// Ported from WPF $W/Themes/Shared/Microsoft/Windows/Themes/BulletChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWpf.Chrome.Luna;

/// <summary>
/// The Luna and Royale check box and radio button bullet, as WPF's shared <c>BulletChrome</c>; <see cref="IsRound"/>
/// draws ellipses.
/// </summary>
public sealed class BulletChrome : Decorator
{
    /// <summary>Defines the <see cref="Background"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Avalonia.Controls.Border.BackgroundProperty.AddOwner<BulletChrome>();

    /// <summary>Defines the <see cref="BorderBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Avalonia.Controls.Border.BorderBrushProperty.AddOwner<BulletChrome>();

    /// <summary>Defines the <see cref="BorderThickness"/> property.</summary>
    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Avalonia.Controls.Border.BorderThicknessProperty.AddOwner<BulletChrome>();

    /// <summary>Defines the <see cref="RenderMouseOver"/> property.</summary>
    public static readonly StyledProperty<bool> RenderMouseOverProperty =
        AvaloniaProperty.Register<BulletChrome, bool>(nameof(RenderMouseOver));

    /// <summary>Defines the <see cref="RenderPressed"/> property.</summary>
    public static readonly StyledProperty<bool> RenderPressedProperty =
        AvaloniaProperty.Register<BulletChrome, bool>(nameof(RenderPressed));

    /// <summary>Defines the <see cref="IsChecked"/> property.</summary>
    public static readonly StyledProperty<bool?> IsCheckedProperty =
        AvaloniaProperty.Register<BulletChrome, bool?>(nameof(IsChecked), false);

    /// <summary>Defines the <see cref="IsRound"/> property.</summary>
    public static readonly StyledProperty<bool> IsRoundProperty =
        AvaloniaProperty.Register<BulletChrome, bool>(nameof(IsRound));

    private static readonly Geometry s_checkMarkGeometry = CreateCheckMarkGeometry();

    private readonly ChromeBrushCache _cache;

    static BulletChrome()
    {
        AffectsRender<BulletChrome>(
            BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, RenderMouseOverProperty,
            RenderPressedProperty, IsCheckedProperty, IsRoundProperty, IsEffectivelyEnabledProperty, FlowDirectionProperty);
        AffectsMeasure<BulletChrome>(BorderThicknessProperty, IsRoundProperty);
    }

    /// <summary>Creates a BulletChrome.</summary>
    public BulletChrome()
    {
        _cache = new ChromeBrushCache(this);
        ResourcesChanged += (_, _) => InvalidateCache();
        ActualThemeVariantChanged += (_, _) => InvalidateCache();
    }

    /// <summary>The token names this chrome reads, without the <c>&lt;Family&gt;.Chrome.</c> prefix.</summary>
    public static IReadOnlyList<string> TokenSet { get; } =
    [
        "BulletChrome.CheckMarkFill", "BulletChrome.CheckMarkPressedFill", "BulletChrome.RadioButtonGlyphFill",
        "BulletChrome.PressedBackground", "BulletChrome.DisabledBackground", "BulletChrome.DisabledBorder",
        "BulletChrome.CheckBoxHoverHighlight", "BulletChrome.RadioButtonHoverHighlight", "BulletChrome.IndeterminateFill",
        "BulletChrome.IndeterminatePressedFill",
    ];

    /// <summary>The fill inside the border while enabled and not pressed.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>The brush of the border while enabled.</summary>
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    /// <summary>The border thickness of the square bullet; a round bullet always has a 1 px border.</summary>
    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>When true the chrome renders with a mouse over look.</summary>
    public bool RenderMouseOver
    {
        get => GetValue(RenderMouseOverProperty);
        set => SetValue(RenderMouseOverProperty, value);
    }

    /// <summary>When true the chrome renders with a pressed look.</summary>
    public bool RenderPressed
    {
        get => GetValue(RenderPressedProperty);
        set => SetValue(RenderPressedProperty, value);
    }

    /// <summary>The check state to draw: true a check mark (or radio dot), null the indeterminate square, false nothing.</summary>
    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>When true the chrome draws a round (radio button) bullet.</summary>
    public bool IsRound
    {
        get => GetValue(IsRoundProperty);
        set => SetValue(IsRoundProperty, value);
    }

    private IBrush? Border => !IsEffectivelyEnabled ? _cache.Brush("BulletChrome.DisabledBorder") : BorderBrush;

    private IBrush? BackgroundBrush
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return _cache.Brush("BulletChrome.DisabledBackground");
            }

            return RenderPressed ? _cache.Brush("BulletChrome.PressedBackground") : Background;
        }
    }

    private IBrush? GlyphFill
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return IsChecked != false ? _cache.Brush("BulletChrome.DisabledBorder") : null;
            }

            if (!IsRound)
            {
                if (IsChecked == true)
                {
                    return _cache.Brush(RenderPressed ? "BulletChrome.CheckMarkPressedFill" : "BulletChrome.CheckMarkFill");
                }

                if (IsChecked is null)
                {
                    return _cache.Brush(RenderPressed ? "BulletChrome.CheckMarkPressedFill" : "BulletChrome.IndeterminateFill");
                }

                return null;
            }

            return IsChecked == true ? _cache.Brush("BulletChrome.RadioButtonGlyphFill") : null;
        }
    }

    private IPen? BorderPen =>
        !IsEffectivelyEnabled ? _cache.Pen("BulletChrome.DisabledBorder", 1.0) : _cache.UserPen(BorderBrush, 1.0);

    private IPen? HighlightPen
    {
        get
        {
            if (!RenderMouseOver || RenderPressed || !IsEffectivelyEnabled)
            {
                return null;
            }

            return _cache.Pen(IsRound ? "BulletChrome.RadioButtonHoverHighlight" : "BulletChrome.CheckBoxHoverHighlight", 2.0);
        }
    }

    /// <summary>Returns 11 × 11 plus the border (2 for a round bullet), clamped to the available size.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var thickness = BorderThickness;
        var isRound = IsRound;
        var borderX = isRound ? 2.0 : thickness.Left + thickness.Right;
        var borderY = isRound ? 2.0 : thickness.Top + thickness.Bottom;
        return new Size(Math.Min(11.0 + borderX, availableSize.Width), Math.Min(11.0 + borderY, availableSize.Height));
    }

    /// <summary>Takes the final size.</summary>
    protected override Size ArrangeOverride(Size finalSize) => finalSize;

    /// <summary>Draws the background, the hover highlight, the glyph and the border.</summary>
    public override void Render(DrawingContext context)
    {
        var thickness = BorderThickness;
        var isUnitThickness = thickness.Left == 1.0 && thickness.Right == 1.0 && thickness.Top == 1.0 && thickness.Bottom == 1.0;
        var bounds = new Rect(Bounds.Size);
        var innerBounds = bounds;

        if (!IsRound)
        {
            innerBounds = new Rect(
                thickness.Left,
                thickness.Top,
                Math.Max(0, bounds.Width - thickness.Left - thickness.Right),
                Math.Max(0, bounds.Height - thickness.Top - thickness.Bottom));
        }

        DrawBackground(context, innerBounds);
        DrawHighlight(context, innerBounds);
        DrawGlyph(context, innerBounds, isUnitThickness);
        DrawBorder(context, bounds, thickness, isUnitThickness);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InvalidateCache();
    }

    private static Geometry CreateCheckMarkGeometry()
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(3, 5.0), true);
        ctx.LineTo(new Point(3, 7.8));
        ctx.LineTo(new Point(5.5, 10.4));
        ctx.LineTo(new Point(10.1, 5.8));
        ctx.LineTo(new Point(10.1, 3));
        ctx.LineTo(new Point(5.5, 7.6));
        ctx.EndFigure(true);
        return geometry;
    }

    private static Rect HelperDeflateRect(Rect rt, Thickness thick) => new(
        rt.Left + thick.Left,
        rt.Top + thick.Top,
        Math.Max(0.0, rt.Width - thick.Left - thick.Right),
        Math.Max(0.0, rt.Height - thick.Top - thick.Bottom));

    private static PathFigure GenerateRectFigure(Rect rect) => new()
    {
        StartPoint = rect.TopLeft,
        IsClosed = true,
        Segments = new PathSegments
        {
            new LineSegment { Point = rect.TopRight },
            new LineSegment { Point = rect.BottomRight },
            new LineSegment { Point = rect.BottomLeft },
        },
    };

    private static Geometry GenerateBorderGeometry(Rect rect, Thickness borderThickness)
    {
        var geometry = new PathGeometry { FillRule = FillRule.EvenOdd };
        geometry.Figures!.Add(GenerateRectFigure(rect));
        geometry.Figures.Add(GenerateRectFigure(HelperDeflateRect(rect, borderThickness)));
        return geometry;
    }

    private void DrawBackground(DrawingContext dc, Rect bounds)
    {
        var fill = BackgroundBrush;
        if (fill is not null && (bounds.Width > 2.0) && (bounds.Height > 2.0))
        {
            if (!IsRound)
            {
                dc.DrawRectangle(fill, null, bounds);
            }
            else
            {
                var centerX = bounds.Width * 0.5;
                var centerY = bounds.Height * 0.5;
                dc.DrawEllipse(fill, null, new Point(centerX, centerY), centerX - 1, centerY - 1);
            }
        }
    }

    private void DrawHighlight(DrawingContext dc, Rect bounds)
    {
        var highlightPen = HighlightPen;
        if (highlightPen is not null && (bounds.Width >= 4.0) && (bounds.Height >= 4.0))
        {
            if (!IsRound)
            {
                dc.DrawRectangle(null, highlightPen, new Rect(bounds.Left + 1.0, bounds.Top + 1.0, bounds.Width - 2.0, bounds.Height - 2.0));
            }
            else
            {
                var centerX = bounds.Width * 0.5;
                var centerY = bounds.Height * 0.5;
                dc.DrawEllipse(null, highlightPen, new Point(centerX, centerY), centerX - 2, centerY - 2);
            }
        }
    }

    private void DrawGlyph(DrawingContext dc, Rect bounds, bool isUnitThickness)
    {
        var glyphFill = GlyphFill;
        if (glyphFill is null || (bounds.Width <= 4.0) || (bounds.Height <= 4.0))
        {
            return;
        }

        if (!IsRound)
        {
            if (IsChecked == true)
            {
                using (isUnitThickness ? default(DrawingContext.PushedState?) : dc.PushTransform(Matrix.CreateTranslation(bounds.Left - 1.0, bounds.Top - 1.0)))
                using (FlowDirection == FlowDirection.RightToLeft
                    ? dc.PushTransform(Matrix.CreateTranslation(-6.5, 0) * Matrix.CreateScale(-1.0, 1.0) * Matrix.CreateTranslation(6.5, 0))
                    : default(DrawingContext.PushedState?))
                {
                    dc.DrawGeometry(glyphFill, null, s_checkMarkGeometry);
                }
            }
            else if (IsChecked is null)
            {
                dc.DrawRectangle(glyphFill, null, new Rect(bounds.Left + 2, bounds.Top + 2, bounds.Width - 4.0, bounds.Height - 4.0));
            }
        }
        else if ((bounds.Width > 8.0) && (bounds.Height > 8.0))
        {
            var centerX = bounds.Width * 0.5;
            var centerY = bounds.Height * 0.5;
            dc.DrawEllipse(glyphFill, null, new Point(centerX, centerY), centerX - 4, centerY - 4);
        }
    }

    private void DrawBorder(DrawingContext dc, Rect bounds, Thickness thickness, bool isUnitThickness)
    {
        if ((bounds.Width < 5.0) || (bounds.Height < 5.0))
        {
            return;
        }

        if (!IsRound)
        {
            if (isUnitThickness)
            {
                var borderPen = BorderPen;
                if (borderPen is not null)
                {
                    dc.DrawRectangle(null, borderPen, new Rect(bounds.Left + 0.5, bounds.Top + 0.5, bounds.Width - 1.0, bounds.Height - 1.0));
                }
            }
            else
            {
                var borderBrush = Border;
                if (borderBrush is not null)
                {
                    dc.DrawGeometry(borderBrush, null, GenerateBorderGeometry(bounds, thickness));
                }
            }
        }
        else
        {
            var borderPen = BorderPen;
            if (borderPen is not null)
            {
                var centerX = bounds.Width * 0.5;
                var centerY = bounds.Height * 0.5;
                dc.DrawEllipse(null, borderPen, new Point(centerX, centerY), centerX - 0.5, centerY - 0.5);
            }
        }
    }

    private void InvalidateCache()
    {
        _cache.Clear();
        InvalidateVisual();
    }
}
