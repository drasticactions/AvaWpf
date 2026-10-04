// Ported from WPF $W/Themes/PresentationFramework.Aero/Microsoft/Windows/Themes/BulletChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaWpf.Animations;

namespace AvaWpf.Chrome.Aero;

/// <summary>The Aero check box and radio button bullet (WPF <c>Microsoft.Windows.Themes.BulletChrome</c>).</summary>
/// <remarks>Draws everything itself and has no child; it derives from <see cref="Control"/>.</remarks>
public sealed class BulletChrome : Control
{
    /// <summary>Defines the <see cref="Background"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<BulletChrome>();

    /// <summary>Defines the <see cref="BorderBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<BulletChrome>();

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

    private const string CheckMarkStroke = "BulletChrome.CheckMark.Stroke";
    private const string CheckMarkPressedStroke = "BulletChrome.CheckMark.Pressed.Stroke";
    private const string CheckMarkDisabledFill = "BulletChrome.CheckMark.Disabled.Fill";
    private const string CheckMarkFill = "BulletChrome.CheckMark.Fill";
    private const string CheckMarkPressedFill = "BulletChrome.CheckMark.Pressed.Fill";
    private const string RadioDisabledGlyphStroke = "BulletChrome.RadioButton.Disabled.GlyphStroke";
    private const string RadioGlyphStroke = "BulletChrome.RadioButton.GlyphStroke";
    private const string RadioDisabledGlyphFill = "BulletChrome.RadioButton.Disabled.GlyphFill";
    private const string RadioGlyphFill = "BulletChrome.RadioButton.GlyphFill";
    private const string RadioHoverGlyphFill = "BulletChrome.RadioButton.Hover.GlyphFill";
    private const string RadioPressedGlyphFill = "BulletChrome.RadioButton.Pressed.GlyphFill";
    private const string IndeterminateHighlight = "BulletChrome.Indeterminate.Highlight";
    private const string IndeterminateHoverHighlight = "BulletChrome.Indeterminate.Hover.Highlight";
    private const string IndeterminatePressedHighlight = "BulletChrome.Indeterminate.Pressed.Highlight";
    private const string HoverBackground = "BulletChrome.Hover.Background";
    private const string PressedBackground = "BulletChrome.Pressed.Background";
    private const string DisabledBackground = "BulletChrome.Disabled.Background";
    private const string HoverBorder = "BulletChrome.Hover.Border";
    private const string PressedBorder = "BulletChrome.Pressed.Border";
    private const string DisabledBorder = "BulletChrome.Disabled.Border";
    private const string CheckBoxDisabledInnerBorder = "BulletChrome.CheckBox.Disabled.InnerBorder";
    private const string CheckBoxInnerBorder = "BulletChrome.CheckBox.InnerBorder";
    private const string CheckBoxHoverInnerBorder = "BulletChrome.CheckBox.Hover.InnerBorder";
    private const string CheckBoxPressedInnerBorder = "BulletChrome.CheckBox.Pressed.InnerBorder";
    private const string IndeterminateDisabledInnerBorder = "BulletChrome.Indeterminate.Disabled.InnerBorder";
    private const string IndeterminateInnerBorder = "BulletChrome.Indeterminate.InnerBorder";
    private const string IndeterminateHoverInnerBorder = "BulletChrome.Indeterminate.Hover.InnerBorder";
    private const string IndeterminatePressedInnerBorder = "BulletChrome.Indeterminate.Pressed.InnerBorder";
    private const string RadioInnerBorder = "BulletChrome.RadioButton.InnerBorder";
    private const string RadioHoverInnerBorder = "BulletChrome.RadioButton.Hover.InnerBorder";
    private const string RadioPressedInnerBorder = "BulletChrome.RadioButton.Pressed.InnerBorder";
    private const string CheckBoxInnerFill = "BulletChrome.CheckBox.InnerFill";
    private const string CheckBoxHoverInnerFill = "BulletChrome.CheckBox.Hover.InnerFill";
    private const string CheckBoxPressedInnerFill = "BulletChrome.CheckBox.Pressed.InnerFill";
    private const string IndeterminateDisabledInnerFill = "BulletChrome.Indeterminate.Disabled.InnerFill";
    private const string IndeterminateInnerFill = "BulletChrome.Indeterminate.InnerFill";
    private const string IndeterminateHoverInnerFill = "BulletChrome.Indeterminate.Hover.InnerFill";
    private const string IndeterminatePressedInnerFill = "BulletChrome.Indeterminate.Pressed.InnerFill";
    private const string PressedBorderColor = "BulletChrome.Pressed.BorderColor";
    private const string PressedBackgroundColor = "BulletChrome.Pressed.BackgroundColor";

