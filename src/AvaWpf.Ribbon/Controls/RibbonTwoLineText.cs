// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonTwoLineText.cs (MIT, see NOTICE.md).
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace AvaWpf.Ribbon;

/// <summary>
/// The label of a Ribbon control: one line, or two with <see cref="HasTwoLines"/>, then an optional glyph.
/// </summary>
/// <remarks>
/// Splits at the space nearest the middle, which matches WPF's half-width wrap for short Ribbon labels.
/// </remarks>
[PseudoClasses(":twolines", ":haspath")]
public class RibbonTwoLineText : TemplatedControl
{
    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<RibbonTwoLineText, string?>(nameof(Text));

    /// <summary>Defines the <see cref="HasTwoLines"/> property.</summary>
    public static readonly StyledProperty<bool> HasTwoLinesProperty =
        AvaloniaProperty.Register<RibbonTwoLineText, bool>(nameof(HasTwoLines));

    /// <summary>Defines the <see cref="PathData"/> property.</summary>
    public static readonly StyledProperty<Geometry?> PathDataProperty =
        AvaloniaProperty.Register<RibbonTwoLineText, Geometry?>(nameof(PathData));

    /// <summary>Defines the <see cref="PathFill"/> property.</summary>
    public static readonly StyledProperty<IBrush?> PathFillProperty =
        AvaloniaProperty.Register<RibbonTwoLineText, IBrush?>(nameof(PathFill));

    /// <summary>Defines the <see cref="PathStroke"/> property.</summary>
    public static readonly StyledProperty<IBrush?> PathStrokeProperty =
        AvaloniaProperty.Register<RibbonTwoLineText, IBrush?>(nameof(PathStroke));

    /// <summary>Defines the <see cref="TextAlignment"/> property.</summary>
    public static readonly StyledProperty<TextAlignment> TextAlignmentProperty =
        AvaloniaProperty.Register<RibbonTwoLineText, TextAlignment>(nameof(TextAlignment));

    /// <summary>Defines the <see cref="TextTrimming"/> property.</summary>
    public static readonly StyledProperty<TextTrimming> TextTrimmingProperty =
        AvaloniaProperty.Register<RibbonTwoLineText, TextTrimming>(nameof(TextTrimming), TextTrimming.None);

    /// <summary>Defines the <see cref="LineHeight"/> property.</summary>
    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<RibbonTwoLineText, double>(nameof(LineHeight), double.NaN);

    /// <summary>Defines the <see cref="FirstLine"/> property.</summary>
    public static readonly DirectProperty<RibbonTwoLineText, string?> FirstLineProperty =
        AvaloniaProperty.RegisterDirect<RibbonTwoLineText, string?>(nameof(FirstLine), o => o.FirstLine);

    /// <summary>Defines the <see cref="SecondLine"/> property.</summary>
    public static readonly DirectProperty<RibbonTwoLineText, string?> SecondLineProperty =
        AvaloniaProperty.RegisterDirect<RibbonTwoLineText, string?>(nameof(SecondLine), o => o.SecondLine);

    /// <summary>Defines the <see cref="LineShift"/> property.</summary>
    public static readonly DirectProperty<RibbonTwoLineText, ITransform?> LineShiftProperty =
        AvaloniaProperty.RegisterDirect<RibbonTwoLineText, ITransform?>(nameof(LineShift), o => o.LineShift);

    private ITransform? _lineShift;
    private string? _firstLine;
    private string? _secondLine;
    private string? _splitText;
    private (string? First, string Second) _split;

    /// <summary>The text.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Whether the text is split over two lines (the large variant of a control).</summary>
    public bool HasTwoLines
    {
        get => GetValue(HasTwoLinesProperty);
        set => SetValue(HasTwoLinesProperty, value);
    }

    /// <summary>The glyph after the last line (the drop-down arrow of a menu button).</summary>
    public Geometry? PathData
    {
        get => GetValue(PathDataProperty);
        set => SetValue(PathDataProperty, value);
    }

    /// <summary>The fill of the glyph.</summary>
    public IBrush? PathFill
    {
        get => GetValue(PathFillProperty);
        set => SetValue(PathFillProperty, value);
    }

