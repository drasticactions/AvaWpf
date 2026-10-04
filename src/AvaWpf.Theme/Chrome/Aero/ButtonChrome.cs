// Ported from WPF $W/Themes/PresentationFramework.Aero/Microsoft/Windows/Themes/ButtonChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaWpf.Animations;

namespace AvaWpf.Chrome.Aero;

/// <summary>The Aero button chrome (WPF <c>Microsoft.Windows.Themes.ButtonChrome</c>); it insets its child by 2 px.</summary>
public sealed class ButtonChrome : Decorator
{
    /// <summary>Defines the <see cref="Background"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<ButtonChrome>();

    /// <summary>Defines the <see cref="BorderBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<ButtonChrome>();

    /// <summary>Defines the <see cref="RenderDefaulted"/> property.</summary>
    public static readonly StyledProperty<bool> RenderDefaultedProperty =
        AvaloniaProperty.Register<ButtonChrome, bool>(nameof(RenderDefaulted));

    /// <summary>Defines the <see cref="RenderMouseOver"/> property.</summary>
    public static readonly StyledProperty<bool> RenderMouseOverProperty =
        AvaloniaProperty.Register<ButtonChrome, bool>(nameof(RenderMouseOver));

    /// <summary>Defines the <see cref="RenderPressed"/> property.</summary>
    public static readonly StyledProperty<bool> RenderPressedProperty =
        AvaloniaProperty.Register<ButtonChrome, bool>(nameof(RenderPressed));

    /// <summary>Defines the <see cref="RoundCorners"/> property.</summary>
    public static readonly StyledProperty<bool> RoundCornersProperty =
        AvaloniaProperty.Register<ButtonChrome, bool>(nameof(RoundCorners), true);

    private const string HoverFill = "ButtonChrome.Hover.Fill";
    private const string PressedFill = "ButtonChrome.Pressed.Fill";
    private const string DisabledFill = "ButtonChrome.Disabled.Fill";
    private const string HoverBorder = "ButtonChrome.Hover.Border";
    private const string PressedBorder = "ButtonChrome.Pressed.Border";
    private const string DisabledBorder = "ButtonChrome.Disabled.Border";
    private const string InnerBorder = "ButtonChrome.InnerBorder";
    private const string DefaultedInnerBorder = "ButtonChrome.Defaulted.InnerBorder";
    private const string PressedLeftShadow = "ButtonChrome.Pressed.LeftShadow";
    private const string PressedTopShadow = "ButtonChrome.Pressed.TopShadow";
    private const string DefaultedInnerBorderColor = "ButtonChrome.Defaulted.InnerBorderColor";
    private const string PressedFill0Color = "ButtonChrome.Pressed.Fill0Color";
    private const string PressedFill2Color = "ButtonChrome.Pressed.Fill2Color";
    private const string PressedFill3Color = "ButtonChrome.Pressed.Fill3Color";
    private const string PressedBorderColor = "ButtonChrome.Pressed.BorderColor";

    private readonly ChromeAnimator _animator;
    private readonly ChromeTokens _tokens;
    private LocalResources? _localResources;

    static ButtonChrome()
    {
        AffectsRender<ButtonChrome>(BackgroundProperty, BorderBrushProperty, RoundCornersProperty, IsEffectivelyEnabledProperty);
    }

    /// <summary>Initializes a new instance of the <see cref="ButtonChrome"/> class.</summary>
    public ButtonChrome()
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
    public static IReadOnlyList<string> TokenSet { get; } =
    [
        HoverFill, PressedFill, DisabledFill, HoverBorder, PressedBorder, DisabledBorder, InnerBorder,
        DefaultedInnerBorder, PressedLeftShadow, PressedTopShadow, DefaultedInnerBorderColor, PressedFill0Color,
        PressedFill2Color, PressedFill3Color, PressedBorderColor,
    ];

    /// <summary>The brush that fills the background of the button.</summary>
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

