// Ported from WPF $W/Themes/PresentationFramework.Royale/Microsoft/Windows/Themes/DataGridHeaderBorder.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaWpf.Chrome.Royale;

/// <summary>The Royale DataGrid column and row header, as WPF's Royale <c>DataGridHeaderBorder</c>.</summary>
public sealed class DataGridHeaderBorder : DataGridHeaderBorderBase
{
    private static readonly Geometry s_arrowUpGeometry = ArrowGeometry(ascending: true);
    private static readonly Geometry s_arrowDownGeometry = ArrowGeometry(ascending: false);

    private readonly ChromeBrushCache _cache;

    /// <summary>Creates the border.</summary>
    public DataGridHeaderBorder() => _cache = new ChromeBrushCache(this);

    /// <summary>The token names this chrome reads, without the <c>&lt;Family&gt;.Chrome.</c> prefix.</summary>
    public static IReadOnlyList<string> TokenSet { get; } = BuildTokenSet();

    /// <inheritdoc/>
    protected override void OnThemeResourcesChanged()
    {
        _cache.Clear();
        base.OnThemeResourcesChanged();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        OnThemeResourcesChanged();
    }

    /// <inheritdoc/>
    protected override void RenderTheme(DrawingContext dc)
    {
        var size = Bounds.Size;
        var horizontal = Orientation == Orientation.Horizontal;
        var isClickable = IsClickable && IsEffectivelyEnabled;
        var isHovered = isClickable && IsHovered;
        var isPressed = isClickable && IsPressed;
        var sortDirection = SortDirection;
        var isSorted = sortDirection is not null;
        var isSelected = IsSelected;
        var scale = RenderScale;

        using DrawingContext.PushedState? rotate = horizontal
            ? dc.PushTransform(Matrix.CreateRotation(-System.Math.PI / 2.0) * Matrix.CreateTranslation(0.0, size.Height))
            : null;
        if (horizontal)
        {
            // When horizontal, rotate the rendering by -90 degrees.
            size = new Size(size.Height, size.Width);
        }

        var background = _cache.Brush(
            isPressed ? "DataGridHeaderBorder.PressedBackground" : isHovered || isSelected ? "DataGridHeaderBorder.HoveredBackground" : "DataGridHeaderBorder.NormalBackground");
        dc.DrawRectangle(background, null, Snap(new Rect(0.0, 0.0, size.Width, size.Height)));

        if (isHovered && !isPressed && (size.Width >= 6.0) && (size.Height >= 4.0))
        {
            // When hovered, there is a colored tab at the bottom.
            using (dc.PushTransform(Matrix.CreateTranslation(0.0, PixelSnap.Round(size.Height - 3.0, scale))))
            {
                var tabGeometry = new StreamGeometry();
                using (var ctx = tabGeometry.Open())
                {
                    ctx.BeginFigure(new Point(0.5, 0.5), true);
                    ctx.LineTo(new Point(size.Width - 0.5, 0.5));
                    ctx.ArcTo(new Point(size.Width - 2.5, 2.5), new Size(2.0, 2.0), 90.0, false, SweepDirection.Clockwise);
                    ctx.LineTo(new Point(2.5, 2.5));
                    ctx.ArcTo(new Point(0.5, 0.5), new Size(2.0, 2.0), 90.0, false, SweepDirection.Clockwise);
                    ctx.EndFigure(true);
                }

                dc.DrawGeometry(_cache.Brush("DataGridHeaderBorder.TabFill"), _cache.Pen("DataGridHeaderBorder.TabStroke"), tabGeometry);
            }
        }

        if (isPressed && (size.Width >= 2.0) && (size.Height >= 2.0))
        {
            // When pressed, there is a border on the left and bottom.
            var border = _cache.Brush("DataGridHeaderBorder.PressedBorder");
            dc.DrawRectangle(border, null, Snap(new Rect(0.0, 0.0, 1.0, size.Height)));
            dc.DrawRectangle(border, null, Snap(new Rect(0.0, Max0(size.Height - 1.0), size.Width, 1.0)));
        }

        if (!isPressed && !isHovered && (size.Width >= 4.0) && IsSeparatorVisible)
        {
            // When not pressed or hovered, draw the resize gripper.
            var sideBrush = SeparatorBrush ?? _cache.Brush(horizontal ? "DataGridHeaderBorder.HorizontalGripper" : "DataGridHeaderBorder.VerticalGripper");
            dc.DrawRectangle(sideBrush, null, Snap(new Rect(horizontal ? 0.0 : Max0(size.Width - 2.0), 4.0, 2.0, Max0(size.Height - 8.0))));
        }

        if (isSorted && (size.Width > 14.0) && (size.Height > 10.0))
        {
            // When sorted, draw an arrow on the right.
            var offset = Matrix.CreateTranslation(PixelSnap.Round(size.Width - 15.0, scale), PixelSnap.Round((size.Height - 5.0) * 0.5, scale));
            using (dc.PushTransform(offset))
            {
                var ascending = sortDirection == ListSortDirection.Ascending;
                dc.DrawGeometry(_cache.Brush("DataGridHeaderBorder.ArrowFill"), null, ascending ? s_arrowUpGeometry : s_arrowDownGeometry);
            }
        }
    }

    private static Geometry ArrowGeometry(bool ascending)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        if (ascending)
        {
            ctx.BeginFigure(new Point(0.0, 5.0), true);
            ctx.LineTo(new Point(5.0, 0.0));
            ctx.LineTo(new Point(10.0, 5.0));
        }
        else
        {
            ctx.BeginFigure(new Point(0.0, 0.0), true);
            ctx.LineTo(new Point(10.0, 0.0));
            ctx.LineTo(new Point(5.0, 5.0));
        }

        ctx.EndFigure(true);
        return geometry;
    }

    private static string[] BuildTokenSet() =>
    [
        "DataGridHeaderBorder.NormalBackground", "DataGridHeaderBorder.HoveredBackground", "DataGridHeaderBorder.PressedBackground",
        "DataGridHeaderBorder.TabStroke", "DataGridHeaderBorder.TabFill", "DataGridHeaderBorder.PressedBorder",
        "DataGridHeaderBorder.HorizontalGripper", "DataGridHeaderBorder.VerticalGripper", "DataGridHeaderBorder.ArrowFill",
    ];
}
