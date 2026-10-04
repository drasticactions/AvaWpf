using System.Collections.Generic;

namespace AvaWpf;

/// <summary>
/// The SystemColors, SystemParameters and SystemFonts values of one OS era, scheme and variant.
/// </summary>
internal sealed class SystemSnapshot(string name, IReadOnlyDictionary<string, uint> colors, IReadOnlyDictionary<string, double> numbers, IReadOnlyDictionary<string, string> strings)
{
    /// <summary>The snapshot name, such as <c>Aero2</c>, <c>Aero2.Dark</c> or <c>Classic.Brick</c>.</summary>
    public string Name { get; } = name;

    /// <summary>The colors by WPF SystemColors role (<c>Control</c>, <c>ControlDark</c>, <c>Highlight</c>…).</summary>
    public IReadOnlyDictionary<string, uint> Colors { get; } = colors;

    /// <summary>Numeric parameters and font metrics; booleans are 0 or 1.</summary>
    public IReadOnlyDictionary<string, double> Numbers { get; } = numbers;

    /// <summary>Font families and enum-valued parameters.</summary>
    public IReadOnlyDictionary<string, string> Strings { get; } = strings;
}
