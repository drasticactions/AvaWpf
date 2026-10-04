// Ported from WPF $W/Themes/PresentationFramework.Classic/Microsoft/Windows/Themes/DataGridHeaderBorder.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaWpf.Chrome.Classic;

/// <summary>The Classic DataGrid header look (WPF <c>Microsoft.Windows.Themes.DataGridHeaderBorder</c>).</summary>
public sealed class DataGridHeaderBorder : DataGridHeaderBorderBase
{
    private const int ArrowUpGeometry = 0;
    private const int ArrowDownGeometry = 1;

    /// <summary>The token names this chrome reads; empty, since every color is a <c>SystemColors</c> resource.</summary>
    public static IReadOnlyList<string> TokenSet { get; } = [];

    /// <summary>Draws the Classic header look.</summary>
    /// <param name="dc">The drawing context.</param>
    protected override void RenderTheme(DrawingContext dc)
    {
        var size = Bounds.Size;
        var isClickable = IsClickable && IsEffectivelyEnabled;
        var isPressed = isClickable && IsPressed;
        var sortDirection = SortDirection;
        var isSorted = sortDirection != null;
        var horizontal = Orientation == Orientation.Horizontal;
        var background = SystemBrush("SystemColors.ControlBrush");
        var light = SystemBrush("SystemColors.ControlLightBrush");
        var dark = SystemBrush("SystemColors.ControlDarkBrush");
        var shouldDrawRight = true;
        var shouldDrawBottom = true;
        var usingSeparatorBrush = false;

        IBrush? darkDarkRight = null;
        if (!horizontal)
        {
            if (IsSeparatorVisible && SeparatorBrush != null)
            {
                darkDarkRight = SeparatorBrush;
                usingSeparatorBrush = true;
            }
            else
            {
                shouldDrawRight = false;
            }
        }
        else
        {
            darkDarkRight = SystemBrush("SystemColors.ControlDarkDarkBrush");
        }

        IBrush? darkDarkBottom = null;
        if (horizontal)
        {
            if (IsSeparatorVisible && SeparatorBrush != null)
            {
                darkDarkBottom = SeparatorBrush;
                usingSeparatorBrush = true;
            }
            else
            {
                shouldDrawBottom = false;
            }
        }
        else
        {
            darkDarkBottom = SystemBrush("SystemColors.ControlDarkDarkBrush");
        }

        FillRect(dc, background, new Rect(0.0, 0.0, size.Width, size.Height));

        if (size.Width > 3.0 && size.Height > 3.0)
        {
            if (isPressed)
            {
                FillRect(dc, dark, new Rect(0.0, 0.0, size.Width, 1.0));
                FillRect(dc, dark, new Rect(0.0, 0.0, 1.0, size.Height));
                FillRect(dc, dark, new Rect(0.0, Max0(size.Height - 1.0), size.Width, 1.0));
                FillRect(dc, dark, new Rect(Max0(size.Width - 1.0), 0.0, 1.0, size.Height));
            }
            else
            {
                FillRect(dc, light, new Rect(0.0, 0.0, 1.0, Max0(size.Height - 1.0)));
                FillRect(dc, light, new Rect(0.0, 0.0, Max0(size.Width - 1.0), 1.0));

                if (shouldDrawRight)
                {
                    if (!usingSeparatorBrush)
                    {
                        FillRect(dc, dark, new Rect(Max0(size.Width - 2.0), 1.0, 1.0, Max0(size.Height - 2.0)));
                    }

                    FillRect(dc, darkDarkRight, new Rect(Max0(size.Width - 1.0), 0.0, 1.0, size.Height));
                }

                if (shouldDrawBottom)
                {
                    if (!usingSeparatorBrush)
                    {
                        FillRect(dc, dark, new Rect(1.0, Max0(size.Height - 2.0), Max0(size.Width - 2.0), 1.0));
                    }

                    FillRect(dc, darkDarkBottom, new Rect(0.0, Max0(size.Height - 1.0), size.Width, 1.0));
                }
            }
        }

        if (isSorted && size.Width > 14.0 && size.Height > 10.0)
        {
            // A sort arrow on the right. The offset is snapped so the 5 px arrow sits on device pixels.
            var x = PixelSnap.Round(size.Width - 15.0, RenderScale);
            var y = PixelSnap.Round((size.Height - 5.0) * 0.5, RenderScale);

            var ascending = sortDirection == ListSortDirection.Ascending;
            var slot = ascending ? ArrowUpGeometry : ArrowDownGeometry;
            if (GetCachedResource(slot) is not Geometry arrowGeometry)
            {
                arrowGeometry = CreateArrow(ascending);
                CacheResource(slot, arrowGeometry);
            }

            // In high contrast the arrow uses the control text color, as WPF with the .NET 4.7 accessibility fixes.
            var sortArrowBrush = IsHighContrast
                ? SystemBrush("SystemColors.ControlTextBrush")
                : SystemBrush("SystemColors.GrayTextBrush");

            using (dc.PushTransform(Matrix.CreateTranslation(x, y)))
            {
                dc.DrawGeometry(sortArrowBrush, null, arrowGeometry);
            }
        }
    }

    private static Geometry CreateArrow(bool ascending)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            if (ascending)
            {
                g.BeginFigure(new Point(0.0, 5.0), true);
                g.LineTo(new Point(5.0, 0.0), false);
                g.LineTo(new Point(10.0, 5.0), false);
            }
            else
            {
                g.BeginFigure(new Point(0.0, 0.0), true);
                g.LineTo(new Point(10.0, 0.0), false);
                g.LineTo(new Point(5.0, 5.0), false);
            }

            g.EndFigure(true);
        }

        return geometry;
    }
}
