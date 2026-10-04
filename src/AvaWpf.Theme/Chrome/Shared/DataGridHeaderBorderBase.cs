// Ported from WPF $W/Themes/Shared/Microsoft/Windows/Themes/DataGridHeaderBorder.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaWpf.Chrome;

/// <summary>
/// The shared base of WPF's <c>DataGridHeaderBorder</c>; each family draws its look in <see cref="RenderTheme"/>.
/// When <see cref="Background"/> or <see cref="BorderBrush"/> is set, it draws as a plain border instead, as in WPF.
/// </summary>
/// <remarks>
/// The cache (<see cref="GetCachedResource"/>/<see cref="CacheResource"/>) is per instance because the brushes come from
/// the theme in effect; it is cleared when resources or the theme variant change.
/// </remarks>
public abstract class DataGridHeaderBorderBase : Decorator
{
    /// <summary>Defines the <see cref="IsHovered"/> property.</summary>
    public static readonly StyledProperty<bool> IsHoveredProperty =
        AvaloniaProperty.Register<DataGridHeaderBorderBase, bool>(nameof(IsHovered));

    /// <summary>Defines the <see cref="IsPressed"/> property.</summary>
    public static readonly StyledProperty<bool> IsPressedProperty =
        AvaloniaProperty.Register<DataGridHeaderBorderBase, bool>(nameof(IsPressed));

    /// <summary>Defines the <see cref="IsClickable"/> property.</summary>
    public static readonly StyledProperty<bool> IsClickableProperty =
        AvaloniaProperty.Register<DataGridHeaderBorderBase, bool>(nameof(IsClickable), true);

    /// <summary>Defines the <see cref="SortDirection"/> property.</summary>
    public static readonly StyledProperty<ListSortDirection?> SortDirectionProperty =
        AvaloniaProperty.Register<DataGridHeaderBorderBase, ListSortDirection?>(nameof(SortDirection));

    /// <summary>Defines the <see cref="IsSelected"/> property.</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<DataGridHeaderBorderBase, bool>(nameof(IsSelected));

    /// <summary>Defines the <see cref="Orientation"/> property.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<DataGridHeaderBorderBase, Orientation>(nameof(Orientation), Orientation.Vertical);

    /// <summary>Defines the <see cref="SeparatorBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> SeparatorBrushProperty =
        AvaloniaProperty.Register<DataGridHeaderBorderBase, IBrush?>(nameof(SeparatorBrush));

    /// <summary>Defines the <see cref="IsSeparatorVisible"/> property.</summary>
    public static readonly StyledProperty<bool> IsSeparatorVisibleProperty =
        AvaloniaProperty.Register<DataGridHeaderBorderBase, bool>(nameof(IsSeparatorVisible), true);

    /// <summary>Defines the <see cref="Background"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<DataGridHeaderBorderBase>();

    /// <summary>Defines the <see cref="BorderBrush"/> property.</summary>
    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<DataGridHeaderBorderBase>();

    /// <summary>Defines the <see cref="BorderThickness"/> property.</summary>
    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<DataGridHeaderBorderBase>();

    private readonly Dictionary<string, IBrush?> _systemBrushes = new(StringComparer.Ordinal);
    private object?[] _cache = [];
    private double _scale = 1.0;

    static DataGridHeaderBorderBase()
    {
        AffectsRender<DataGridHeaderBorderBase>(
            IsHoveredProperty, IsPressedProperty, IsClickableProperty, SortDirectionProperty, IsSelectedProperty,
            OrientationProperty, SeparatorBrushProperty, IsSeparatorVisibleProperty, BackgroundProperty,
            BorderBrushProperty, BorderThicknessProperty, InputElement.IsEffectivelyEnabledProperty);
        AffectsArrange<DataGridHeaderBorderBase>(IsPressedProperty, IsClickableProperty);
        AffectsMeasure<DataGridHeaderBorderBase>(
            BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, OrientationProperty, IsPressedProperty, IsClickableProperty);
    }

    /// <summary>Initializes a new instance of the <see cref="DataGridHeaderBorderBase"/> class.</summary>
    protected DataGridHeaderBorderBase()
    {
        ResourcesChanged += (_, _) => OnThemeResourcesChanged();
        ActualThemeVariantChanged += (_, _) => OnThemeResourcesChanged();
    }

    /// <summary>Whether the hover look is applied (only while <see cref="IsClickable"/>).</summary>
    public bool IsHovered
    {
        get => GetValue(IsHoveredProperty);
        set => SetValue(IsHoveredProperty, value);
    }

    /// <summary>Whether the pressed look is applied (only while <see cref="IsClickable"/>); it also offsets the child 1 px.</summary>
    public bool IsPressed
    {
        get => GetValue(IsPressedProperty);
        set => SetValue(IsPressedProperty, value);
    }

    /// <summary>When false, the hover and pressed looks are not applied even when <see cref="IsHovered"/> or <see cref="IsPressed"/> is true.</summary>
    public bool IsClickable
    {
        get => GetValue(IsClickableProperty);
        set => SetValue(IsClickableProperty, value);
    }

