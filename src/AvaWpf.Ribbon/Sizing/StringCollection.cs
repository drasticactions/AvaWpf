// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/StringCollectionConverter.cs (MIT, see NOTICE.md).
using System;
using Avalonia.Collections;

namespace AvaWpf.Ribbon;

/// <summary>
/// A list of names, written in XAML as one comma-separated string (WPF's <c>StringCollectionConverter</c>), such as
/// <c>GroupSizeReductionOrder="Clipboard,Font,Paragraph"</c>.
/// </summary>
public class StringCollection : AvaloniaList<string>
{
    /// <summary>Parses a comma-separated list. Whitespace around each name is removed; empty names are dropped.</summary>
    /// <param name="s">The text.</param>
    /// <returns>The list.</returns>
    public static StringCollection Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);
        var result = new StringCollection();
        foreach (var part in s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            result.Add(part);
        }

        return result;
    }

    /// <inheritdoc/>
    public override string ToString() => string.Join(",", this);
}
