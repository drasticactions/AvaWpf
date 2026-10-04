// Ported from WPF $W/Themes/PresentationFramework.Aero/Microsoft/Windows/Themes/ListBoxChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaWpf.Animations;

namespace AvaWpf.Chrome.Aero;

/// <summary>The Aero list box and text box border (WPF <c>Microsoft.Windows.Themes.ListBoxChrome</c>).</summary>
public sealed class ListBoxChrome : Decorator
{
    /// <summary>Defines the <see cref="Background"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<ListBoxChrome>();

    /// <summary>Defines the <see cref="BorderBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<ListBoxChrome>();

    /// <summary>Defines the <see cref="BorderThickness"/> property.</summary>
    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<ListBoxChrome>(new StyledPropertyMetadata<Thickness>(new Thickness(1)));

    /// <summary>Defines the <see cref="RenderMouseOver"/> property.</summary>
    public static readonly StyledProperty<bool> RenderMouseOverProperty =
        AvaloniaProperty.Register<ListBoxChrome, bool>(nameof(RenderMouseOver));

    /// <summary>Defines the <see cref="RenderFocused"/> property.</summary>
    public static readonly StyledProperty<bool> RenderFocusedProperty =
        AvaloniaProperty.Register<ListBoxChrome, bool>(nameof(RenderFocused));

    private const string DisabledBackground = "ListBoxChrome.Disabled.Background";
    private const string HoverBorder = "ListBoxChrome.Hover.Border";
    private const string FocusedBorder = "ListBoxChrome.Focused.Border";
    private const string DisabledBorder = "ListBoxChrome.Disabled.Border";

    private readonly ChromeAnimator _animator;
    private readonly ChromeTokens _tokens;
    private AnimatedDouble? _borderOverlayOpacity;