    /// <summary>The sort direction to show with an arrow, or null when not sorted.</summary>
    public ListSortDirection? SortDirection
    {
        get => GetValue(SortDirectionProperty);
        set => SetValue(SortDirectionProperty, value);
    }

    /// <summary>Whether the selected look is applied.</summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary><see cref="Orientation.Vertical"/> for a column header (the default), <see cref="Orientation.Horizontal"/> for a row header.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>The brush of the separator line between headers.</summary>
    public IBrush? SeparatorBrush
    {
        get => GetValue(SeparatorBrushProperty);
        set => SetValue(SeparatorBrushProperty, value);
    }

    /// <summary>Whether the separator between headers is drawn (WPF's <c>SeparatorVisibility == Visible</c>). Default true.</summary>
    public bool IsSeparatorVisible
    {
        get => GetValue(IsSeparatorVisibleProperty);
        set => SetValue(IsSeparatorVisibleProperty, value);
    }

    /// <summary>When set (with or without <see cref="BorderBrush"/>), the element draws as a plain border filled with this brush.</summary>
    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>When set (with or without <see cref="Background"/>), the element draws as a plain border with this brush.</summary>
    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    /// <summary>The border thickness used when the element draws as a plain border.</summary>
    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    /// <summary>True when <see cref="Background"/> or <see cref="BorderBrush"/> is set and the plain border is used.</summary>
    protected bool UsingBorderImplementation => Background != null || BorderBrush != null;

    /// <summary>
    /// The family's padding around the child, or null for the shared default: 3 px on each side with 15 px on the right
    /// of a column header (room for the sort arrow).
    /// </summary>
    protected virtual Thickness? ThemeDefaultPadding => null;

    /// <summary>The device pixels per DIP at the last render (1.0 outside a visual tree).</summary>
    protected double RenderScale => _scale;

    /// <summary>
    /// The padding used when <see cref="Decorator.Padding"/> is zero: <see cref="ThemeDefaultPadding"/> (or the shared
    /// default), shifted 1 px right and down while pressed and clickable.
    /// </summary>
    protected Thickness DefaultPadding
    {
        get
        {
            var themePadding = ThemeDefaultPadding;
            var padding = themePadding ?? (Orientation == Orientation.Vertical ? new Thickness(3, 3, 15, 3) : new Thickness(3.0));

            if (IsPressed && IsClickable)
            {
                padding = new Thickness(padding.Left + 1.0, padding.Top + 1.0, padding.Right - 1.0, padding.Bottom - 1.0);
            }

            return padding;
        }
    }

    /// <summary>Draws the theme look, or the plain border while <see cref="UsingBorderImplementation"/>.</summary>
    /// <param name="context">The drawing context.</param>
    public sealed override void Render(DrawingContext context)
    {
        _scale = PixelSnap.Scale(this);
        if (UsingBorderImplementation)
        {
            RenderBorder(context);
        }
        else
        {
            RenderTheme(context);
        }
    }

    /// <summary>Draws the family's header look over the whole render size.</summary>
    /// <param name="dc">The drawing context.</param>
    protected abstract void RenderTheme(DrawingContext dc);

    /// <summary>
    /// Measures the child inside the padding (<see cref="Decorator.Padding"/> when non-zero, else
    /// <see cref="DefaultPadding"/>); a finite constraint is reduced by the padding, clamped at zero.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (UsingBorderImplementation)
        {
            return LayoutHelper.MeasureChild(Child, availableSize, Padding, BorderThickness);
        }

        var child = Child;
        if (child != null)
        {
            var padding = EffectivePadding();

            var childWidth = availableSize.Width;
            var childHeight = availableSize.Height;

            if (!double.IsInfinity(childWidth))
            {
                childWidth = Math.Max(0.0, childWidth - padding.Left - padding.Right);
            }

            if (!double.IsInfinity(childHeight))
            {
                childHeight = Math.Max(0.0, childHeight - padding.Top - padding.Bottom);
            }

            child.Measure(new Size(childWidth, childHeight));
            var desiredSize = child.DesiredSize;

            return new Size(desiredSize.Width + padding.Left + padding.Right, desiredSize.Height + padding.Top + padding.Bottom);
        }