    /// <summary>The stroke of the glyph.</summary>
    public IBrush? PathStroke
    {
        get => GetValue(PathStrokeProperty);
        set => SetValue(PathStrokeProperty, value);
    }

    /// <summary>The alignment of the lines.</summary>
    public TextAlignment TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    /// <summary>The trimming of the last line.</summary>
    public TextTrimming TextTrimming
    {
        get => GetValue(TextTrimmingProperty);
        set => SetValue(TextTrimmingProperty, value);
    }

    /// <summary>The height of each line. NaN uses the font's line height.</summary>
    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    /// <summary>
    /// The vertical shift that puts each line's baseline where WPF does with a fixed <see cref="LineHeight"/>.
    /// WPF uses <c>LineHeight × ascent ÷ line spacing</c>; Avalonia keeps the font's ascent. Null without a line height.
    /// </summary>
    public ITransform? LineShift
    {
        get => _lineShift;
        private set => SetAndRaise(LineShiftProperty, ref _lineShift, value);
    }

    /// <summary>The first line: null when the text is on one line.</summary>
    public string? FirstLine
    {
        get => _firstLine;
        private set => SetAndRaise(FirstLineProperty, ref _firstLine, value);
    }

    /// <summary>The last line (the whole text when it is on one line); the glyph follows it.</summary>
    public string? SecondLine
    {
        get => _secondLine;
        private set => SetAndRaise(SecondLineProperty, ref _secondLine, value);
    }

    /// <summary>Splits a label at the space nearest its middle.</summary>
    /// <param name="text">The label.</param>
    /// <returns>The two lines; the first is null when there is no space.</returns>
    internal static (string? First, string Second) Split(string text)
    {
        var trimmed = text.Trim();
        var best = -1;
        var middle = trimmed.Length / 2.0;
        for (var i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] == ' ' && (best < 0 || Math.Abs(i - middle) < Math.Abs(best - middle)))
            {
                best = i;
            }
        }

        return best < 0 ? (null, trimmed) : (trimmed[..best].TrimEnd(), trimmed[(best + 1)..].TrimStart());
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty || change.Property == HasTwoLinesProperty)
        {
            UpdateLines();
        }
        else if (change.Property == PathDataProperty)
        {
            PseudoClasses.Set(":haspath", change.NewValue is not null);
        }
        else if (change.Property == LineHeightProperty || change.Property == FontFamilyProperty || change.Property == FontSizeProperty
            || change.Property == FontStyleProperty || change.Property == FontWeightProperty)
        {
            UpdateLineShift();
        }
    }

    private void UpdateLineShift()
    {
        var lineHeight = LineHeight;
        if (double.IsNaN(lineHeight) || lineHeight <= 0)
        {
            LineShift = null;
            return;
        }

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight);
        var metrics = typeface.GlyphTypeface.Metrics;
        var ascent = -metrics.Ascent;
        var spacing = ascent + metrics.Descent + metrics.LineGap;
        if (spacing <= 0)
        {
            LineShift = null;
            return;
        }

        using var layout = new TextLayout("X", typeface, FontSize, null, lineHeight: lineHeight);
        var shift = (lineHeight * ascent / spacing) - layout.TextLines[0].Baseline;
        LineShift = Math.Abs(shift) < 0.01 ? null : new TranslateTransform(0, shift);
    }

    private void UpdateLines()
    {
        PseudoClasses.Set(":twolines", HasTwoLines);
        var text = Text ?? string.Empty;
        if (!HasTwoLines)
        {
            FirstLine = null;
            SecondLine = text;
            return;
        }

        if (!ReferenceEquals(text, _splitText))
        {
            // Large <-> small toggles HasTwoLines at every group size step; split each text once.
            _splitText = text;
            _split = Split(text);
        }

        var (first, second) = _split;
        if (first is null)
        {
            // A one-word label sits on the first line; the second line holds only the glyph.
            FirstLine = second;
            SecondLine = string.Empty;
        }
        else
        {
            FirstLine = first ?? string.Empty;
            SecondLine = second;
        }
    }
}
