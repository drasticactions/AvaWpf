// Ported from WPF $W/Themes/PresentationFramework.Luna/Microsoft/Windows/Themes/ScrollChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWpf.Chrome.Luna;

/// <summary>
/// The Luna scroll bar button and thumb, as WPF's Luna <c>ScrollChrome</c>. Like WPF's, it has no desired size and draws
/// no child.
/// </summary>
public sealed class ScrollChrome : Decorator
{
    /// <summary>Defines the <see cref="ThemeColor"/> property.</summary>
    public static readonly StyledProperty<ThemeColor> ThemeColorProperty =
        ButtonChrome.ThemeColorProperty.AddOwner<ScrollChrome>();

    /// <summary>Defines the <see cref="HasOuterBorder"/> property.</summary>
    public static readonly StyledProperty<bool> HasOuterBorderProperty =
        AvaloniaProperty.Register<ScrollChrome, bool>(nameof(HasOuterBorder), true);

    /// <summary>Defines the ScrollGlyph attached property: the glyph a ScrollChrome draws.</summary>
    public static readonly AttachedProperty<ScrollGlyph> ScrollGlyphProperty =
        AvaloniaProperty.RegisterAttached<ScrollChrome, Control, ScrollGlyph>("ScrollGlyph", ScrollGlyph.None);

    /// <summary>Defines the <see cref="RenderMouseOver"/> property.</summary>
    public static readonly StyledProperty<bool> RenderMouseOverProperty =
        ButtonChrome.RenderMouseOverProperty.AddOwner<ScrollChrome>();

    /// <summary>Defines the <see cref="RenderPressed"/> property.</summary>
    public static readonly StyledProperty<bool> RenderPressedProperty =
        ButtonChrome.RenderPressedProperty.AddOwner<ScrollChrome>();

    private static readonly Geometry s_leftArrowGeometry = Arrow(new(4.5, 0.0), new(0.0, 4.5), new(4.5, 9.0), new(6.0, 7.5), new(3.0, 4.5), new(6.0, 1.5));
    private static readonly Geometry s_rightArrowGeometry = Arrow(new(3.5, 0.0), new(8.0, 4.5), new(3.5, 9.0), new(2.0, 7.5), new(5.0, 4.5), new(2.0, 1.5));
    private static readonly Geometry s_upArrowGeometry = Arrow(new(0.0, 4.5), new(4.5, 0.0), new(9.0, 4.5), new(7.5, 6.0), new(4.5, 3.0), new(1.5, 6.0));
    private static readonly Geometry s_downArrowGeometry = Arrow(new(0.0, 3.5), new(4.5, 8.0), new(9.0, 3.5), new(7.5, 2.0), new(4.5, 5.0), new(1.5, 2.0));

    private readonly ChromeBrushCache _cache;

    static ScrollChrome()
    {
        AffectsRender<ScrollChrome>(
            ThemeColorProperty, HasOuterBorderProperty, ScrollGlyphProperty, PaddingProperty, RenderMouseOverProperty,
            RenderPressedProperty, IsEffectivelyEnabledProperty);
    }

    /// <summary>Creates a ScrollChrome.</summary>
    public ScrollChrome()
    {
        _cache = new ChromeBrushCache(this);
        ResourcesChanged += (_, _) => InvalidateCache();
        ActualThemeVariantChanged += (_, _) => InvalidateCache();
    }

