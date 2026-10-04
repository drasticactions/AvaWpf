using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaWpf;

/// <summary>
/// WPF rounds a text line's height to the nearest device pixel; Avalonia rounds every measured size up. Tahoma at 11 px
/// has a 13.28 px line, so WPF lays it out 13 px tall and Avalonia 14. A TextBlock whose line would round down gets
/// that rounded height as its <see cref="TextBlock.LineHeight"/>, unless the app gave it a line height of its own.
/// </summary>
internal static class TextLineRounding
{
    private static readonly AttachedProperty<bool> s_ownedProperty =
        AvaloniaProperty.RegisterAttached<TextBlock, bool>("LineHeightRounded", typeof(TextLineRounding));

    private static bool s_registered;
    private static bool s_setting;

    internal static void EnsureRegistered()
    {
        if (s_registered)
        {
            return;
        }

        s_registered = true;
        TextBlock.FontSizeProperty.Changed.AddClassHandler<TextBlock>((t, _) => Apply(t));
        TextBlock.FontFamilyProperty.Changed.AddClassHandler<TextBlock>((t, _) => Apply(t));
        TextBlock.FontWeightProperty.Changed.AddClassHandler<TextBlock>((t, _) => Apply(t));
        TextBlock.FontStyleProperty.Changed.AddClassHandler<TextBlock>((t, _) => Apply(t));
        TextBlock.FontStretchProperty.Changed.AddClassHandler<TextBlock>((t, _) => Apply(t));
        Visual.VisualParentProperty.Changed.AddClassHandler<TextBlock>((t, _) => Apply(t));

        // A line height set by anyone else is the app's: it is kept from then on. One set on an ancestor is inherited,
        // which our value on a TextBlock below would hide, so those TextBlocks drop theirs.
        TextBlock.LineHeightProperty.Changed.AddClassHandler<Control>((c, e) =>
        {
            if (s_setting || e.Priority == BindingPriority.Inherited)
            {
                return;
            }

            if (c is TextBlock text)
            {
                text.ClearValue(s_ownedProperty);
                return;
            }

            foreach (var below in c.GetVisualDescendants().OfType<TextBlock>())
            {
                if (below.GetValue(s_ownedProperty))
                {
                    Apply(below);
                }
            }
        });
    }

    private static void Apply(TextBlock text)
    {
        if (s_setting)
        {
            return;
        }

        s_setting = true;
        try
        {
            // Our value comes off first, so an inherited or style line height underneath shows through.
            if (text.GetValue(s_ownedProperty))
            {
                text.ClearValue(TextBlock.LineHeightProperty);
                text.ClearValue(s_ownedProperty);
            }

            if (!double.IsNaN(text.LineHeight))
            {
                return;
            }

            var rounded = RoundedLineHeight(text);
            if (!double.IsNaN(rounded))
            {
                text.LineHeight = rounded;
                text.SetValue(s_ownedProperty, true);
            }
        }
        finally
        {
            s_setting = false;
        }
    }

    /// <summary>The line height rounded to the nearest device pixel, or NaN when Avalonia's round-up gives the same.</summary>
    private static double RoundedLineHeight(TextBlock text)
    {
        // Runs of other sizes or fonts have their own line metrics.
        if (text.Inlines is { Count: > 0 })
        {
            return double.NaN;
        }

        var typeface = new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch);
        if (!FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphs))
        {
            return double.NaN;
        }

        var metrics = glyphs.Metrics;
        var natural = (metrics.Descent - metrics.Ascent + metrics.LineGap) * text.FontSize / metrics.DesignEmHeight;
        var scale = TopLevel.GetTopLevel(text)?.RenderScaling ?? 1;
        var rounded = Math.Round(natural * scale, MidpointRounding.AwayFromZero) / scale;
        return rounded < natural - 1e-6 ? rounded : double.NaN;
    }
}