    static ListBoxChrome()
    {
        AffectsRender<ListBoxChrome>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, IsEffectivelyEnabledProperty);
        AffectsMeasure<ListBoxChrome>(BorderThicknessProperty);
    }

    /// <summary>Initializes a new instance of the <see cref="ListBoxChrome"/> class.</summary>
    public ListBoxChrome()
    {
        _animator = new ChromeAnimator(this);
        _tokens = new ChromeTokens(this);
        ResourcesChanged += (_, _) => OnThemeResourcesChanged();
        ActualThemeVariantChanged += (_, _) => OnThemeResourcesChanged();
    }

    /// <summary>The chrome token names this class reads, without the <c>&lt;Family&gt;.Chrome.</c> prefix.</summary>
    public static IReadOnlyList<string> TokenSet { get; } = [DisabledBackground, HoverBorder, FocusedBorder, DisabledBorder];

    /// <summary>The brush that fills the inside of the border.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>The brush of the border.</summary>
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    /// <summary>The thickness of the border. Default 1; 0 draws no border and no inner gap.</summary>
    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>When true, the chrome draws the mouse-over look.</summary>
    public bool RenderMouseOver
    {
        get => GetValue(RenderMouseOverProperty);
        set => SetValue(RenderMouseOverProperty, value);
    }

    /// <summary>When true, the chrome draws the focused look, which takes precedence over the mouse-over look.</summary>
    public bool RenderFocused
    {
        get => GetValue(RenderFocusedProperty);
        set => SetValue(RenderFocusedProperty, value);
    }

    // As WPF: animate only when client-area animation is on and the chrome is enabled.
    private bool Animates => IsEffectivelyEnabled && WpfAnimations.IsMotionEnabled(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == RenderMouseOverProperty)
        {
            OnRenderMouseOverChanged(change.GetNewValue<bool>());
        }
        else if (change.Property == RenderFocusedProperty)
        {
            DropLocalResources();
            InvalidateVisual();
        }
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        DropLocalResources();
    }

    /// <summary>Measures the child inside the border and, when there is a border, a further 1 px on every side.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size desired;
        var border = BorderThickness;
        var borderX = border.Left + border.Right;
        var borderY = border.Top + border.Bottom;

        // When BorderThickness is 0, draw no border; otherwise add 1 on each side for the inner border.
        if (borderX > 0 || borderY > 0)
        {
            borderX += 2.0;
            borderY += 2.0;
        }

        var child = Child;
        if (child != null)
        {
            var isWidthTooSmall = availableSize.Width < borderX;
            var isHeightTooSmall = availableSize.Height < borderY;
            var childConstraint = new Size(isWidthTooSmall ? 0 : availableSize.Width - borderX, isHeightTooSmall ? 0 : availableSize.Height - borderY);

            child.Measure(childConstraint);
            desired = child.DesiredSize;
            desired = new Size(desired.Width + (isWidthTooSmall ? 0 : borderX), desired.Height + (isHeightTooSmall ? 0 : borderY));
        }
        else
        {
            desired = new Size(Math.Min(borderX, availableSize.Width), Math.Min(borderY, availableSize.Height));
        }

        return desired;
    }

    /// <summary>Arranges the child inside the border and the 1 px inner gap.</summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var childArrangeRect = default(Rect);
        var border = BorderThickness;
        var left = border.Left;
        var top = border.Top;
        var borderX = border.Left + border.Right;
        var borderY = border.Top + border.Bottom;

        // When BorderThickness is 0, draw no border; otherwise add 1 on each side for the inner border.
        if (borderX > 0 || borderY > 0)
        {
            left += 1.0;
            top += 1.0;
            borderX += 2.0;
            borderY += 2.0;
        }

        if ((finalSize.Width > borderX) && (finalSize.Height > borderY))
        {
            childArrangeRect = new Rect(left, top, finalSize.Width - borderX, finalSize.Height - borderY);
        }

        Child?.Arrange(childArrangeRect);
        return finalSize;
    }

    /// <summary>Draws the background, the disabled overlay and the border with its state overlay.</summary>
    public override void Render(DrawingContext context)
    {
        var dc = context;
        var bounds = new Rect(Bounds.Size);
        var snap = DeviceSnapper.For(this);
        var border = BorderThickness;
        var borderX = border.Left + border.Right;
        var borderY = border.Top + border.Bottom;

        // The disabled overlay is inset 1 px inside the border.
        var isSimpleBorder = border.Left == 1.0 && border.Right == 1.0 && border.Top == 1.0 && border.Bottom == 1.0;
        var innerBorderThickness = (borderX == 0.0 && borderY == 0.0) ? 0.0 : 1.0;
        var innerBorderThickness2 = 2 * innerBorderThickness;

        if ((bounds.Width > borderX) && (bounds.Height > borderY))
        {
            var fill = Background;
            if (fill != null)
            {
                dc.DrawRectangle(fill, null, snap.Fill(bounds.Left + border.Left, bounds.Top + border.Top, bounds.Width - borderX, bounds.Height - borderY));
            }

            if ((bounds.Width > borderX + innerBorderThickness2) && (bounds.Height > borderY + innerBorderThickness2))
            {
                if (BackgroundOverlay is { } overlay)
                {
                    var backgroundRect = snap.Fill(
                        bounds.Left + border.Left + innerBorderThickness,
                        bounds.Top + border.Top + innerBorderThickness,
                        bounds.Width - borderX - innerBorderThickness2,
                        bounds.Height - borderY - innerBorderThickness2);
                    dc.DrawRectangle(overlay, null, backgroundRect, 1, 1);
                }
            }
        }

        // innerBorderThickness is 0 when the border is 0.
        if (innerBorderThickness > 0 && (bounds.Width >= borderX) && (bounds.Height >= borderY))
        {
            if (isSimpleBorder)
            {
                var rect = snap.Stroke(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                var pen = _tokens.Pen(BorderBrush, snap.Line);
                var overlayPen = BorderOverlayPen(snap.Line);

                if (pen != null)
                {
                    dc.DrawRectangle(null, pen, rect, 1.0, 1.0);
                }

                if (overlayPen != null)
                {
                    dc.DrawRectangle(null, overlayPen, rect, 1.0, 1.0);
                }
            }
            else if (BorderBrush is { } borderBrush)
            {
                dc.DrawGeometry(borderBrush, null, GetBorderGeometry(border, bounds, snap));
            }
        }
    }

    private static Geometry GetBorderGeometry(Thickness thickness, Rect bounds, DeviceSnapper snap)
    {
        var outer = snap.Fill(bounds);
        var inner = snap.Fill(
            bounds.Left + thickness.Left,
            bounds.Top + thickness.Top,
            Math.Max(0, bounds.Width - thickness.Left - thickness.Right),
            Math.Max(0, bounds.Height - thickness.Top - thickness.Bottom));

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            // WPF's PathGeometry fills even-odd: the inner figure is the hole.
            ctx.SetFillRule(FillRule.EvenOdd);
            ctx.BeginFigure(outer.TopLeft, true);
            ctx.LineTo(outer.BottomLeft);
            ctx.LineTo(outer.BottomRight);
            ctx.LineTo(outer.TopRight);
            ctx.EndFigure(true);
            ctx.BeginFigure(inner.TopLeft, true);
            ctx.LineTo(inner.BottomLeft);
            ctx.LineTo(inner.BottomRight);
            ctx.LineTo(inner.TopRight);
            ctx.EndFigure(true);
        }

        return geometry;
    }

    private IBrush? BackgroundOverlay => !IsEffectivelyEnabled ? _tokens.Brush(DisabledBackground) : null;

    private IPen? BorderOverlayPen(double thickness)
    {
        if (!IsEffectivelyEnabled)
        {
            return _tokens.Pen(DisabledBorder, thickness);
        }

        if (Animates && _borderOverlayOpacity is { } opacity)
        {
            return ChromeBrush.Pen(ChromeBrush.WithOpacity(_tokens.Brush(HoverBorder), opacity.Value), thickness);
        }

        if (RenderFocused)
        {
            return _tokens.Pen(FocusedBorder, thickness);
        }

        return RenderMouseOver ? _tokens.Pen(HoverBorder, thickness) : null;
    }

    private void OnRenderMouseOverChanged(bool newValue)
    {
        if (RenderFocused)
        {
            return;
        }

        if (Animates)
        {
            if (newValue)
            {
                if (_borderOverlayOpacity == null)
                {
                    // WPF's per-instance clone of the hover border overlay, at opacity 0.
                    _borderOverlayOpacity = _animator.CreateDouble(0);
                    InvalidateVisual();
                }

                _borderOverlayOpacity.AnimateTo(1, ChromeTimings.HoverIn);
            }
            else if (_borderOverlayOpacity == null)
            {
                InvalidateVisual();
            }
            else
            {
                _borderOverlayOpacity.AnimateTo(0, ChromeTimings.HoverOut);
            }
        }
        else
        {
            DropLocalResources();
            InvalidateVisual();
        }
    }

    private void OnThemeResourcesChanged()
    {
        DropLocalResources();
        _tokens.Clear();
        InvalidateVisual();
    }

    private void DropLocalResources()
    {
        _borderOverlayOpacity?.Stop();
        _borderOverlayOpacity = null;
    }
}
