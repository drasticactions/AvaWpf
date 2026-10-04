// Ported from WPF $W/Themes/PresentationFramework.Royale/Microsoft/Windows/Themes/ButtonChrome.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWpf.Chrome.Royale;

/// <summary>The Royale button face, as WPF's Royale <c>ButtonChrome</c>; the child is inset 4 px on each side.</summary>
public sealed class ButtonChrome : Decorator
{
    /// <summary>Defines the <see cref="Fill"/> property.</summary>
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ButtonChrome, IBrush?>(nameof(Fill));

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

    private const double SideThickness = 4.0;
    private const double SideThickness2 = 2 * SideThickness;

    private readonly ChromeBrushCache _cache;

    static ButtonChrome()
    {
        AffectsRender<ButtonChrome>(
            FillProperty, BorderBrushProperty, RenderDefaultedProperty, RenderMouseOverProperty, RenderPressedProperty,
            IsEffectivelyEnabledProperty);
    }

    /// <summary>Creates a ButtonChrome.</summary>
    public ButtonChrome()
    {
        _cache = new ChromeBrushCache(this);
        ResourcesChanged += (_, _) => InvalidateCache();
        ActualThemeVariantChanged += (_, _) => InvalidateCache();
    }

    /// <summary>The token names this chrome reads, without the <c>&lt;Family&gt;.Chrome.</c> prefix.</summary>
    public static IReadOnlyList<string> TokenSet { get; } =
    [
        "ButtonChrome.DisabledBorder", "ButtonChrome.DefaultedInnerHighlight", "ButtonChrome.HoverInnerHighlight",
        "ButtonChrome.BottomShade", "ButtonChrome.PressedBottomShade", "ButtonChrome.RightShade",
        "ButtonChrome.PressedTopShade", "ButtonChrome.LeftShade", "ButtonChrome.PressedLeftShade",
        "ButtonChrome.DisabledFill", "ButtonChrome.PressedFill",
    ];

    /// <summary>The brush of the button face in the normal state.</summary>
    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>The brush of the 1 px border while enabled.</summary>
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    /// <summary>When true the chrome renders with a defaulted look.</summary>
    public bool RenderDefaulted
    {
        get => GetValue(RenderDefaultedProperty);
        set => SetValue(RenderDefaultedProperty, value);
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

    private IPen? BorderPen =>
        !IsEffectivelyEnabled ? _cache.Pen("ButtonChrome.DisabledBorder", 1.0) : _cache.UserPen(BorderBrush, 1.0);

    private IPen? InnerHighlight
    {
        get
        {
            if (!IsEffectivelyEnabled || RenderPressed)
            {
                return null;
            }

            if (RenderMouseOver)
            {
                return _cache.Pen("ButtonChrome.HoverInnerHighlight", 2.6666666667);
            }

            if (RenderDefaulted)
            {
                return _cache.Pen("ButtonChrome.DefaultedInnerHighlight", 2.6666666667);
            }

            return null;
        }
    }

    private IBrush? BottomShade
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return null;
            }