    /// <summary>When true, the chrome draws the default-button look: a cyan inner border and a pulsing hover fill.</summary>
    public bool RenderDefaulted
    {
        get => GetValue(RenderDefaultedProperty);
        set => SetValue(RenderDefaultedProperty, value);
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

    /// <summary>When true (the default), the left corners are round; otherwise they are square.</summary>
    public bool RoundCorners
    {
        get => GetValue(RoundCornersProperty);
        set => SetValue(RoundCornersProperty, value);
    }

    // As WPF: animate only when client-area animation is on (WpfAnimations) and the chrome is enabled.
    private bool Animates => IsEffectivelyEnabled && WpfAnimations.IsMotionEnabled(this);

    // Without animation state the chrome draws the static look of its current state.
    private LocalResources? Local => Animates ? _localResources : null;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RenderDefaultedProperty)
        {
            OnRenderDefaultedChanged(change.GetNewValue<bool>());
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
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ResumeDefaultPulse();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DropLocalResources();
    }

    /// <summary>Measures the child 4 px smaller than the available size and adds 4 px to its desired size.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size desired;
        var child = Child;
        if (child != null)
        {
            var isWidthTooSmall = availableSize.Width < 4.0;
            var isHeightTooSmall = availableSize.Height < 4.0;
            var childConstraint = new Size(isWidthTooSmall ? 0 : availableSize.Width - 4.0, isHeightTooSmall ? 0 : availableSize.Height - 4.0);

            child.Measure(childConstraint);
            desired = child.DesiredSize;

            desired = new Size(desired.Width + (isWidthTooSmall ? 0 : 4.0), desired.Height + (isHeightTooSmall ? 0 : 4.0));
        }
        else
        {
            desired = new Size(Math.Min(4.0, availableSize.Width), Math.Min(4.0, availableSize.Height));
        }