    /// <summary>The token names this chrome reads, without the <c>&lt;Family&gt;.Chrome.</c> prefix.</summary>
    public static IReadOnlyList<string> TokenSet { get; } =
    [
        "ScrollChrome.DisabledFill", "ScrollChrome.DisabledGlyph", "ScrollChrome.DisabledGripperGlyphShadow",
        "ScrollChrome.DisabledInnerBorder", "ScrollChrome.DisabledShadow",
        "ScrollChrome.NormalColor.LineButtonFill", "ScrollChrome.NormalColor.HoverLineButtonFill",
        "ScrollChrome.NormalColor.PressedLineButtonFill",
        "ScrollChrome.NormalColor.VerticalFill", "ScrollChrome.NormalColor.HorizontalFill",
        "ScrollChrome.NormalColor.HoverVerticalFill", "ScrollChrome.NormalColor.HoverHorizontalFill",
        "ScrollChrome.NormalColor.PressedVerticalFill", "ScrollChrome.NormalColor.PressedHorizontalFill",
        "ScrollChrome.NormalColor.ArrowGlyph", "ScrollChrome.NormalColor.GripperGlyph",
        "ScrollChrome.NormalColor.HoverGripperGlyph", "ScrollChrome.NormalColor.PressedGripperGlyph",
        "ScrollChrome.NormalColor.GripperGlyphShadow", "ScrollChrome.NormalColor.HoverGripperGlyphShadow",
        "ScrollChrome.NormalColor.PressedGripperGlyphShadow", "ScrollChrome.NormalColor.OuterBorder",
        "ScrollChrome.NormalColor.InnerBorder", "ScrollChrome.NormalColor.HoverInnerBorder",
        "ScrollChrome.NormalColor.HoverThumbInnerBorder", "ScrollChrome.NormalColor.PressedInnerBorder",
        "ScrollChrome.NormalColor.Shadow",
        "ScrollChrome.Homestead.VerticalFill", "ScrollChrome.Homestead.HorizontalFill",
        "ScrollChrome.Homestead.HoverVerticalFill", "ScrollChrome.Homestead.HoverHorizontalFill",
        "ScrollChrome.Homestead.PressedVerticalFill", "ScrollChrome.Homestead.PressedHorizontalFill",
        "ScrollChrome.Homestead.ArrowGlyph", "ScrollChrome.Homestead.GripperGlyph",
        "ScrollChrome.Homestead.HoverGripperGlyph", "ScrollChrome.Homestead.PressedGripperGlyph",
        "ScrollChrome.Homestead.GripperGlyphShadow", "ScrollChrome.Homestead.HoverGripperGlyphShadow",
        "ScrollChrome.Homestead.PressedGripperGlyphShadow", "ScrollChrome.Homestead.OuterBorder",
        "ScrollChrome.Homestead.InnerBorder", "ScrollChrome.Homestead.HoverInnerBorder",
        "ScrollChrome.Homestead.PressedInnerBorder", "ScrollChrome.Homestead.Shadow",
        "ScrollChrome.Metallic.VerticalFill", "ScrollChrome.Metallic.HorizontalFill",
        "ScrollChrome.Metallic.HoverVerticalFill", "ScrollChrome.Metallic.HoverHorizontalFill",
        "ScrollChrome.Metallic.PressedVerticalFill", "ScrollChrome.Metallic.PressedHorizontalFill",
        "ScrollChrome.Metallic.ArrowGlyph", "ScrollChrome.Metallic.HoverArrowGlyph", "ScrollChrome.Metallic.GripperGlyph",
        "ScrollChrome.Metallic.GripperGlyphShadow", "ScrollChrome.Metallic.OuterBorder",
        "ScrollChrome.Metallic.HoverOuterBorder", "ScrollChrome.Metallic.PressedOuterBorder",
        "ScrollChrome.Metallic.InnerBorder", "ScrollChrome.Metallic.HoverInnerBorder",
        "ScrollChrome.Metallic.PressedInnerBorder",
    ];

    /// <summary>The Luna color scheme to draw. Templates bind it to the <c>Luna.ThemeColor</c> resource.</summary>
    public ThemeColor ThemeColor
    {
        get => GetValue(ThemeColorProperty);
        set => SetValue(ThemeColorProperty, value);
    }

    /// <summary>Whether the white outer border and the shadow are drawn (Normal Color and Homestead).</summary>
    public bool HasOuterBorder
    {
        get => GetValue(HasOuterBorderProperty);
        set => SetValue(HasOuterBorderProperty, value);
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

    private ScrollGlyph ScrollGlyph => GetValue(ScrollGlyphProperty);

    private string Scheme => LunaScheme.Name(ThemeColor);

    private bool IsGripper => ScrollGlyph is ScrollGlyph.HorizontalGripper or ScrollGlyph.VerticalGripper;

    private IBrush? Fill
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return _cache.Brush("ScrollChrome.DisabledFill");
            }

            var glyph = ScrollGlyph;
            var scheme = Scheme;
            var state = RenderPressed ? "Pressed" : RenderMouseOver ? "Hover" : string.Empty;
            if (glyph == ScrollGlyph.VerticalGripper)
            {
                return _cache.Brush("ScrollChrome." + scheme + "." + state + "VerticalFill");
            }