        return default;
    }

    /// <summary>Arranges the child inside the padding used by <see cref="MeasureOverride"/>.</summary>
    /// <param name="finalSize">The final size.</param>
    /// <returns>The size used.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (UsingBorderImplementation)
        {
            return LayoutHelper.ArrangeChild(Child, finalSize, Padding, BorderThickness);
        }

        var child = Child;
        if (child != null)
        {
            var padding = EffectivePadding();
            var childWidth = Math.Max(0.0, finalSize.Width - padding.Left - padding.Right);
            var childHeight = Math.Max(0.0, finalSize.Height - padding.Top - padding.Bottom);
            child.Arrange(new Rect(padding.Left, padding.Top, childWidth, childHeight));
        }

        return finalSize;
    }

    /// <summary>Clamps <paramref name="d"/> at zero (WPF's <c>Max0</c>).</summary>
    /// <param name="d">The value.</param>
    /// <returns>The value, or 0 when negative.</returns>
    protected static double Max0(double d) => Math.Max(0.0, d);

    /// <summary>Snaps the edges of <paramref name="rect"/> to device pixels at the current <see cref="RenderScale"/>.</summary>
    /// <param name="rect">The rectangle in DIPs.</param>
    /// <returns>The snapped rectangle.</returns>
    protected Rect Snap(Rect rect) => PixelSnap.Rect(rect, _scale);

    /// <summary>Fills <paramref name="rect"/> with its edges snapped to device pixels, as WPF's <c>SnapsToDevicePixels</c>.</summary>
    /// <param name="dc">The drawing context.</param>
    /// <param name="brush">The brush, or null to draw nothing.</param>
    /// <param name="rect">The rectangle in DIPs.</param>
    protected void FillRect(DrawingContext dc, IBrush? brush, Rect rect)
    {
        if (brush != null)
        {
            dc.DrawRectangle(brush, null, Snap(rect));
        }
    }

    /// <summary>Looks up a <c>SystemColors</c> brush by its resource key (for example <c>SystemColors.ControlDarkBrush</c>).</summary>
    /// <param name="key">The full resource key.</param>
    /// <returns>The brush, or null when the theme does not define it.</returns>
    /// <remarks>The result is cached per instance until resources or the theme variant change.</remarks>
    protected IBrush? SystemBrush(string key)
    {
        if (_systemBrushes.TryGetValue(key, out var cached))
        {
            return cached;
        }

        IBrush? brush = ChromeResources.Resource(this, key) switch
        {
            IBrush b => b,
            Color c => new ImmutableSolidColorBrush(c),
            _ => null,
        };
        _systemBrushes[key] = brush;
        return brush;
    }

    /// <summary>Looks up a chrome brush token of the family in effect (see <see cref="ChromeResources.Brush"/>).</summary>
    /// <param name="name">The token name relative to <c>&lt;Family&gt;.Chrome.</c>.</param>
    /// <returns>The brush, or null when the family does not define it.</returns>
    protected IBrush? ChromeBrush(string name) => ChromeResources.Brush(this, name);

    /// <summary>
    /// True while the <c>SystemParameters.HighContrast</c> resource is true. WPF draws the sort arrow in the control text
    /// color instead of the gray text color then.
    /// </summary>
    protected bool IsHighContrast => ChromeResources.Resource(this, "SystemParameters.HighContrast") is true;

    /// <summary>Retrieves a cached brush, pen or geometry stored with <see cref="CacheResource"/>, or null.</summary>
    /// <param name="index">The derived class's cache slot.</param>
    /// <returns>The cached object, or null.</returns>
    protected object? GetCachedResource(int index) => index >= 0 && index < _cache.Length ? _cache[index] : null;

    /// <summary>Caches a brush, pen or geometry until resources or the theme variant change.</summary>
    /// <param name="index">The derived class's cache slot.</param>
    /// <param name="value">The object to cache.</param>
    protected void CacheResource(int index, object value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= _cache.Length)
        {
            Array.Resize(ref _cache, index + 1);
        }

        _cache[index] = value;
    }

    /// <summary>Releases every cached resource (WPF's <c>ReleaseCache</c>, for example after a Luna color scheme change).</summary>
    protected void ReleaseCache()
    {
        Array.Clear(_cache);
        _systemBrushes.Clear();
    }

    /// <summary>
    /// Called when resources or the theme variant change. The default releases the cache and redraws; overrides must
    /// call the base.
    /// </summary>
    protected virtual void OnThemeResourcesChanged()
    {
        ReleaseCache();
        InvalidateVisual();
    }

    private Thickness EffectivePadding()
    {
        // The public Padding is used when it is set (non-zero).
        var padding = Padding;
        return padding.Equals(default(Thickness)) ? DefaultPadding : padding;
    }

    // WPF's Border.OnRender for square corners: the border as four edge strips, the background inside the border.
    private void RenderBorder(DrawingContext dc)
    {
        var bounds = new Rect(Bounds.Size);
        var border = BorderThickness;
        var borderBrush = BorderBrush;

        if (borderBrush != null && (border.Left > 0 || border.Top > 0 || border.Right > 0 || border.Bottom > 0))
        {
            FillRect(dc, borderBrush, new Rect(bounds.Left, bounds.Top, bounds.Width, border.Top));
            FillRect(dc, borderBrush, new Rect(bounds.Left, bounds.Bottom - border.Bottom, bounds.Width, border.Bottom));
            FillRect(dc, borderBrush, new Rect(bounds.Left, bounds.Top + border.Top, border.Left, Max0(bounds.Height - border.Top - border.Bottom)));
            FillRect(dc, borderBrush, new Rect(bounds.Right - border.Right, bounds.Top + border.Top, border.Right, Max0(bounds.Height - border.Top - border.Bottom)));
        }

        var background = Background;
        if (background != null)
        {
            var inner = PixelSnap.Inset(bounds, border, _scale);
            if (inner.Width > 0 && inner.Height > 0)
            {
                dc.DrawRectangle(background, null, inner);
            }
        }
    }
}
