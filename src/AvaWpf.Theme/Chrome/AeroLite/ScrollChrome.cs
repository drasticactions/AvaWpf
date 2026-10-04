// Ported from WPF $W/Themes/PresentationFramework.AeroLite/Microsoft/Windows/Themes/ScrollChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWpf.Chrome.AeroLite;

/// <summary>The AeroLite scroll bar thumb chrome (WPF <c>Microsoft.Windows.Themes.ScrollChrome</c>), without animations.</summary>
/// <remarks>
/// Draws only the <see cref="ScrollGlyph.VerticalGripper"/> and <see cref="ScrollGlyph.HorizontalGripper"/> glyphs.
/// It has no child and derives from <see cref="Control"/>.
/// </remarks>
public sealed class ScrollChrome : Control
{
    /// <summary>Defines the attached <c>ScrollGlyph</c> property, the glyph a ScrollChrome draws.</summary>
    public static readonly AttachedProperty<ScrollGlyph> ScrollGlyphProperty =
        AvaloniaProperty.RegisterAttached<ScrollChrome, Control, ScrollGlyph>("ScrollGlyph", ScrollGlyph.None);

    /// <summary>Defines the <see cref="RenderMouseOver"/> property.</summary>
    public static readonly StyledProperty<bool> RenderMouseOverProperty =
        AvaloniaProperty.Register<ScrollChrome, bool>(nameof(RenderMouseOver));

    /// <summary>Defines the <see cref="RenderPressed"/> property.</summary>
    public static readonly StyledProperty<bool> RenderPressedProperty =
        AvaloniaProperty.Register<ScrollChrome, bool>(nameof(RenderPressed));

    private const string ThumbFill = "ScrollChrome.Thumb.Fill";
    private const string ThumbHoverFill = "ScrollChrome.Thumb.Hover.Fill";
    private const string ThumbPressedFill = "ScrollChrome.Thumb.Pressed.Fill";
    private const string ThumbBorder = "ScrollChrome.Thumb.Border";
    private const string ThumbHoverBorder = "ScrollChrome.Thumb.Hover.Border";
    private const string ThumbPressedBorder = "ScrollChrome.Thumb.Pressed.Border";
    private const string ThumbGlyph = "ScrollChrome.Thumb.Glyph";
    private const string GrayTextBrush = "SystemColors.GrayTextBrush";

    private readonly ChromeTokens _tokens;
    private ScrollGlyph _scrollGlyph;

    static ScrollChrome()
    {
        AffectsRender<ScrollChrome>(ScrollGlyphProperty, RenderMouseOverProperty, RenderPressedProperty, IsEffectivelyEnabledProperty);
    }

    /// <summary>Initializes a new instance of the <see cref="ScrollChrome"/> class.</summary>
    public ScrollChrome()
    {
        _tokens = new ChromeTokens(this);
        ResourcesChanged += (_, _) => OnThemeResourcesChanged();
        ActualThemeVariantChanged += (_, _) => OnThemeResourcesChanged();
    }

    /// <summary>
    /// The chrome token names this class reads, without the <c>&lt;Family&gt;.Chrome.</c> prefix. The disabled gripper
    /// also uses the <c>SystemColors.GrayTextBrush</c> resource.
    /// </summary>
    public static IReadOnlyList<string> TokenSet { get; } =
    [
        ThumbFill, ThumbHoverFill, ThumbPressedFill, ThumbBorder, ThumbHoverBorder, ThumbPressedBorder, ThumbGlyph,
    ];

    /// <summary>When true, the chrome draws the mouse-over look.</summary>
    public bool RenderMouseOver
    {
        get => GetValue(RenderMouseOverProperty);
        set => SetValue(RenderMouseOverProperty, value);
    }

    /// <summary>When true, the chrome draws the pressed look.</summary>
    public bool RenderPressed
    {
        get => GetValue(RenderPressedProperty);
        set => SetValue(RenderPressedProperty, value);
    }

    private bool IsGripper => _scrollGlyph is ScrollGlyph.HorizontalGripper or ScrollGlyph.VerticalGripper;

    /// <summary>Gets the glyph that a ScrollChrome draws.</summary>
    /// <param name="element">The element the value is read from.</param>
    /// <returns>The glyph.</returns>
    public static ScrollGlyph GetScrollGlyph(Control element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(ScrollGlyphProperty);
    }

