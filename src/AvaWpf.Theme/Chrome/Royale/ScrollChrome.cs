// Ported from WPF $W/Themes/PresentationFramework.Royale/Microsoft/Windows/Themes/ScrollChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWpf.Chrome.Royale;

/// <summary>
/// The Royale scroll bar button and thumb, as WPF's Royale <c>ScrollChrome</c>. Like WPF's, it has no desired size and
/// draws no child.
/// </summary>
public sealed class ScrollChrome : Decorator
{
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
            HasOuterBorderProperty, ScrollGlyphProperty, PaddingProperty, RenderMouseOverProperty,
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
        "ScrollChrome.VerticalFill", "ScrollChrome.HorizontalFill", "ScrollChrome.HoverVerticalFill",
        "ScrollChrome.HoverHorizontalFill", "ScrollChrome.PressedVerticalFill", "ScrollChrome.PressedHorizontalFill",
        "ScrollChrome.DisabledFill", "ScrollChrome.DisabledGlyph", "ScrollChrome.ArrowGlyph", "ScrollChrome.HoverArrowGlyph",
        "ScrollChrome.GripperGlyph", "ScrollChrome.DisabledGripperGlyphShadow", "ScrollChrome.GripperGlyphShadow",
        "ScrollChrome.DisabledOuterBorder", "ScrollChrome.OuterBorder", "ScrollChrome.HoverOuterBorder",
        "ScrollChrome.InnerBorder",
    ];

    /// <summary>Whether the outer border is drawn; kept for template parity, as WPF's Royale chrome always draws it.</summary>
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

    private bool IsGripper => ScrollGlyph is ScrollGlyph.HorizontalGripper or ScrollGlyph.VerticalGripper;

    private IBrush? Fill
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return _cache.Brush("ScrollChrome.DisabledFill");
            }

            var vertical = ScrollGlyph == ScrollGlyph.VerticalGripper;
            if (RenderPressed)
            {
                return _cache.Brush(vertical ? "ScrollChrome.PressedVerticalFill" : "ScrollChrome.PressedHorizontalFill");
            }

            if (RenderMouseOver)
            {
                return _cache.Brush(vertical ? "ScrollChrome.HoverVerticalFill" : "ScrollChrome.HoverHorizontalFill");
            }

            return _cache.Brush(vertical ? "ScrollChrome.VerticalFill" : "ScrollChrome.HorizontalFill");
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

            if (IsGripper)
            {
                return _cache.Brush("ScrollChrome.GripperGlyph");
            }

            return _cache.Brush(RenderMouseOver || RenderPressed ? "ScrollChrome.HoverArrowGlyph" : "ScrollChrome.ArrowGlyph");
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

            return _cache.Brush(IsEffectivelyEnabled ? "ScrollChrome.GripperGlyphShadow" : "ScrollChrome.DisabledGripperGlyphShadow");
        }
    }

    private IPen? OuterBorder
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return _cache.Pen("ScrollChrome.DisabledOuterBorder");
            }

            return _cache.Pen(RenderMouseOver || RenderPressed ? "ScrollChrome.HoverOuterBorder" : "ScrollChrome.OuterBorder");
        }
    }

    private IPen? InnerBorder => _cache.Pen("ScrollChrome.InnerBorder");

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

    /// <summary>Draws the borders and the glyph inside <see cref="Decorator.Padding"/> when it is set.</summary>
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