    // The colors the code animations move to: "<brush>" + stop index + "Color".
    private static readonly string[] s_hoverIndeterminateInnerBorder = Indexed(IndeterminateHoverInnerBorder, 0, 1, 2);
    private static readonly string[] s_hoverIndeterminateInnerFill = Indexed(IndeterminateHoverInnerFill, 0, 1);
    private static readonly string[] s_hoverIndeterminateHighlight = Indexed(IndeterminateHoverHighlight, 0, 2, 3);
    private static readonly string[] s_hoverCheckBoxInnerBorder = Indexed(CheckBoxHoverInnerBorder, 0, 1, 2);
    private static readonly string[] s_hoverCheckBoxInnerFill = Indexed(CheckBoxHoverInnerFill, 0, 1);
    private static readonly string[] s_hoverRadioGlyphFill = Indexed(RadioHoverGlyphFill, 0, 1, 2);
    private static readonly string[] s_indeterminateInnerBorder = Indexed(IndeterminateInnerBorder, 0, 1, 2);
    private static readonly string[] s_indeterminateInnerFill = Indexed(IndeterminateInnerFill, 0, 1);
    private static readonly string[] s_indeterminateHighlight = Indexed(IndeterminateHighlight, 0, 2, 3);
    private static readonly string[] s_pressedIndeterminateInnerBorder = Indexed(IndeterminatePressedInnerBorder, 0, 1, 2);
    private static readonly string[] s_pressedIndeterminateInnerFill = Indexed(IndeterminatePressedInnerFill, 0, 1);
    private static readonly string[] s_pressedIndeterminateHighlight = Indexed(IndeterminatePressedHighlight, 0, 2, 3);
    private static readonly string[] s_pressedCheckBoxInnerBorder = Indexed(CheckBoxPressedInnerBorder, 0, 1, 2);
    private static readonly string[] s_pressedCheckBoxInnerFill = Indexed(CheckBoxPressedInnerFill, 0, 1);
    private static readonly string[] s_pressedRadioGlyphFill = Indexed(RadioPressedGlyphFill, 0, 1, 2);

    // The highlight stops WPF animates (stop 1 keeps its color).
    private static readonly int[] s_highlightStops = [0, 2, 3];

    private static Geometry? s_checkMarkGeometry;

    private readonly ChromeAnimator _animator;
    private readonly ChromeTokens _tokens;
    private LocalResources? _localResources;

    static BulletChrome()
    {
        AffectsRender<BulletChrome>(BackgroundProperty, BorderBrushProperty, IsRoundProperty, IsEffectivelyEnabledProperty);
        AffectsMeasure<BulletChrome>(IsRoundProperty);
    }

    /// <summary>Initializes a new instance of the <see cref="BulletChrome"/> class.</summary>
    public BulletChrome()
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

    /// <summary>The brush that fills the box or circle.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>The brush of the outer border.</summary>
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

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

    /// <summary>The check state: true draws the check mark or dot, null the indeterminate fill. Default false.</summary>
    public bool? IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>When true, the chrome draws a radio button circle; otherwise a check box.</summary>
    public bool IsRound
    {
        get => GetValue(IsRoundProperty);
        set => SetValue(IsRoundProperty, value);
    }

    // As WPF: animate only when client-area animation is on and the chrome is enabled.
    private bool Animates => IsEffectivelyEnabled && WpfAnimations.IsMotionEnabled(this);

