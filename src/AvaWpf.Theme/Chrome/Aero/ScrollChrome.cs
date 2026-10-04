// Ported from WPF $W/Themes/PresentationFramework.Aero/Microsoft/Windows/Themes/ScrollChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaWpf.Animations;

namespace AvaWpf.Chrome.Aero;

/// <summary>The Aero scroll bar thumb and arrow button chrome (WPF <c>Microsoft.Windows.Themes.ScrollChrome</c>).</summary>
/// <remarks>Draws everything itself and has no child; it derives from <see cref="Control"/>.</remarks>
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

    private const string HorizontalThumbFill = "ScrollChrome.HorizontalThumb.Fill";
    private const string HorizontalThumbHoverFill = "ScrollChrome.HorizontalThumb.Hover.Fill";
    private const string HorizontalThumbPressedFill = "ScrollChrome.HorizontalThumb.Pressed.Fill";
    private const string VerticalThumbFill = "ScrollChrome.VerticalThumb.Fill";
    private const string VerticalThumbHoverFill = "ScrollChrome.VerticalThumb.Hover.Fill";
    private const string VerticalThumbPressedFill = "ScrollChrome.VerticalThumb.Pressed.Fill";
    private const string ThumbBorder = "ScrollChrome.Thumb.Border";
    private const string ThumbHoverBorder = "ScrollChrome.Thumb.Hover.Border";
    private const string ThumbPressedBorder = "ScrollChrome.Thumb.Pressed.Border";
    private const string ThumbInnerBorder = "ScrollChrome.Thumb.InnerBorder";
    private const string ThumbShadow = "ScrollChrome.Thumb.Shadow";
    private const string HorizontalThumbGlyph = "ScrollChrome.HorizontalThumb.Glyph";
    private const string HorizontalThumbHoverGlyph = "ScrollChrome.HorizontalThumb.Hover.Glyph";
    private const string HorizontalThumbPressedGlyph = "ScrollChrome.HorizontalThumb.Pressed.Glyph";
    private const string VerticalThumbGlyph = "ScrollChrome.VerticalThumb.Glyph";
    private const string VerticalThumbHoverGlyph = "ScrollChrome.VerticalThumb.Hover.Glyph";
    private const string VerticalThumbPressedGlyph = "ScrollChrome.VerticalThumb.Pressed.Glyph";
    private const string ButtonGlyph = "ScrollChrome.Button.Glyph";
    private const string ButtonEnabledGlyph = "ScrollChrome.Button.Enabled.Glyph";
    private const string ButtonHoverGlyph = "ScrollChrome.Button.Hover.Glyph";
    private const string ButtonPressedGlyph = "ScrollChrome.Button.Pressed.Glyph";
    private const string ThumbGlyphShadow = "ScrollChrome.Thumb.GlyphShadow";
    private const string ThumbHoverBorderColor = "ScrollChrome.Thumb.Hover.BorderColor";
    private const string ThumbPressedBorderColor = "ScrollChrome.Thumb.Pressed.BorderColor";

    // The colors the code animations move to: "<brush>" + stop index + "Color".
    private static readonly string[] s_enabledButtonGlyph = Indexed("ScrollChrome.Button.Enabled.Glyph", 3);
    private static readonly string[] s_hoverFill = Indexed("ScrollChrome.Thumb.Hover.Fill", 4);
    private static readonly string[] s_hoverThumbGlyph = Indexed("ScrollChrome.Thumb.Hover.Glyph", 3);
    private static readonly string[] s_hoverButtonGlyph = Indexed("ScrollChrome.Button.Hover.Glyph", 3);
    private static readonly string[] s_pressedFill = Indexed("ScrollChrome.Thumb.Pressed.Fill", 4);
    private static readonly string[] s_pressedThumbGlyph = Indexed("ScrollChrome.Thumb.Pressed.Glyph", 3);
    private static readonly string[] s_pressedButtonGlyph = Indexed("ScrollChrome.Button.Pressed.Glyph", 3);

    private static Geometry? s_leftArrowGeometry;
    private static Geometry? s_rightArrowGeometry;
    private static Geometry? s_upArrowGeometry;
    private static Geometry? s_downArrowGeometry;

    private readonly ChromeAnimator _animator;
    private readonly ChromeTokens _tokens;
    private ScrollGlyph _scrollGlyph;
    private LocalResources? _localResources;

    static ScrollChrome()
    {
        AffectsRender<ScrollChrome>(ScrollGlyphProperty, RenderMouseOverProperty, RenderPressedProperty, IsEffectivelyEnabledProperty);
    }

    /// <summary>Initializes a new instance of the <see cref="ScrollChrome"/> class.</summary>
    public ScrollChrome()
    {
        _animator = new ChromeAnimator(this);
        _tokens = new ChromeTokens(this);
        ResourcesChanged += (_, _) => OnThemeResourcesChanged();
        ActualThemeVariantChanged += (_, _) => OnThemeResourcesChanged();
    }

    /// <summary>
    /// The chrome token names this class reads, without the <c>&lt;Family&gt;.Chrome.</c> prefix. Keys ending in
    /// <c>Color</c> are the colors the code animations move to.
    /// </summary>
    public static IReadOnlyList<string> TokenSet { get; } = BuildTokenSet();

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

    // As WPF: animate only when client-area animation is on. Unlike the other chrome, the disabled look animates too.
    private bool Animates => WpfAnimations.IsMotionEnabled(this);

    private LocalResources? Local => Animates ? _localResources : null;

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

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ScrollGlyphProperty)
        {
            _scrollGlyph = change.GetNewValue<ScrollGlyph>();

            // The per-instance clones depend on the glyph kind; start again from the static look.
            DropLocalResources();
        }
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            OnEnabledChanged(change.GetNewValue<bool>());
        }
        else if (change.Property == RenderMouseOverProperty)
        {
            OnRenderMouseOverChanged(change.GetNewValue<bool>());
        }
        else if (change.Property == RenderPressedProperty)
        {
            OnRenderPressedChanged(change.GetNewValue<bool>());
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DropLocalResources();
    }

    /// <summary>Returns a zero size: the chrome fills whatever it is given.</summary>
    protected override Size MeasureOverride(Size availableSize) => default;

    /// <summary>Takes the final size.</summary>
    protected override Size ArrangeOverride(Size finalSize) => finalSize;

    /// <summary>Draws the shadow, the outer and inner borders with the fill, and the glyph.</summary>
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var snap = DeviceSnapper.For(this);
        _scrollGlyph = GetScrollGlyph(this);

        // The rectangle is the center line of the 1 px outermost stroke.
        if ((bounds.Width >= 1.0) && (bounds.Height >= 1.0))
        {
            bounds = new Rect(bounds.X + 0.5, bounds.Y + 0.5, bounds.Width - 1.0, bounds.Height - 1.0);
        }

        switch (_scrollGlyph)
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

        DrawShadow(context, ref bounds, snap);
        DrawBorders(context, ref bounds, snap);
        DrawGlyph(context, bounds, snap);
    }

    // The snapped center line of a 1 px stroke whose WPF center line is the given rectangle.
    private static Rect StrokeRect(Rect centerLine, DeviceSnapper snap) =>
        snap.Stroke(centerLine.X - 0.5, centerLine.Y - 0.5, centerLine.Width + 1.0, centerLine.Height + 1.0);

    private void DrawShadow(DrawingContext dc, ref Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width > 0.0) && (bounds.Height > 2.0))
        {
            if (Shadow(snap.Line) is { } pen)
            {
                dc.DrawRectangle(null, pen, StrokeRect(new Rect(bounds.X, bounds.Y + 2.0, bounds.Width, bounds.Height - 2.0), snap), 3.0, 3.0);
            }

            bounds = new Rect(bounds.X, bounds.Y, Math.Max(0.0, bounds.Width - 1.0), bounds.Height - 1.0);
        }
    }

    private void DrawBorders(DrawingContext dc, ref Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width >= 2.0) && (bounds.Height >= 2.0))
        {
            var brush = Fill;
            var pen = OuterBorder(snap.Line);
            if (pen != null)
            {
                dc.DrawRectangle(brush, pen, StrokeRect(bounds, snap), 1.0, 1.0);
                brush = null;
            }

            bounds = bounds.Deflate(1.0);
            if ((bounds.Width >= 2.0) && (bounds.Height >= 2.0))
            {
                pen = InnerBorder(snap.Line);
                if ((pen != null) || (brush != null))
                {
                    dc.DrawRectangle(brush, pen, StrokeRect(bounds, snap), 0.5, 0.5);
                }

                bounds = bounds.Deflate(1.0);
            }
        }
    }

    private void DrawGlyph(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width > 0.0) && (bounds.Height > 0.0))
        {
            var brush = Glyph;
            if ((brush != null) && (_scrollGlyph != ScrollGlyph.None))
            {
                switch (_scrollGlyph)
                {
                    case ScrollGlyph.HorizontalGripper:
                        DrawHorizontalGripper(dc, brush, bounds, snap);
                        break;
                    case ScrollGlyph.VerticalGripper:
                        DrawVerticalGripper(dc, brush, bounds, snap);
                        break;
                    case ScrollGlyph.LeftArrow:
                    case ScrollGlyph.RightArrow:
                    case ScrollGlyph.UpArrow:
                    case ScrollGlyph.DownArrow:
                        DrawArrow(dc, brush, bounds);
                        break;
                }
            }
        }
    }

    private void DrawHorizontalGripper(DrawingContext dc, IBrush brush, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width > 15.0) && (bounds.Height > 2.0))
        {
            var glyphShadow = GlyphShadow;
            var height = Math.Min(7.0, bounds.Height);
            var shadowHeight = height + 1.0;
            var x = bounds.X + ((bounds.Width * 0.5) - 4.0);
            var y = bounds.Y + ((bounds.Height - height) * 0.5);

            for (var i = 0; i < 9; i += 3)
            {
                if (glyphShadow != null)
                {
                    dc.DrawRectangle(glyphShadow, null, snap.Fill(x + i - 0.5, y - 0.5, 3.0, shadowHeight));
                }

                dc.DrawRectangle(brush, null, snap.Fill(x + i, y, 2.0, height));
            }
        }
    }

    private void DrawVerticalGripper(DrawingContext dc, IBrush brush, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width > 2.0) && (bounds.Height > 15.0))
        {
            var glyphShadow = GlyphShadow;
            var width = Math.Min(7.0, bounds.Width);
            var shadowWidth = width + 1.0;
            var x = bounds.X + ((bounds.Width - width) * 0.5);
            var y = bounds.Y + ((bounds.Height * 0.5) - 4.0);

            for (var i = 0; i < 9; i += 3)
            {
                if (glyphShadow != null)
                {
                    dc.DrawRectangle(glyphShadow, null, snap.Fill(x - 0.5, y + i - 0.5, shadowWidth, 3.0));
                }

                dc.DrawRectangle(brush, null, snap.Fill(x, y + i, width, 2.0));
            }
        }
    }

    private void DrawArrow(DrawingContext dc, IBrush brush, Rect bounds)
    {
        var glyphWidth = 7.0;
        var glyphHeight = 4.0;
        if (_scrollGlyph is ScrollGlyph.LeftArrow or ScrollGlyph.RightArrow)
        {
            glyphWidth = 4.0;
            glyphHeight = 7.0;
        }

        Matrix matrix;
        if ((bounds.Width < glyphWidth) || (bounds.Height < glyphHeight))
        {
            var widthScale = Math.Min(glyphWidth, bounds.Width) / glyphWidth;
            var heightScale = Math.Min(glyphHeight, bounds.Height) / glyphHeight;
            var x = ((bounds.X + (bounds.Width * 0.5)) / widthScale) - (glyphWidth * 0.5);
            var y = ((bounds.Y + (bounds.Height * 0.5)) / heightScale) - (glyphHeight * 0.5);

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

        var geometry = _scrollGlyph switch
        {
            ScrollGlyph.LeftArrow => s_leftArrowGeometry ??= Triangle(new Point(4.0, 0.0), new Point(0, 3.5), new Point(4.0, 7.0)),
            ScrollGlyph.RightArrow => s_rightArrowGeometry ??= Triangle(new Point(0.0, 0.0), new Point(4, 3.5), new Point(0.0, 7.0)),
            ScrollGlyph.UpArrow => s_upArrowGeometry ??= Triangle(new Point(0.0, 4.0), new Point(3.5, 0), new Point(7.0, 4.0)),
            _ => s_downArrowGeometry ??= Triangle(new Point(0.0, 0.0), new Point(3.5, 4.0), new Point(7.0, 0.0)),
        };

        using (dc.PushTransform(matrix))
        {
            dc.DrawGeometry(brush, null, geometry);
        }
    }

    private static Geometry Triangle(Point a, Point b, Point c)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(a, true);
            ctx.LineTo(b);
            ctx.LineTo(c);
            ctx.EndFigure(true);
        }

        return geometry;
    }

    private string FillToken(string horizontal, string vertical) =>
        _scrollGlyph is ScrollGlyph.HorizontalGripper or ScrollGlyph.LeftArrow or ScrollGlyph.RightArrow ? horizontal : vertical;

    private IBrush? Fill
    {
        get
        {
            if (Local is { } local)
            {
                return ChromeBrush.With(_tokens.Brush(local.FillToken), local.FillColors(), local.FillOpacity.Value);
            }

            if (RenderPressed)
            {
                return _tokens.Brush(FillToken(HorizontalThumbPressedFill, VerticalThumbPressedFill));
            }

            if (RenderMouseOver)
            {
                return _tokens.Brush(FillToken(HorizontalThumbHoverFill, VerticalThumbHoverFill));
            }

            return IsEffectivelyEnabled || IsGripper ? _tokens.Brush(FillToken(HorizontalThumbFill, VerticalThumbFill)) : null;
        }
    }

    private IPen? OuterBorder(double thickness)
    {
        if (Local is { } local)
        {
            return ChromeBrush.Pen(ChromeBrush.With(_tokens.Brush(ThumbBorder), [local.OuterBorderColor.Value], local.OuterBorderOpacity.Value), thickness);
        }

        if (RenderPressed)
        {
            return _tokens.Pen(ThumbPressedBorder, thickness);
        }

        if (RenderMouseOver)
        {
            return _tokens.Pen(ThumbHoverBorder, thickness);
        }

        return IsEffectivelyEnabled || IsGripper ? _tokens.Pen(ThumbBorder, thickness) : null;
    }

    private IPen? InnerBorder(double thickness)
    {
        if (Local is { } local)
        {
            return ChromeBrush.Pen(ChromeBrush.WithOpacity(_tokens.Brush(ThumbInnerBorder), local.InnerBorderOpacity.Value), thickness);
        }

        return IsEffectivelyEnabled || IsGripper ? _tokens.Pen(ThumbInnerBorder, thickness) : null;
    }

    private IPen? Shadow(double thickness)
    {
        if (Local is { } local)
        {
            return ChromeBrush.Pen(ChromeBrush.WithOpacity(_tokens.Brush(ThumbShadow), local.ShadowOpacity.Value), thickness);
        }

        return IsEffectivelyEnabled || IsGripper ? _tokens.Pen(ThumbShadow, thickness) : null;
    }

    private IBrush? Glyph
    {
        get
        {
            if (Local is { } local)
            {
                return ChromeBrush.With(_tokens.Brush(local.GlyphToken), local.GlyphColors(), local.GlyphOpacity.Value);
            }

            if (_scrollGlyph == ScrollGlyph.HorizontalGripper)
            {
                return _tokens.Brush(RenderPressed ? HorizontalThumbPressedGlyph : RenderMouseOver ? HorizontalThumbHoverGlyph : HorizontalThumbGlyph);
            }

            if (_scrollGlyph == ScrollGlyph.VerticalGripper)
            {
                return _tokens.Brush(RenderPressed ? VerticalThumbPressedGlyph : RenderMouseOver ? VerticalThumbHoverGlyph : VerticalThumbGlyph);
            }

            if (RenderPressed)
            {
                return _tokens.Brush(ButtonPressedGlyph);
            }

            if (RenderMouseOver)
            {
                return _tokens.Brush(ButtonHoverGlyph);
            }

            return _tokens.Brush(IsEffectivelyEnabled ? ButtonEnabledGlyph : ButtonGlyph);
        }
    }

    private IBrush? GlyphShadow
    {
        get
        {
            if (Local is { } local)
            {
                return local.IsGripper ? ChromeBrush.WithOpacity(_tokens.Brush(ThumbGlyphShadow), local.GlyphShadowOpacity.Value) : null;
            }

            return IsGripper ? _tokens.Brush(ThumbGlyphShadow) : null;
        }
    }

    private void OnEnabledChanged(bool newValue)
    {
        if (Animates)
        {
            if (newValue)
            {
                var local = EnsureLocalResources();
                var duration = ChromeTimings.HoverIn;
                if (local.IsGripper)
                {
                    local.GlyphOpacity.AnimateTo(1, duration);
                    local.GlyphShadowOpacity.AnimateTo(_tokens.Opacity(ThumbGlyphShadow, 0.63), duration);
                }
                else
                {
                    local.FillOpacity.AnimateTo(1, duration);
                    local.OuterBorderOpacity.AnimateTo(1, duration);
                    local.InnerBorderOpacity.AnimateTo(_tokens.Opacity(ThumbInnerBorder, 0.63), duration);
                    local.ShadowOpacity.AnimateTo(_tokens.Opacity(ThumbShadow, 0.5), duration);
                    AnimateTo(local.Glyph, s_enabledButtonGlyph, duration);
                }
            }
            else if (_localResources == null)
            {
                InvalidateVisual();
            }
            else
            {
                var local = _localResources;
                var duration = ChromeTimings.HoverOut;
                if (local.IsGripper)
                {
                    local.GlyphOpacity.AnimateTo(local.GlyphOpacityBase, duration);
                    local.GlyphShadowOpacity.AnimateTo(local.GlyphShadowOpacityBase, duration);
                }
                else
                {
                    local.FillOpacity.AnimateTo(local.FillOpacityBase, duration);
                    local.OuterBorderOpacity.AnimateTo(local.OuterBorderOpacityBase, duration);
                    local.InnerBorderOpacity.AnimateTo(local.InnerBorderOpacityBase, duration);
                    local.ShadowOpacity.AnimateTo(local.ShadowOpacityBase, duration);
                    AnimateToBase(local.Glyph, local.GlyphBase, duration);
                }
            }
        }
        else
        {
            DropLocalResources();
            InvalidateVisual();
        }
    }

    private void OnRenderMouseOverChanged(bool newValue)
    {
        if (Animates)
        {
            var local = EnsureLocalResources();
            if (newValue)
            {
                AnimateToHover(local);
            }
            else
            {
                var duration = ChromeTimings.HoverOut;
                local.OuterBorderColor.AnimateTo(local.OuterBorderColorBase, duration);
                AnimateToBase(local.Fill, local.FillBase, duration);
                AnimateToBase(local.Glyph, local.GlyphBase, duration);
            }
        }
        else
        {
            DropLocalResources();
            InvalidateVisual();
        }
    }

    private void AnimateToHover(LocalResources local)
    {
        var duration = ChromeTimings.HoverIn;
        local.OuterBorderColor.AnimateTo(_tokens.Color(ThumbHoverBorderColor), duration);
        AnimateTo(local.Fill, s_hoverFill, duration);
        AnimateTo(local.Glyph, local.IsGripper ? s_hoverThumbGlyph : s_hoverButtonGlyph, duration);
    }

    private void OnRenderPressedChanged(bool newValue)
    {
        if (Animates)
        {
            var local = EnsureLocalResources();
            if (newValue)
            {
                var duration = ChromeTimings.HoverIn;
                local.OuterBorderColor.AnimateTo(_tokens.Color(ThumbPressedBorderColor), duration);
                AnimateTo(local.Fill, s_pressedFill, duration);
                AnimateTo(local.Glyph, local.IsGripper ? s_pressedThumbGlyph : s_pressedButtonGlyph, duration);
            }
            else
            {
                AnimateToHover(local);
            }
        }
        else
        {
            DropLocalResources();
            InvalidateVisual();
        }
    }

    private void AnimateTo(AnimatedColor[] values, string[] tokens, TimeSpan duration)
    {
        for (var i = 0; i < tokens.Length; i++)
        {
            values[i].AnimateTo(_tokens.Color(tokens[i]), duration);
        }
    }

    private static void AnimateToBase(AnimatedColor[] values, Color[] bases, TimeSpan duration)
    {
        for (var i = 0; i < values.Length; i++)
        {
            values[i].AnimateTo(bases[i], duration);
        }
    }

    private void OnThemeResourcesChanged()
    {
        DropLocalResources();
        _tokens.Clear();
        InvalidateVisual();
    }

    private LocalResources EnsureLocalResources()
    {
        if (_localResources == null)
        {
            _scrollGlyph = GetScrollGlyph(this);
            var fillToken = FillToken(HorizontalThumbFill, VerticalThumbFill);
            var glyphToken = _scrollGlyph switch
            {
                ScrollGlyph.HorizontalGripper => HorizontalThumbGlyph,
                ScrollGlyph.VerticalGripper => VerticalThumbGlyph,
                _ => ButtonGlyph,
            };
            _localResources = new LocalResources(_animator, _tokens, IsGripper, fillToken, glyphToken);
            InvalidateVisual();
        }

        return _localResources;
    }

    private void DropLocalResources()
    {
        _localResources?.Stop();
        _localResources = null;
    }

    private static string[] Indexed(string brush, int count)
    {
        var names = new string[count];
        for (var i = 0; i < count; i++)
        {
            names[i] = brush + i + "Color";
        }

        return names;
    }

    private static string[] BuildTokenSet()
    {
        var list = new List<string>
        {
            HorizontalThumbFill, HorizontalThumbHoverFill, HorizontalThumbPressedFill, VerticalThumbFill,
            VerticalThumbHoverFill, VerticalThumbPressedFill, ThumbBorder, ThumbHoverBorder, ThumbPressedBorder,
            ThumbInnerBorder, ThumbShadow, HorizontalThumbGlyph, HorizontalThumbHoverGlyph, HorizontalThumbPressedGlyph,
            VerticalThumbGlyph, VerticalThumbHoverGlyph, VerticalThumbPressedGlyph, ButtonGlyph, ButtonEnabledGlyph,
            ButtonHoverGlyph, ButtonPressedGlyph, ThumbGlyphShadow, ThumbHoverBorderColor, ThumbPressedBorderColor,
        };

        foreach (var set in new[] { s_enabledButtonGlyph, s_hoverFill, s_hoverThumbGlyph, s_hoverButtonGlyph, s_pressedFill, s_pressedThumbGlyph, s_pressedButtonGlyph })
        {
            list.AddRange(set);
        }

        return list.ToArray();
    }

    /// <summary>The per-instance animated values, as WPF's per-instance brush clones.</summary>
    private sealed class LocalResources
    {
        public LocalResources(ChromeAnimator animator, ChromeTokens tokens, bool isGripper, string fillToken, string glyphToken)
        {
            IsGripper = isGripper;
            FillToken = fillToken;
            GlyphToken = glyphToken;

            FillBase = ChromeBrush.StopColors(tokens.Brush(fillToken), 4);
            OuterBorderColorBase = ChromeBrush.StopColors(tokens.Brush(ThumbBorder), 1)[0];
            GlyphBase = ChromeBrush.StopColors(tokens.Brush(glyphToken), 3);

            FillOpacityBase = isGripper ? tokens.Opacity(fillToken, 1) : 0;
            OuterBorderOpacityBase = isGripper ? tokens.Opacity(ThumbBorder, 1) : 0;
            InnerBorderOpacityBase = isGripper ? tokens.Opacity(ThumbInnerBorder, 0.63) : 0;
            ShadowOpacityBase = isGripper ? tokens.Opacity(ThumbShadow, 0.5) : 0;
            GlyphOpacityBase = isGripper ? 1 : tokens.Opacity(glyphToken, 1);
            GlyphShadowOpacityBase = 1;

            FillOpacity = animator.CreateDouble(FillOpacityBase);
            OuterBorderOpacity = animator.CreateDouble(OuterBorderOpacityBase);
            InnerBorderOpacity = animator.CreateDouble(InnerBorderOpacityBase);
            ShadowOpacity = animator.CreateDouble(ShadowOpacityBase);
            GlyphOpacity = animator.CreateDouble(GlyphOpacityBase);
            GlyphShadowOpacity = animator.CreateDouble(GlyphShadowOpacityBase);
            OuterBorderColor = animator.CreateColor(OuterBorderColorBase);
            Fill = Create(animator, FillBase);
            Glyph = Create(animator, GlyphBase);
        }

        public bool IsGripper { get; }

        public string FillToken { get; }

        public string GlyphToken { get; }

        public Color[] FillBase { get; }

        public Color OuterBorderColorBase { get; }

        public Color[] GlyphBase { get; }

        public double FillOpacityBase { get; }

        public double OuterBorderOpacityBase { get; }

        public double InnerBorderOpacityBase { get; }

        public double ShadowOpacityBase { get; }

        public double GlyphOpacityBase { get; }

        public double GlyphShadowOpacityBase { get; }

        public AnimatedDouble FillOpacity { get; }

        public AnimatedDouble OuterBorderOpacity { get; }

        public AnimatedDouble InnerBorderOpacity { get; }

        public AnimatedDouble ShadowOpacity { get; }

        public AnimatedDouble GlyphOpacity { get; }

        public AnimatedDouble GlyphShadowOpacity { get; }

        public AnimatedColor OuterBorderColor { get; }

        public AnimatedColor[] Fill { get; }

        public AnimatedColor[] Glyph { get; }

        public Color[] FillColors() => Values(Fill);

        public Color[] GlyphColors() => Values(Glyph);

        public void Stop()
        {
            FillOpacity.Stop();
            OuterBorderOpacity.Stop();
            InnerBorderOpacity.Stop();
            ShadowOpacity.Stop();
            GlyphOpacity.Stop();
            GlyphShadowOpacity.Stop();
            OuterBorderColor.Stop();
            foreach (var v in Fill)
            {
                v.Stop();
            }

            foreach (var v in Glyph)
            {
                v.Stop();
            }
        }

        private static AnimatedColor[] Create(ChromeAnimator animator, Color[] colors)
        {
            var values = new AnimatedColor[colors.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                values[i] = animator.CreateColor(colors[i]);
            }

            return values;
        }

        private static Color[] Values(AnimatedColor[] values)
        {
            var colors = new Color[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                colors[i] = values[i].Value;
            }

            return colors;
        }
    }
}