            return _cache.Brush(RenderPressed ? "ButtonChrome.PressedBottomShade" : "ButtonChrome.BottomShade");
        }
    }

    private IBrush? RightShade =>
        !IsEffectivelyEnabled || RenderPressed ? null : _cache.Brush("ButtonChrome.RightShade");

    private IBrush? TopShade =>
        IsEffectivelyEnabled && RenderPressed ? _cache.Brush("ButtonChrome.PressedTopShade") : null;

    private IBrush? LeftShade
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return null;
            }

            return _cache.Brush(RenderPressed ? "ButtonChrome.PressedLeftShade" : "ButtonChrome.LeftShade");
        }
    }

    private IBrush? Background
    {
        get
        {
            if (!IsEffectivelyEnabled)
            {
                return _cache.Brush("ButtonChrome.DisabledFill");
            }

            if (RenderPressed)
            {
                return _cache.Brush("ButtonChrome.PressedFill");
            }

            return Fill;
        }
    }

    /// <summary>Inflates the desired size of the child by 4 on all four sides.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size desired;
        var child = Child;
        if (child is not null)
        {
            var isWidthTooSmall = availableSize.Width < SideThickness2;
            var isHeightTooSmall = availableSize.Height < SideThickness2;
            var childConstraint = new Size(
                isWidthTooSmall ? 0.0 : availableSize.Width - SideThickness2,
                isHeightTooSmall ? 0.0 : availableSize.Height - SideThickness2);

            child.Measure(childConstraint);
            desired = child.DesiredSize;

            if (!isWidthTooSmall)
            {
                desired = desired.WithWidth(desired.Width + SideThickness2);
            }

            if (!isHeightTooSmall)
            {
                desired = desired.WithHeight(desired.Height + SideThickness2);
            }
        }
        else
        {
            desired = new Size(Math.Min(SideThickness2, availableSize.Width), Math.Min(SideThickness2, availableSize.Height));
        }

        return desired;
    }

    /// <summary>Arranges the child centered, 4 inside each side.</summary>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(0d, finalSize.Width - SideThickness2);
        var height = Math.Max(0d, finalSize.Height - SideThickness2);
        var childArrangeRect = new Rect((finalSize.Width - width) * 0.5, (finalSize.Height - height) * 0.5, width, height);

        Child?.Arrange(childArrangeRect);
        return finalSize;
    }

    /// <summary>Draws the chrome. Assumes the inner highlight is 2⅔ px thick.</summary>
    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);

        if (DrawBackground(context, ref bounds))
        {
            return;
        }

        DrawShades(context, ref bounds);
        DrawInnerHighlight(context, ref bounds);
        DrawBorder(context, ref bounds);
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InvalidateCache();
    }

    private bool DrawBackground(DrawingContext dc, ref Rect bounds)
    {
        var brush = Background;
        if (brush is not null)
        {
            dc.DrawRectangle(brush, null, bounds, 3.0, 3.0);
        }

        return (bounds.Width < 0.6) || (bounds.Height < 0.6);
    }

    private void DrawShades(DrawingContext dc, ref Rect bounds)
    {
        // Shades are inset an additional 0.3.
        var b = bounds.Deflate(0.3);

        var brush = TopShade;
        if (brush is not null)
        {
            dc.DrawRectangle(brush, null, new Rect(b.Left, b.Top, b.Width, 6.0), 3.0, 3.0);
        }

        brush = BottomShade;
        if (brush is not null)
        {
            dc.DrawRectangle(brush, null, new Rect(b.Left, b.Bottom - 6.0, b.Width, 6.0), 3.0, 3.0);
        }

        brush = LeftShade;
        if (brush is not null)
        {
            dc.DrawRectangle(brush, null, new Rect(b.Left, b.Top, 6.0, b.Height), 3.0, 3.0);
        }

        brush = RightShade;
        if (brush is not null)
        {
            dc.DrawRectangle(brush, null, new Rect(b.Right - 6.0, b.Top, 6.0, b.Height), 3.0, 3.0);
        }
    }

    private void DrawInnerHighlight(DrawingContext dc, ref Rect bounds)
    {
        var pen = InnerHighlight;
        if (pen is not null && (bounds.Width >= (8.0 / 3.0)) && (bounds.Height >= (8.0 / 3.0)))
        {
            dc.DrawRectangle(
                null,
                pen,
                new Rect(bounds.Left + 4.0 / 3.0, bounds.Top + 4.0 / 3.0, bounds.Width - 8.0 / 3.0, bounds.Height - 8.0 / 3.0),
                2.0,
                2.0);
        }
    }

    private void DrawBorder(DrawingContext dc, ref Rect bounds)
    {
        var borderPen = BorderPen;
        if ((borderPen is not null) && (bounds.Width >= 1.0) && (bounds.Height >= 1.0))
        {
            dc.DrawRectangle(null, borderPen, new Rect(bounds.Left + 0.5, bounds.Top + 0.5, bounds.Width - 1.0, bounds.Height - 1.0), 2.0, 2.0);
        }
    }

    private void InvalidateCache()
    {
        _cache.Clear();
        InvalidateVisual();
    }
}