    private LocalResources? Local => Animates ? _localResources : null;

    private static Geometry CheckMarkGeometry => s_checkMarkGeometry ??= CreateCheckMarkGeometry();

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RenderMouseOverProperty)
        {
            OnRenderMouseOverChanged(change.GetNewValue<bool>());
        }
        else if (change.Property == RenderPressedProperty)
        {
            OnRenderPressedChanged(change.GetNewValue<bool>());
        }
        else if (change.Property == IsCheckedProperty || change.Property == IsEffectivelyEnabledProperty)
        {
            // WPF also runs OnIsCheckedChanged when IsEnabled changes, to set up the glyph animations.
            OnIsCheckedChanged();
        }
        else if (change.Property == IsRoundProperty)
        {
            DropLocalResources();

            // Force an update of the glyph colors.
            OnIsCheckedChanged();
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DropLocalResources();
    }

    /// <summary>Returns 12 × 12 for a radio button and 13 × 13 for a check box.</summary>
    protected override Size MeasureOverride(Size availableSize) => IsRound ? new Size(12.0, 12.0) : new Size(13.0, 13.0);

    /// <summary>Draws the background, the inner border and fill, the glyph and the outer border.</summary>
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var snap = DeviceSnapper.For(this);

        DrawBackground(context, bounds, snap);
        DrawInnerBorder(context, bounds, snap);
        DrawGlyph(context, bounds, snap);
        DrawBorder(context, bounds, snap);
    }

    private void DrawBackground(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width > 4.0) && (bounds.Height > 4.0))
        {
            var fill = Background;
            var overlay = BackgroundOverlay;

            if (!IsRound)
            {
                var backgroundRect = snap.Inside(bounds.Left, bounds.Top, bounds.Width, bounds.Height, 1);
                if (fill != null)
                {
                    dc.DrawRectangle(fill, null, backgroundRect);
                }

                if (overlay != null)
                {
                    dc.DrawRectangle(overlay, null, backgroundRect);
                }
            }
            else
            {
                var centerX = bounds.Width * 0.5;
                var centerY = bounds.Height * 0.5;
                if (fill != null)
                {
                    dc.DrawEllipse(fill, null, new Point(centerX, centerY), centerX - 1, centerY - 1);
                }

                if (overlay != null)
                {
                    dc.DrawEllipse(overlay, null, new Point(centerX, centerY), centerX - 1, centerY - 1);
                }
            }
        }
    }

    private void DrawInnerBorder(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width >= 6.0) && (bounds.Height >= 6.0))
        {
            var innerFill = InnerFill;

            if (!IsRound)
            {
                if (innerFill != null)
                {
                    dc.DrawRectangle(innerFill, null, snap.Fill(bounds.Left + 3.0, bounds.Top + 3.0, bounds.Width - 6.0, bounds.Height - 6.0));
                }

                if (InnerBorderPen(snap.Line) is { } innerBorder)
                {
                    dc.DrawRectangle(null, innerBorder, snap.Stroke(bounds.Left + 2.0, bounds.Top + 2.0, bounds.Width - 4.0, bounds.Height - 4.0));
                }
            }
            else
            {
                var centerX = bounds.Width * 0.5;
                var centerY = bounds.Height * 0.5;

                if (innerFill != null)
                {
                    dc.DrawEllipse(innerFill, null, new Point(centerX, centerY), centerX - 3.0, centerY - 3.0);
                }

                if (InnerBorderPen(1.0) is { } innerBorder)
                {
                    dc.DrawEllipse(null, innerBorder, new Point(centerX, centerY), centerX - 2.5, centerY - 2.5);
                }
            }
        }
    }

    private void DrawGlyph(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if (!IsRound)
        {
            var glyphStroke = GlyphStroke;
            var glyphFill = GlyphFill;
            if (glyphStroke != null || glyphFill != null)
            {
                // Reverse the check mark in RTL so it draws to screen as in LTR (a mirror about x = 6.5).
                using (FlowDirection == FlowDirection.RightToLeft ? dc.PushTransform(new Matrix(-1.0, 0.0, 0.0, 1.0, 13.0, 0.0)) : default(DrawingContext.PushedState?))
                {
                    dc.DrawGeometry(null, glyphStroke, CheckMarkGeometry);
                    dc.DrawGeometry(glyphFill, null, CheckMarkGeometry);
                }
            }

            if (HighlightStroke(snap.Line) is { } highlight)
            {
                dc.DrawRectangle(null, highlight, snap.Stroke(3.0, 3.0, 7.0, 7.0));
            }
        }
        else
        {
            var centerX = bounds.Width * 0.5;
            var centerY = bounds.Height * 0.5;
            var glyphFill = GlyphFill;
            var glyphStroke = GlyphStroke;
            if (glyphFill != null || glyphStroke != null)
            {
                dc.DrawEllipse(glyphFill, glyphStroke, new Point(centerX, centerY), centerX - 3, centerY - 3);
            }
        }
    }

    private void DrawBorder(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width >= 5.0) && (bounds.Height >= 5.0))
        {
            var thickness = IsRound ? 1.0 : snap.Line;
            var pen = _tokens.Pen(BorderBrush, thickness);
            var overlayPen = BorderOverlayPen(thickness);

            if (pen != null || overlayPen != null)
            {
                if (!IsRound)
                {
                    var rect = snap.Stroke(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                    if (pen != null)
                    {
                        dc.DrawRectangle(null, pen, rect);
                    }

                    if (overlayPen != null)
                    {
                        dc.DrawRectangle(null, overlayPen, rect);
                    }
                }
                else
                {
                    var centerX = bounds.Width * 0.5;
                    var centerY = bounds.Height * 0.5;
                    if (pen != null)
                    {
                        dc.DrawEllipse(null, pen, new Point(centerX, centerY), centerX - 0.5, centerY - 0.5);
                    }

                    if (overlayPen != null)
                    {
                        dc.DrawEllipse(null, overlayPen, new Point(centerX, centerY), centerX - 0.5, centerY - 0.5);
                    }
                }
            }
        }
    }

    private IPen? HighlightStroke(double thickness)
    {
        if (Local is { } local)
        {
            return ChromeBrush.Pen(ChromeBrush.With(_tokens.Brush(IndeterminateHighlight), local.HighlightColors(), local.HighlightOpacity.Value), thickness);
        }

        if (!IsRound && IsChecked == null)
        {
            if (RenderPressed)
            {
                return _tokens.Pen(IndeterminatePressedHighlight, thickness);
            }

            return RenderMouseOver ? _tokens.Pen(IndeterminateHoverHighlight, thickness) : _tokens.Pen(IndeterminateHighlight, thickness);
        }

        return null;
    }

    private IBrush? BackgroundOverlay
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return _tokens.Brush(DisabledBackground);
            }

            if (Local is { } local)
            {
                return ChromeBrush.With(_tokens.Brush(HoverBackground), [local.BackgroundColor.Value], local.OverlayOpacity.Value);
            }

            if (RenderPressed)
            {
                return _tokens.Brush(PressedBackground);
            }

            return RenderMouseOver ? _tokens.Brush(HoverBackground) : null;
        }
    }

    private IBrush? InnerFill
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return IsChecked == null ? _tokens.Brush(IndeterminateDisabledInnerFill) : null;
            }

            if (Local is { } local)
            {
                return ChromeBrush.With(_tokens.Brush(CheckBoxInnerFill), local.InnerFillColors(), 1.0);
            }

            if (IsChecked == null)
            {
                if (RenderPressed)
                {
                    return _tokens.Brush(IndeterminatePressedInnerFill);
                }

                return RenderMouseOver ? _tokens.Brush(IndeterminateHoverInnerFill) : _tokens.Brush(IndeterminateInnerFill);
            }

            if (RenderPressed)
            {
                return _tokens.Brush(CheckBoxPressedInnerFill);
            }

            return RenderMouseOver ? _tokens.Brush(CheckBoxHoverInnerFill) : _tokens.Brush(CheckBoxInnerFill);
        }
    }

    private IPen? GlyphStroke
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return IsRound && IsChecked == true ? _tokens.Pen(RadioDisabledGlyphStroke, 1.0) : null;
            }

            if (Local is { } local)
            {
                return IsRound
                    ? ChromeBrush.Pen(ChromeBrush.WithOpacity(_tokens.Brush(RadioGlyphStroke), local.GlyphOpacity.Value), 1.0)
                    : ChromeBrush.Pen(ChromeBrush.WithOpacity(_tokens.Brush(CheckMarkStroke), local.GlyphOpacity.Value), 1.5);
            }

            if (!IsRound)
            {
                if (IsChecked == true)
                {
                    return RenderPressed ? _tokens.Pen(CheckMarkPressedStroke, 1.5) : _tokens.Pen(CheckMarkStroke, 1.5);
                }

                return IsChecked == false && RenderPressed ? _tokens.Pen(CheckMarkPressedStroke, 1.5) : null;
            }

            return IsChecked == true || RenderPressed ? _tokens.Pen(RadioGlyphStroke, 1.0) : null;
        }
    }

    private IBrush? GlyphFill
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                if (IsChecked == true)
                {
                    return !IsRound ? _tokens.Brush(CheckMarkDisabledFill) : _tokens.Brush(RadioDisabledGlyphFill);
                }

                return null;
            }

            if (Local is { } local)
            {
                return ChromeBrush.With(_tokens.Brush(IsRound ? RadioGlyphFill : CheckMarkFill), local.GlyphFillColors(), local.GlyphOpacity.Value);
            }

            if (!IsRound)
            {
                if (IsChecked == true)
                {
                    return RenderPressed ? _tokens.Brush(CheckMarkPressedFill) : _tokens.Brush(CheckMarkFill);
                }

                return IsChecked == false && RenderPressed ? _tokens.Brush(CheckMarkPressedFill) : null;
            }

            if (IsChecked == true)
            {
                if (RenderPressed)
                {
                    return _tokens.Brush(RadioPressedGlyphFill);
                }

                return RenderMouseOver ? _tokens.Brush(RadioHoverGlyphFill) : _tokens.Brush(RadioGlyphFill);
            }

            return RenderPressed ? _tokens.Brush(RadioPressedGlyphFill) : null;
        }
    }

    private IPen? BorderOverlayPen(double thickness)
    {
        if (!IsEffectivelyEnabled)
        {
            return _tokens.Pen(DisabledBorder, thickness);
        }

        if (Local is { } local)
        {
            return ChromeBrush.Pen(ChromeBrush.With(_tokens.Brush(HoverBorder), [local.BorderColor.Value], local.OverlayOpacity.Value), thickness);
        }

        if (RenderPressed)
        {
            return _tokens.Pen(PressedBorder, thickness);
        }

        return RenderMouseOver ? _tokens.Pen(HoverBorder, thickness) : null;
    }

    private IPen? InnerBorderPen(double thickness)
    {
        if (!IsEffectivelyEnabled)
        {
            return IsChecked == null ? _tokens.Pen(IndeterminateDisabledInnerBorder, thickness) : _tokens.Pen(CheckBoxDisabledInnerBorder, thickness);
        }

        // As WPF, the animated inner border is a clone of the check box one, for radio buttons too.
        if (Local is { } local)
        {
            return ChromeBrush.Pen(ChromeBrush.With(_tokens.Brush(CheckBoxInnerBorder), local.InnerBorderColors(), 1.0), thickness);
        }

        if (RenderPressed)
        {
            if (!IsRound)
            {
                return IsChecked == null ? _tokens.Pen(IndeterminatePressedInnerBorder, thickness) : _tokens.Pen(CheckBoxPressedInnerBorder, thickness);
            }

            return _tokens.Pen(RadioPressedInnerBorder, thickness);
        }

        if (RenderMouseOver)
        {
            if (!IsRound)
            {
                return IsChecked == null ? _tokens.Pen(IndeterminateHoverInnerBorder, thickness) : _tokens.Pen(CheckBoxHoverInnerBorder, thickness);
            }

            return _tokens.Pen(RadioHoverInnerBorder, thickness);
        }

        if (!IsRound)
        {
            return IsChecked == null ? _tokens.Pen(IndeterminateInnerBorder, thickness) : _tokens.Pen(CheckBoxInnerBorder, thickness);
        }

        return _tokens.Pen(RadioInnerBorder, thickness);
    }

    private void OnRenderMouseOverChanged(bool newValue)
    {
        if (Animates)
        {
            if (newValue)
            {
                AnimateToHover();
            }
            else if (_localResources == null)
            {
                InvalidateVisual();
            }
            else
            {
                var local = _localResources;
                var duration = ChromeTimings.HoverOut;
                local.OverlayOpacity.AnimateTo(0, duration);

                // WPF reuses one ColorAnimation for the dot below and only gives it a duration in the check box branch,
                // so after the indeterminate branch the dot animates over WPF's automatic 1 s.
                var glyphDuration = ChromeTimings.Automatic;
                if (IsChecked == null)
                {
                    AnimateToIndeterminate(local);
                }
                else
                {
                    glyphDuration = duration;
                    AnimateToBase(local.InnerBorder, local.InnerBorderBase, duration);
                    AnimateToBase(local.InnerFill, local.InnerFillBase, duration);
                }

                if (IsRound)
                {
                    AnimateToBase(local.GlyphFill, local.GlyphFillBase, glyphDuration);
                }
            }
        }
        else
        {
            DropLocalResources();
            InvalidateVisual();
        }
    }

    private void AnimateToHover()
    {
        var local = EnsureLocalResources();
        var duration = ChromeTimings.HoverIn;

        // Border and background overlay opacity, and their colors back to the hover colors.
        local.OverlayOpacity.AnimateTo(1, duration);
        local.BorderColor.AnimateTo(local.BorderBase, duration);
        local.BackgroundColor.AnimateTo(local.BackgroundBase, duration);

        if (IsChecked == null)
        {
            AnimateTo(local.InnerBorder, s_hoverIndeterminateInnerBorder, duration);
            AnimateTo(local.InnerFill, s_hoverIndeterminateInnerFill, duration);
            AnimateHighlightTo(local, s_hoverIndeterminateHighlight, duration);
        }
        else
        {
            AnimateTo(local.InnerBorder, s_hoverCheckBoxInnerBorder, duration);
            AnimateTo(local.InnerFill, s_hoverCheckBoxInnerFill, duration);
        }

        if (IsRound && IsChecked == true)
        {
            AnimateTo(local.GlyphFill, s_hoverRadioGlyphFill, duration);
        }
    }

    private void AnimateToIndeterminate(LocalResources local)
    {
        var duration = ChromeTimings.BulletCheck;

        local.GlyphOpacity.AnimateTo(0, duration);
        local.HighlightOpacity.AnimateTo(1.0, duration);

        AnimateTo(local.InnerBorder, s_indeterminateInnerBorder, duration);
        AnimateTo(local.InnerFill, s_indeterminateInnerFill, duration);
        AnimateHighlightTo(local, s_indeterminateHighlight, duration);
    }

    private void OnRenderPressedChanged(bool newValue)
    {
        if (Animates)
        {
            var duration = ChromeTimings.BulletPress;
            if (newValue)
            {
                var local = EnsureLocalResources();

                local.BorderColor.AnimateTo(_tokens.Color(PressedBorderColor), duration);
                local.BackgroundColor.AnimateTo(_tokens.Color(PressedBackgroundColor), duration);

                if (IsChecked == null)
                {
                    AnimateTo(local.InnerBorder, s_pressedIndeterminateInnerBorder, duration);
                    AnimateTo(local.InnerFill, s_pressedIndeterminateInnerFill, duration);
                    AnimateHighlightTo(local, s_pressedIndeterminateHighlight, duration);
                }
                else
                {
                    AnimateTo(local.InnerBorder, s_pressedCheckBoxInnerBorder, duration);
                    AnimateTo(local.InnerFill, s_pressedCheckBoxInnerFill, duration);
                }

                if (IsChecked != null)
                {
                    local.GlyphOpacity.AnimateTo(1.0, duration);
                }

                if (IsRound)
                {
                    AnimateTo(local.GlyphFill, s_pressedRadioGlyphFill, duration);
                }
            }
            else if (_localResources == null)
            {
                InvalidateVisual();
            }
            else
            {
                AnimateToHover();

                var local = _localResources;
                if (IsChecked != true)
                {
                    local.GlyphOpacity.AnimateTo(0, duration);
                }

                if (IsRound)
                {
                    AnimateToBase(local.GlyphFill, local.GlyphFillBase, duration);
                }
            }
        }
        else
        {
            DropLocalResources();
            InvalidateVisual();
        }
    }

    // Also runs when IsRound and IsEnabled change, to set up the glyph animations.
    private void OnIsCheckedChanged()
    {
        if (Animates)
        {
            var local = EnsureLocalResources();
            var duration = ChromeTimings.BulletCheck;

            if (IsChecked == null)
            {
                AnimateToIndeterminate(local);
            }
            else
            {
                local.GlyphOpacity.AnimateTo(IsChecked == true ? 1 : 0, duration);
                local.HighlightOpacity.AnimateTo(0, duration);

                if (RenderMouseOver)
                {
                    AnimateToHover();
                }
                else
                {
                    AnimateToBase(local.InnerBorder, local.InnerBorderBase, duration);
                    AnimateToBase(local.InnerFill, local.InnerFillBase, duration);
                }
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

    private void AnimateHighlightTo(LocalResources local, string[] tokens, TimeSpan duration)
    {
        for (var i = 0; i < tokens.Length; i++)
        {
            local.Highlight[s_highlightStops[i]].AnimateTo(_tokens.Color(tokens[i]), duration);
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
            _localResources = new LocalResources(
                _animator,
                _tokens.Brush(HoverBackground),
                _tokens.Brush(HoverBorder),
                _tokens.Brush(CheckBoxInnerFill),
                _tokens.Brush(CheckBoxInnerBorder),
                _tokens.Brush(IndeterminateHighlight),
                _tokens.Brush(IsRound ? RadioGlyphFill : CheckMarkFill));
            InvalidateVisual();
        }

        return _localResources;
    }

    private void DropLocalResources()
    {
        _localResources?.Stop();
        _localResources = null;
    }

    private static Geometry CreateCheckMarkGeometry()
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(9.0, 1.833), true);
            ctx.LineTo(new Point(10.667, 3.167));
            ctx.LineTo(new Point(7, 10.667));
            ctx.LineTo(new Point(5.333, 10.667));
            ctx.LineTo(new Point(3.333, 8.167));
            ctx.LineTo(new Point(3.333, 6.833));
            ctx.LineTo(new Point(4.833, 6.5));
            ctx.LineTo(new Point(6, 8));
            ctx.EndFigure(true);
        }

        return geometry;
    }

    private static string[] Indexed(string brush, params int[] stops)
    {
        var names = new string[stops.Length];
        for (var i = 0; i < stops.Length; i++)
        {
            names[i] = brush + stops[i] + "Color";
        }

        return names;
    }

    private static string[] BuildTokenSet()
    {
        var list = new List<string>
        {
            CheckMarkStroke, CheckMarkPressedStroke, CheckMarkDisabledFill, CheckMarkFill, CheckMarkPressedFill,
            RadioDisabledGlyphStroke, RadioGlyphStroke, RadioDisabledGlyphFill, RadioGlyphFill, RadioHoverGlyphFill,
            RadioPressedGlyphFill, IndeterminateHighlight, IndeterminateHoverHighlight, IndeterminatePressedHighlight,
            HoverBackground, PressedBackground, DisabledBackground, HoverBorder, PressedBorder, DisabledBorder,
            CheckBoxDisabledInnerBorder, CheckBoxInnerBorder, CheckBoxHoverInnerBorder, CheckBoxPressedInnerBorder,
            IndeterminateDisabledInnerBorder, IndeterminateInnerBorder, IndeterminateHoverInnerBorder,
            IndeterminatePressedInnerBorder, RadioInnerBorder, RadioHoverInnerBorder, RadioPressedInnerBorder,
            CheckBoxInnerFill, CheckBoxHoverInnerFill, CheckBoxPressedInnerFill, IndeterminateDisabledInnerFill,
            IndeterminateInnerFill, IndeterminateHoverInnerFill, IndeterminatePressedInnerFill, PressedBorderColor,
            PressedBackgroundColor,
        };

        foreach (var set in new[]
        {
            s_hoverIndeterminateInnerBorder, s_hoverIndeterminateInnerFill, s_hoverIndeterminateHighlight,
            s_hoverCheckBoxInnerBorder, s_hoverCheckBoxInnerFill, s_hoverRadioGlyphFill, s_indeterminateInnerBorder,
            s_indeterminateInnerFill, s_indeterminateHighlight, s_pressedIndeterminateInnerBorder,
            s_pressedIndeterminateInnerFill, s_pressedIndeterminateHighlight, s_pressedCheckBoxInnerBorder,
            s_pressedCheckBoxInnerFill, s_pressedRadioGlyphFill,
        })
        {
            list.AddRange(set);
        }

        return list.ToArray();
    }

    /// <summary>The per-instance animated values, as WPF's per-instance brush clones.</summary>
    private sealed class LocalResources
    {
        public LocalResources(ChromeAnimator animator, IBrush? hoverBackground, IBrush? hoverBorder, IBrush? innerFill, IBrush? innerBorder, IBrush? highlight, IBrush? glyphFill)
        {
            BackgroundBase = ChromeBrush.StopColors(hoverBackground, 1)[0];
            BorderBase = ChromeBrush.StopColors(hoverBorder, 1)[0];
            InnerFillBase = ChromeBrush.StopColors(innerFill, 2);
            InnerBorderBase = ChromeBrush.StopColors(innerBorder, 3);
            GlyphFillBase = ChromeBrush.StopColors(glyphFill, 3);

            // WPF animates the background and border overlay opacities together, and the glyph stroke and fill
            // opacities together.
            OverlayOpacity = animator.CreateDouble(0);
            HighlightOpacity = animator.CreateDouble(0);
            GlyphOpacity = animator.CreateDouble(0);
            BackgroundColor = animator.CreateColor(BackgroundBase);
            BorderColor = animator.CreateColor(BorderBase);
            InnerFill = Create(animator, InnerFillBase);
            InnerBorder = Create(animator, InnerBorderBase);
            Highlight = Create(animator, ChromeBrush.StopColors(highlight, 4));
            GlyphFill = Create(animator, GlyphFillBase);
        }

        public Color BackgroundBase { get; }

        public Color BorderBase { get; }

        public Color[] InnerFillBase { get; }

        public Color[] InnerBorderBase { get; }

        public Color[] GlyphFillBase { get; }

        public AnimatedDouble OverlayOpacity { get; }

        public AnimatedDouble HighlightOpacity { get; }

        public AnimatedDouble GlyphOpacity { get; }

        public AnimatedColor BackgroundColor { get; }

        public AnimatedColor BorderColor { get; }

        public AnimatedColor[] InnerFill { get; }

        public AnimatedColor[] InnerBorder { get; }

        public AnimatedColor[] Highlight { get; }

        public AnimatedColor[] GlyphFill { get; }

        public Color[] InnerFillColors() => Values(InnerFill);

        public Color[] InnerBorderColors() => Values(InnerBorder);

        public Color[] HighlightColors() => Values(Highlight);

        public Color[] GlyphFillColors() => Values(GlyphFill);

        public void Stop()
        {
            OverlayOpacity.Stop();
            HighlightOpacity.Stop();
            GlyphOpacity.Stop();
            BackgroundColor.Stop();
            BorderColor.Stop();
            foreach (var set in new[] { InnerFill, InnerBorder, Highlight, GlyphFill })
            {
                foreach (var v in set)
                {
                    v.Stop();
                }
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