        return desired;
    }

    /// <summary>Arranges the child centered, 2 px in from every side.</summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(0d, finalSize.Width - 4.0);
        var height = Math.Max(0d, finalSize.Height - 4.0);
        var childArrangeRect = new Rect((finalSize.Width - width) * 0.5, (finalSize.Height - height) * 0.5, width, height);

        Child?.Arrange(childArrangeRect);

        return finalSize;
    }

    /// <summary>Draws the background, the pressed shadows, the outer border and the inner border.</summary>
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var snap = DeviceSnapper.For(this);

        DrawBackground(context, bounds, snap);
        DrawDropShadows(context, bounds, snap);
        DrawBorder(context, bounds, snap);
        DrawInnerBorder(context, bounds, snap);
    }

    private void DrawBackground(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if (!IsEffectivelyEnabled && !RoundCorners)
        {
            return;
        }

        var fill = Background;
        if ((bounds.Width > 4.0) && (bounds.Height > 4.0))
        {
            var backgroundRect = snap.Inside(bounds.Left, bounds.Top, bounds.Width, bounds.Height, 1);

            if (fill != null)
            {
                dc.DrawRectangle(fill, null, backgroundRect);
            }

            fill = BackgroundOverlay;
            if (fill != null)
            {
                dc.DrawRectangle(fill, null, backgroundRect);
            }
        }
    }

    private void DrawDropShadows(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width > 4.0) && (bounds.Height > 4.0))
        {
            if (LeftDropShadowBrush is { } leftShadow)
            {
                dc.DrawRectangle(leftShadow, null, snap.Fill(1.0, 1.0, 2.0, bounds.Bottom - 2.0));
            }

            if (TopDropShadowBrush is { } topShadow)
            {
                dc.DrawRectangle(topShadow, null, snap.Fill(1.0, 1.0, bounds.Right - 2.0, 2.0));
            }
        }
    }

    private void DrawBorder(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if ((bounds.Width >= 5.0) && (bounds.Height >= 5.0))
        {
            var pen = _tokens.Pen(BorderBrush, snap.Line);
            var overlayPen = BorderOverlayPen(snap.Line);
            var isEnabled = IsEffectivelyEnabled;

            if (pen != null || overlayPen != null)
            {
                var rect = snap.Stroke(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                if (RoundCorners)
                {
                    if (isEnabled && pen != null)
                    {
                        dc.DrawRectangle(null, pen, rect, 2.75, 2.75);
                    }

                    if (overlayPen != null)
                    {
                        dc.DrawRectangle(null, overlayPen, rect, 2.75, 2.75);
                    }
                }
                else
                {
                    // The left side is flat; the right corners have a 2 px radius.
                    var borderGeometry = new StreamGeometry();
                    using (var ctx = borderGeometry.Open())
                    {
                        ctx.BeginFigure(new Point(rect.Left, rect.Top), false);
                        ctx.LineTo(new Point(rect.Left, rect.Bottom));
                        ctx.LineTo(new Point(rect.Right - 2.0, rect.Bottom));
                        ctx.ArcTo(new Point(rect.Right, rect.Bottom - 2.0), new Size(2.0, 2.0), 0.0, false, SweepDirection.CounterClockwise);
                        ctx.LineTo(new Point(rect.Right, rect.Top + 2.0));
                        ctx.ArcTo(new Point(rect.Right - 2.0, rect.Top), new Size(2.0, 2.0), 0.0, false, SweepDirection.CounterClockwise);
                        ctx.EndFigure(true);
                    }

                    if (isEnabled && pen != null)
                    {
                        dc.DrawGeometry(null, pen, borderGeometry);
                    }

                    if (overlayPen != null)
                    {
                        dc.DrawGeometry(null, overlayPen, borderGeometry);
                    }
                }
            }
        }
    }

    private void DrawInnerBorder(DrawingContext dc, Rect bounds, DeviceSnapper snap)
    {
        if (!IsEffectivelyEnabled && !RoundCorners)
        {
            return;
        }

        if ((bounds.Width >= 4.0) && (bounds.Height >= 4.0))
        {
            if (InnerBorderPen(snap.Line) is { } innerBorder)
            {
                dc.DrawRectangle(null, innerBorder, snap.StrokeInside(bounds.Left, bounds.Top, bounds.Width, bounds.Height, 1), 1.75, 1.75);
            }
        }
    }

    private IBrush? BackgroundOverlay
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return _tokens.Brush(DisabledFill);
            }

            if (Local is { } local)
            {
                return ChromeBrush.With(_tokens.Brush(HoverFill), local.FillColors(), local.OverlayOpacity.Value);
            }

            if (RenderPressed)
            {
                return _tokens.Brush(PressedFill);
            }

            return RenderMouseOver ? _tokens.Brush(HoverFill) : null;
        }
    }

    private IPen? BorderOverlayPen(double thickness)
    {
        if (!IsEffectivelyEnabled)
        {
            return RoundCorners ? _tokens.Pen(DisabledBorder, thickness) : null;
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
            return _tokens.Pen(InnerBorder, thickness);
        }

        if (Local is { } local)
        {
            return ChromeBrush.Pen(ChromeBrush.With(_tokens.Brush(InnerBorder), [local.Inner0.Value, local.Inner1.Value], local.InnerOpacity.Value), thickness);
        }

        if (RenderPressed)
        {
            return null;
        }

        return RenderDefaulted ? _tokens.Pen(DefaultedInnerBorder, thickness) : _tokens.Pen(InnerBorder, thickness);
    }

    private IBrush? LeftDropShadowBrush => DropShadowBrush(PressedLeftShadow);

    private IBrush? TopDropShadowBrush => DropShadowBrush(PressedTopShadow);

    private IBrush? DropShadowBrush(string token)
    {
        if (!IsEffectivelyEnabled)
        {
            return null;
        }

        if (Local is { } local)
        {
            return ChromeBrush.WithOpacity(_tokens.Brush(token), local.ShadowOpacity.Value);
        }

        return RenderPressed ? _tokens.Brush(token) : null;
    }

    private void OnRenderDefaultedChanged(bool newValue)
    {
        if (Animates)
        {
            if (newValue)
            {
                var local = EnsureLocalResources();
                var color = _tokens.Color(DefaultedInnerBorderColor);
                local.Inner0.AnimateTo(color, ChromeTimings.HoverIn);
                local.Inner1.AnimateTo(color, ChromeTimings.HoverIn);

                if (!RenderPressed)
                {
                    StartDefaultPulse(local);
                }
            }
            else if (_localResources == null)
            {
                if (!RenderPressed)
                {
                    InvalidateVisual();
                }
            }
            else
            {
                var local = _localResources;
                if (!RenderPressed)
                {
                    local.OverlayOpacity.AnimateTo(0, ChromeTimings.HoverOut);
                }

                local.Inner0.AnimateTo(local.InnerBase[0], ChromeTimings.HoverOut);
                local.Inner1.AnimateTo(local.InnerBase[1], ChromeTimings.HoverOut);
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
            if (!RenderPressed)
            {
                if (newValue)
                {
                    EnsureLocalResources().OverlayOpacity.AnimateTo(1, ChromeTimings.HoverIn);
                }
                else if (_localResources == null)
                {
                    InvalidateVisual();
                }
                else if (RenderDefaulted)
                {
                    // The mouse was over the button, so the opacity should be 1; a quick pass may have left it lower, so
                    // the first key frame brings it to 1 before the pulse resumes:  \__/ \__/ \__/ \_...
                    var currentOpacity = _localResources.OverlayOpacity.Value;
                    var to1 = TimeSpan.FromSeconds((1.0 - currentOpacity) * ChromeTimings.DefaultPulseUp.TotalSeconds);
                    _localResources.OverlayOpacity.Animate(
                        [
                            new ChromeKeyFrame<double>(1.0, to1),
                            new ChromeKeyFrame<double>(1.0, to1 + ChromeTimings.DefaultPulseResumeHold, IsDiscrete: true),
                            new ChromeKeyFrame<double>(0.0, to1 + ChromeTimings.DefaultPulseResumeFall),
                            new ChromeKeyFrame<double>(currentOpacity, ChromeTimings.DefaultPulsePeriod),
                        ],
                        repeatForever: true);
                }
                else
                {
                    _localResources.OverlayOpacity.AnimateTo(0, ChromeTimings.HoverOut);
                }
            }
        }
        else
        {
            DropLocalResources();
            InvalidateVisual();
        }
    }

    private void OnRenderPressedChanged(bool newValue)
    {
        if (Animates)
        {
            var duration = ChromeTimings.Press;
            if (newValue)
            {
                var local = EnsureLocalResources();
                local.OverlayOpacity.AnimateTo(1, duration);
                local.ShadowOpacity.AnimateTo(1, duration);
                local.InnerOpacity.AnimateTo(0, duration);

                var top = _tokens.Color(PressedFill0Color);
                local.Fill[0].AnimateTo(top, duration);
                local.Fill[1].AnimateTo(top, duration);
                local.Fill[2].AnimateTo(_tokens.Color(PressedFill2Color), duration);
                local.Fill[3].AnimateTo(_tokens.Color(PressedFill3Color), duration);
                local.BorderColor.AnimateTo(_tokens.Color(PressedBorderColor), duration);
            }
            else if (_localResources == null)
            {
                InvalidateVisual();
            }
            else
            {
                var local = _localResources;
                local.ShadowOpacity.AnimateTo(0, duration);
                local.InnerOpacity.AnimateTo(1, duration);

                if (!RenderMouseOver)
                {
                    local.OverlayOpacity.AnimateTo(0, duration);
                }

                local.BorderColor.AnimateTo(local.BorderBase, duration);
                for (var i = 0; i < local.Fill.Length; i++)
                {
                    local.Fill[i].AnimateTo(local.FillBase[i], duration);
                }
            }
        }
        else
        {
            DropLocalResources();
            InvalidateVisual();
        }
    }

    // WPF: __/ \__/ \__/ \__... WPF's frame rate hint of 10 is ignored; the animator runs at the TopLevel frame rate.
    private static void StartDefaultPulse(LocalResources local) =>
        local.OverlayOpacity.Animate(
            [
                new ChromeKeyFrame<double>(1.0, ChromeTimings.DefaultPulseUp),
                new ChromeKeyFrame<double>(1.0, ChromeTimings.DefaultPulseHold, IsDiscrete: true),
                new ChromeKeyFrame<double>(0.0, ChromeTimings.DefaultPulsePeriod),
            ],
            repeatForever: true);

    // A default pulse set up outside a visual tree jumps to its end, so restart it once the chrome can animate.
    private void ResumeDefaultPulse()
    {
        if (!RenderDefaulted || RenderPressed || RenderMouseOver || !Animates || TopLevel.GetTopLevel(this) is null)
        {
            return;
        }

        var local = EnsureLocalResources();
        var color = _tokens.Color(DefaultedInnerBorderColor);
        local.Inner0.Set(color);
        local.Inner1.Set(color);
        StartDefaultPulse(local);
    }

    private void OnThemeResourcesChanged()
    {
        // The animation state holds colors of the old theme; drop it and resolve the tokens again.
        DropLocalResources();
        _tokens.Clear();
        InvalidateVisual();
        ResumeDefaultPulse();
    }

    private LocalResources EnsureLocalResources()
    {
        if (_localResources == null)
        {
            _localResources = new LocalResources(_animator, _tokens.Brush(HoverFill), _tokens.Brush(HoverBorder), _tokens.Brush(InnerBorder));
            InvalidateVisual();
        }

        return _localResources;
    }

    private void DropLocalResources()
    {
        _localResources?.Stop();
        _localResources = null;
    }

    /// <summary>The per-instance animated values, as WPF's per-instance brush clones.</summary>
    private sealed class LocalResources
    {
        public LocalResources(ChromeAnimator animator, IBrush? hoverFill, IBrush? hoverBorder, IBrush? innerBorder)
        {
            FillBase = ChromeBrush.StopColors(hoverFill, 4);
            BorderBase = ChromeBrush.StopColors(hoverBorder, 1)[0];
            InnerBase = ChromeBrush.StopColors(innerBorder, 2);

            // The fill and the border overlay always animate together in WPF, so they share one opacity.
            OverlayOpacity = animator.CreateDouble(0);
            ShadowOpacity = animator.CreateDouble(0);
            InnerOpacity = animator.CreateDouble(1);
            Fill = new AnimatedColor[FillBase.Length];
            for (var i = 0; i < Fill.Length; i++)
            {
                Fill[i] = animator.CreateColor(FillBase[i]);
            }

            BorderColor = animator.CreateColor(BorderBase);
            Inner0 = animator.CreateColor(InnerBase[0]);
            Inner1 = animator.CreateColor(InnerBase[1]);
        }

        public Color[] FillBase { get; }

        public Color BorderBase { get; }

        public Color[] InnerBase { get; }

        public AnimatedDouble OverlayOpacity { get; }

        public AnimatedDouble ShadowOpacity { get; }

        public AnimatedDouble InnerOpacity { get; }

        public AnimatedColor[] Fill { get; }

        public AnimatedColor BorderColor { get; }

        public AnimatedColor Inner0 { get; }

        public AnimatedColor Inner1 { get; }

        public Color[] FillColors() => [Fill[0].Value, Fill[1].Value, Fill[2].Value, Fill[3].Value];

        public void Stop()
        {
            OverlayOpacity.Stop();
            ShadowOpacity.Stop();
            InnerOpacity.Stop();
            foreach (var f in Fill)
            {
                f.Stop();
            }

            BorderColor.Stop();
            Inner0.Stop();
            Inner1.Stop();
        }
    }
}