    /// <summary>Sets the glyph that a ScrollChrome draws.</summary>
    /// <param name="element">The element the value is set on.</param>
    /// <param name="value">The glyph.</param>
    public static void SetScrollGlyph(Control element, ScrollGlyph value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ScrollGlyphProperty, value);
    }

    /// <summary>Returns a zero size: the chrome fills whatever it is given.</summary>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>Takes the final size.</summary>
    protected override Size ArrangeOverride(Size finalSize) => finalSize;

    /// <summary>Draws the border with the fill, and the gripper.</summary>
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var snap = DeviceSnapper.For(this);
        _scrollGlyph = GetScrollGlyph(this);

        // The rectangle is the center line of the 1 px border.
        if ((bounds.Width >= 1.0) && (bounds.Height >= 1.0))
        {
            bounds = new Rect(bounds.X + 0.5, bounds.Y + 0.5, bounds.Width - 1.0, bounds.Height - 1.0);
        }

        switch (_scrollGlyph)
        {
            case ScrollGlyph.HorizontalGripper:
                if (bounds.Height >= 1.0)
                {
                    bounds = new Rect(bounds.X, bounds.Y + 1.0, bounds.Width, bounds.Height - 1.0);
                }

                break;
            case ScrollGlyph.VerticalGripper:
                if (bounds.Width >= 1.0)
                {
                    bounds = new Rect(bounds.X + 1.0, bounds.Y, bounds.Width - 1.0, bounds.Height);
                }

                break;
        }

        DrawBorders(context, bounds, snap);
        DrawGlyph(context, bounds, snap);
    }

    private void DrawBorders(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width >= 2.0) && (bounds.Height >= 2.0))
        {
            if (Border(snap.Line) is { } pen)
            {
                var rect = snap.Stroke(bounds.X - 0.5, bounds.Y - 0.5, bounds.Width + 1.0, bounds.Height + 1.0);
                dc.DrawRectangle(Fill, pen, rect);
            }
        }
    }

    private void DrawGlyph(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width > 0.0) && (bounds.Height > 0.0))
        {
            if (Glyph is { } brush)
            {
                switch (_scrollGlyph)
                {
                    case ScrollGlyph.HorizontalGripper:
                        DrawHorizontalGripper(dc, brush, bounds, snap);
                        break;
                    case ScrollGlyph.VerticalGripper:
                        DrawVerticalGripper(dc, brush, bounds, snap);
                        break;
                }
            }
        }
    }

    private static void DrawHorizontalGripper(DrawingContext dc, IBrush brush, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width > 15.0) && (bounds.Height > 2.0))
        {
            var height = Math.Min(7.0, bounds.Height);
            var x = bounds.X + ((bounds.Width * 0.5) - 4.0);
            var y = bounds.Y + ((bounds.Height - height) * 0.5);

            for (var i = 0; i < 9; i += 3)
            {
                dc.DrawRectangle(brush, null, snap.Fill(x + i, y, 2.0, height));
            }
        }
    }

    private static void DrawVerticalGripper(DrawingContext dc, IBrush brush, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width > 2.0) && (bounds.Height > 15.0))
        {
            var width = Math.Min(7.0, bounds.Width);
            var x = bounds.X + ((bounds.Width - width) * 0.5);
            var y = bounds.Y + ((bounds.Height * 0.5) - 4.0);

            for (var i = 0; i < 9; i += 3)
            {
                dc.DrawRectangle(brush, null, snap.Fill(x, y + i, width, 2.0));
            }
        }
    }

    private IBrush? Fill
    {
        get
        {
            if (!IsGripper)
            {
                return null;
            }

            if (RenderPressed)
            {
                return _tokens.Brush(ThumbPressedFill);
            }

            return RenderMouseOver ? _tokens.Brush(ThumbHoverFill) : _tokens.Brush(ThumbFill);
        }
    }

    private IPen? Border(double thickness)
    {
        if (!IsGripper)
        {
            return null;
        }

        if (RenderPressed)
        {
            return _tokens.Pen(ThumbPressedBorder, thickness);
        }

        return RenderMouseOver ? _tokens.Pen(ThumbHoverBorder, thickness) : _tokens.Pen(ThumbBorder, thickness);
    }

    private IBrush? Glyph
    {
        get
        {
            if (!IsGripper)
            {
                return null;
            }

            return IsEffectivelyEnabled ? _tokens.Brush(ThumbGlyph) : _tokens.Resource(GrayTextBrush);
        }
    }

    private void OnThemeResourcesChanged()
    {
        _tokens.Clear();
        InvalidateVisual();
    }
}
