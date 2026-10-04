// Ported from WPF $W/Themes/PresentationFramework.AeroLite/Microsoft/Windows/Themes/DataGridHeaderBorder.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaWpf.Chrome.AeroLite;

/// <summary>The AeroLite DataGrid header look (WPF <c>Microsoft.Windows.Themes.DataGridHeaderBorder</c>).</summary>
public sealed class DataGridHeaderBorder : DataGridHeaderBorderBase
{
    private const string NormalBackground = "DataGridHeaderBorder.Normal.Background";
    private const string PressedBackground = "DataGridHeaderBorder.Pressed.Background";
    private const string HoveredBackground = "DataGridHeaderBorder.Hovered.Background";
    private const string NormalSides = "DataGridHeaderBorder.Normal.Sides";
    private const string PressedSides = "DataGridHeaderBorder.Pressed.Sides";
    private const string HoveredSides = "DataGridHeaderBorder.Hovered.Sides";
    private const string ArrowFill = "DataGridHeaderBorder.Arrow.Fill";

    private static Geometry? s_arrowUpGeometry;
    private static Geometry? s_arrowDownGeometry;

    /// <summary>The chrome token names this class reads, without the <c>&lt;Family&gt;.Chrome.</c> prefix.</summary>
    public static IReadOnlyList<string> TokenSet { get; } =
    [
        NormalBackground, PressedBackground, HoveredBackground, NormalSides, PressedSides, HoveredSides, ArrowFill,
    ];

    /// <summary>A column header pads its child 5, 4, 5, 4; a row header uses the shared default.</summary>
    protected override Thickness? ThemeDefaultPadding =>
        Orientation == Orientation.Vertical ? new Thickness(5.0, 4.0, 5.0, 4.0) : null;

    /// <summary>Draws the AeroLite header look.</summary>
    /// <param name="dc">The drawing context.</param>
    protected override void RenderTheme(DrawingContext dc)
    {
        var size = Bounds.Size;
        var horizontal = Orientation == Orientation.Horizontal;
        var isClickable = IsClickable && IsEffectivelyEnabled;
        var isHovered = isClickable && IsHovered;
        var isPressed = isClickable && IsPressed;
        var sortDirection = SortDirection;
        var isSorted = sortDirection != null;

        DrawingContext.PushedState? rotation = null;
        if (horizontal)
        {
            // Rotate by -90 degrees when horizontal.
            rotation = dc.PushTransform(Matrix.CreateRotation(-Math.PI / 2) * Matrix.CreateTranslation(0.0, size.Height));
            size = new Size(size.Height, size.Width);
        }

        var backgroundType = NormalBackground;
        if (isPressed)
        {
            backgroundType = PressedBackground;
        }
        else if (isHovered)
        {
            backgroundType = HoveredBackground;
        }

        FillRect(dc, Token(backgroundType), new Rect(0.0, 0.0, size.Width, size.Height));

        if (size.Width >= 2.0 || size.Height >= 2.0)
        {
            var sideType = NormalSides;
            if (isPressed)
            {
                sideType = PressedSides;
            }
            else if (isHovered)
            {
                sideType = HoveredSides;
            }

            if (IsSeparatorVisible)
            {
                var sideBrush = SeparatorBrush ?? Token(sideType);

                if (size.Width >= 2.0)
                {
                    if (horizontal)
                    {
                        FillRect(dc, sideBrush, new Rect(0.0, 0.0, 1.0, size.Height)); // left
                        if (sideType != NormalSides)
                        {
                            FillRect(dc, sideBrush, new Rect(size.Width - 0.0, 0.0, 1.0, size.Height)); // right
                        }
                    }
                    else
                    {
                        if (sideType != NormalSides)
                        {
                            FillRect(dc, sideBrush, new Rect(-1.0, 0.0, 1.0, size.Height)); // left
                        }

                        FillRect(dc, sideBrush, new Rect(size.Width - 1.0, 0.0, 1.0, size.Height)); // right
                    }
                }

                if (size.Height >= 2.0)
                {
                    FillRect(dc, sideBrush, new Rect(0.0, 0.0, size.Width, 1.0)); // top
                    FillRect(dc, sideBrush, new Rect(0.0, size.Height - 1.0, size.Width, 1.0)); // bottom
                }
            }
        }

        if (isSorted && (size.Width > 14.0) && (size.Height > 10.0))
        {
            using (dc.PushTransform(Matrix.CreateTranslation((size.Width - 8.0) * 0.5, 1.0)))
            {
                var ascending = sortDirection == ListSortDirection.Ascending;
                var arrowGeometry = ascending
                    ? s_arrowUpGeometry ??= Arrow(new Point(0.0, 4.0), new Point(4.0, 0.0), new Point(8.0, 4.0))
                    : s_arrowDownGeometry ??= Arrow(new Point(0.0, 0.0), new Point(8.0, 0.0), new Point(4.0, 4.0));

                dc.DrawGeometry(Token(ArrowFill), null, arrowGeometry);
            }
        }

        rotation?.Dispose();
    }

    private IBrush? Token(string name)
    {
        // The per-instance cache of the base, indexed by the position of the token in TokenSet.
        var index = IndexOf(name);
        if (GetCachedResource(index) is IBrush cached)
        {
            return cached;
        }

        var brush = ChromeBrush(name);
        if (brush != null)
        {
            CacheResource(index, brush);
        }

        return brush;
    }

    private static int IndexOf(string name)
    {
        for (var i = 0; i < TokenSet.Count; i++)
        {
            if (string.Equals(TokenSet[i], name, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(name));
    }

    private static Geometry Arrow(Point a, Point b, Point c)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(a, true);
            ctx.LineTo(b, false);
            ctx.LineTo(c, false);
            ctx.EndFigure(true);
        }

        return geometry;
    }
}