            if (glyph == ScrollGlyph.HorizontalGripper)
            {
                return _cache.Brush("ScrollChrome." + scheme + "." + state + "HorizontalFill");
            }

            // Line buttons: Normal Color has its own diagonal fill; the others reuse the horizontal thumb fill.
            return ThemeColor == ThemeColor.NormalColor
                ? _cache.Brush("ScrollChrome.NormalColor." + state + "LineButtonFill")
                : _cache.Brush("ScrollChrome." + scheme + "." + state + "HorizontalFill");
        }
    }

    private IBrush? Glyph
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return _cache.Brush("ScrollChrome.DisabledGlyph");
            }

            var themeColor = ThemeColor;
            if (IsGripper)
            {
                if (themeColor == ThemeColor.Metallic)
                {
                    return _cache.Brush("ScrollChrome.Metallic.GripperGlyph");
                }

                var state = RenderPressed ? "Pressed" : RenderMouseOver ? "Hover" : string.Empty;
                return _cache.Brush("ScrollChrome." + Scheme + "." + state + "GripperGlyph");
            }

            if (themeColor == ThemeColor.Metallic)
            {
                return _cache.Brush(RenderMouseOver || RenderPressed ? "ScrollChrome.Metallic.HoverArrowGlyph" : "ScrollChrome.Metallic.ArrowGlyph");
            }

            return _cache.Brush("ScrollChrome." + Scheme + ".ArrowGlyph");
        }
    }

    private IBrush? GlyphShadow
    {
        get
        {
            if (!IsGripper)
            {
                return null;
            }

            if (!IsEffectivelyEnabled)
            {
                return _cache.Brush("ScrollChrome.DisabledGripperGlyphShadow");
            }

            if (ThemeColor == ThemeColor.Metallic)
            {
                return _cache.Brush("ScrollChrome.Metallic.GripperGlyphShadow");
            }

            var state = RenderPressed ? "Pressed" : RenderMouseOver ? "Hover" : string.Empty;
            return _cache.Brush("ScrollChrome." + Scheme + "." + state + "GripperGlyphShadow");
        }
    }

    private IPen? OuterBorder
    {
        get
        {
            var themeColor = ThemeColor;
            if (themeColor == ThemeColor.Metallic)
            {
                if (!IsEffectivelyEnabled)
                {
                    return _cache.Pen("ScrollChrome.NormalColor.OuterBorder");
                }

                if (RenderPressed && IsGripper)
                {
                    return _cache.Pen("ScrollChrome.Metallic.PressedOuterBorder");
                }

                if (RenderPressed || RenderMouseOver)
                {
                    return _cache.Pen("ScrollChrome.Metallic.HoverOuterBorder");
                }

                return _cache.Pen("ScrollChrome.Metallic.OuterBorder");
            }

            if (HasOuterBorder)
            {
                // Normal Color or Homestead.
                return _cache.Pen("ScrollChrome." + Scheme + ".OuterBorder");
            }

            return null;
        }
    }

    private IPen? InnerBorder
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return _cache.Pen("ScrollChrome.DisabledInnerBorder");
            }

            var themeColor = ThemeColor;
            if (RenderPressed)
            {
                return _cache.Pen("ScrollChrome." + Scheme + ".PressedInnerBorder");
            }

            if (RenderMouseOver)
            {
                if (themeColor == ThemeColor.NormalColor && IsGripper)
                {
                    return _cache.Pen("ScrollChrome.NormalColor.HoverThumbInnerBorder");
                }

                return _cache.Pen("ScrollChrome." + Scheme + ".HoverInnerBorder");
            }

            return _cache.Pen("ScrollChrome." + Scheme + ".InnerBorder");
        }
    }

    private IPen? Shadow
    {
        get
        {
            if (!HasOuterBorder)
            {
                return null;
            }

            if (!IsEffectivelyEnabled)
            {
                return _cache.Pen("ScrollChrome.DisabledShadow");
            }

            return ThemeColor == ThemeColor.Metallic ? null : _cache.Pen("ScrollChrome." + Scheme + ".Shadow");
        }
    }

    /// <summary>Gets the glyph a ScrollChrome on <paramref name="element"/> draws.</summary>
    public static ScrollGlyph GetScrollGlyph(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(ScrollGlyphProperty);
    }

    /// <summary>Sets the glyph a ScrollChrome on <paramref name="element"/> draws.</summary>
    public static void SetScrollGlyph(Control element, ScrollGlyph value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ScrollGlyphProperty, value);
    }

    /// <summary>Returns no desired size, as WPF's ScrollChrome.</summary>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>Takes the final size.</summary>
    protected override Size ArrangeOverride(Size finalSize) => finalSize;

    /// <summary>Draws the shadow, the borders and the glyph inside <see cref="Decorator.Padding"/> when it is set.</summary>
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var glyph = ScrollGlyph;

        if (IsSet(PaddingProperty))
        {
            bounds = ApplyPadding(bounds, Padding);
        }

        if ((bounds.Width >= 1.0) && (bounds.Height >= 1.0))
        {
            bounds = new Rect(bounds.X + 0.5, bounds.Y + 0.5, bounds.Width - 1.0, bounds.Height - 1.0);
        }

        switch (glyph)
        {
            case ScrollGlyph.LeftArrow:
            case ScrollGlyph.RightArrow:
            case ScrollGlyph.HorizontalGripper:
                if (bounds.Height >= 1.0)
                {
                    bounds = new Rect(bounds.X, bounds.Y + 1.0, bounds.Width, bounds.Height - 1.0);
                }

                break;
            case ScrollGlyph.UpArrow:
            case ScrollGlyph.DownArrow:
            case ScrollGlyph.VerticalGripper:
                if (bounds.Width >= 1.0)
                {
                    bounds = new Rect(bounds.X + 1.0, bounds.Y, bounds.Width - 1.0, bounds.Height);
                }

                break;
        }

        DrawShadow(context, ref bounds);
        DrawBorders(context, ref bounds);
        DrawGlyph(context, glyph, ref bounds);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InvalidateCache();
    }

    private static Rect ApplyPadding(Rect bounds, Thickness padding)
    {
        double x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height;
        var totalPadding = padding.Left + padding.Right;
        if (totalPadding >= width)
        {
            width = 0.0;
        }
        else
        {
            x += padding.Left;
            width -= totalPadding;
        }

        totalPadding = padding.Top + padding.Bottom;
        if (totalPadding >= height)
        {
            height = 0.0;
        }
        else
        {
            y += padding.Top;
            height -= totalPadding;
        }

        return new Rect(x, y, width, height);
    }

    private static Geometry Arrow(Point start, params Point[] points)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(start, true);
        foreach (var p in points)
        {
            ctx.LineTo(p);
        }

        ctx.EndFigure(true);
        return geometry;
    }

    private void DrawShadow(DrawingContext dc, ref Rect bounds)
    {
        if ((bounds.Width > 0.0) && (bounds.Height > 2.0))
        {
            var pen = Shadow;
            if (pen is not null)
            {
                dc.DrawRectangle(null, pen, new Rect(bounds.X, bounds.Y + 2.0, bounds.Width, bounds.Height - 2.0), 3.0, 3.0);
                bounds = new Rect(bounds.X, bounds.Y, Math.Max(0.0, bounds.Width - 1.0), bounds.Height - 1.0);
            }
        }
    }

    private void DrawBorders(DrawingContext dc, ref Rect bounds)
    {
        if ((bounds.Width >= 2.0) && (bounds.Height >= 2.0))
        {
            var brush = Fill;
            var pen = OuterBorder;
            if (pen is not null)
            {
                dc.DrawRectangle(brush, pen, bounds, 2.0, 2.0);
                brush = null; // The fill is drawn with the outer border.
                bounds = bounds.Deflate(1.0);
            }

            if ((bounds.Width >= 2.0) && (bounds.Height >= 2.0))
            {
                pen = InnerBorder;
                if ((pen is not null) || (brush is not null))
                {
                    dc.DrawRectangle(brush, pen, bounds, 1.5, 1.5);
                    bounds = bounds.Deflate(1.0);
                }
            }
        }
    }

    private void DrawGlyph(DrawingContext dc, ScrollGlyph glyph, ref Rect bounds)
    {
        if ((bounds.Width > 0.0) && (bounds.Height > 0.0))
        {
            var brush = Glyph;
            if ((brush is not null) && (glyph != ScrollGlyph.None))
            {
                switch (glyph)
                {
                    case ScrollGlyph.HorizontalGripper:
                        DrawHorizontalGripper(dc, brush, bounds);
                        break;
                    case ScrollGlyph.VerticalGripper:
                        DrawVerticalGripper(dc, brush, bounds);
                        break;
                    case ScrollGlyph.LeftArrow:
                    case ScrollGlyph.RightArrow:
                    case ScrollGlyph.UpArrow:
                    case ScrollGlyph.DownArrow:
                        DrawArrow(dc, brush, bounds, glyph);
                        break;
                }
            }
        }
    }

    private void DrawHorizontalGripper(DrawingContext dc, IBrush brush, Rect bounds)
    {
        if ((bounds.Width > 8.0) && (bounds.Height > 2.0))
        {
            var glyphShadow = GlyphShadow;
            var height = Math.Min(6.0, bounds.Height);
            var x = bounds.X + ((bounds.Width * 0.5) - 4.0);
            var y = bounds.Y + ((bounds.Height - height) * 0.5);
            height -= 1.0;

            for (var i = 0; i < 8; i += 2)
            {
                dc.DrawRectangle(brush, null, new Rect(x + i, y, 1.0, height));
                if (glyphShadow is not null)
                {
                    dc.DrawRectangle(glyphShadow, null, new Rect(x + i + 1, y + 1, 1.0, height));
                }
            }
        }
    }

    private void DrawVerticalGripper(DrawingContext dc, IBrush brush, Rect bounds)
    {
        if ((bounds.Width > 2.0) && (bounds.Height > 8.0))
        {
            var glyphShadow = GlyphShadow;
            var width = Math.Min(6.0, bounds.Width);
            var x = bounds.X + ((bounds.Width - width) * 0.5);
            var y = bounds.Y + ((bounds.Height * 0.5) - 4.0);
            width -= 1.0;

            for (var i = 0; i < 8; i += 2)
            {
                dc.DrawRectangle(brush, null, new Rect(x, y + i, width, 1.0));
                if (glyphShadow is not null)
                {
                    dc.DrawRectangle(glyphShadow, null, new Rect(x + 1, y + i + 1, width, 1.0));
                }
            }
        }
    }

    private static void DrawArrow(DrawingContext dc, IBrush brush, Rect bounds, ScrollGlyph glyph)
    {
        var glyphWidth = 9.0;
        var glyphHeight = 9.0;
        switch (glyph)
        {
            case ScrollGlyph.LeftArrow:
            case ScrollGlyph.RightArrow:
                glyphWidth = 8.0;
                break;
            case ScrollGlyph.UpArrow:
            case ScrollGlyph.DownArrow:
                glyphHeight = 8.0;
                break;
        }

        Matrix matrix;
        if ((bounds.Width < glyphWidth) || (bounds.Height < glyphHeight))
        {
            var widthScale = Math.Min(glyphWidth, bounds.Width) / glyphWidth;
            var heightScale = Math.Min(glyphHeight, bounds.Height) / glyphHeight;
            var x = (bounds.X + (bounds.Width * 0.5)) / widthScale - (glyphWidth * 0.5);
            var y = (bounds.Y + (bounds.Height * 0.5)) / heightScale - (glyphHeight * 0.5);
            if (!double.IsFinite(widthScale) || !double.IsFinite(heightScale) || !double.IsFinite(x) || !double.IsFinite(y))
            {
                return;
            }

            matrix = Matrix.CreateTranslation(x, y) * Matrix.CreateScale(widthScale, heightScale);
        }
        else
        {
            var x = bounds.X + (bounds.Width * 0.5) - (glyphWidth * 0.5);
            var y = bounds.Y + (bounds.Height * 0.5) - (glyphHeight * 0.5);
            matrix = Matrix.CreateTranslation(x, y);
        }

        using (dc.PushTransform(matrix))
        {
            var geometry = glyph switch
            {
                ScrollGlyph.LeftArrow => s_leftArrowGeometry,
                ScrollGlyph.RightArrow => s_rightArrowGeometry,
                ScrollGlyph.UpArrow => s_upArrowGeometry,
                _ => s_downArrowGeometry,
            };
            dc.DrawGeometry(brush, null, geometry);
        }
    }

    private void InvalidateCache()
    {
        _cache.Clear();
        InvalidateVisual();
    }
}
