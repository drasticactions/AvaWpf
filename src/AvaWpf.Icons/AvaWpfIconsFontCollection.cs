using System;
using Avalonia.Media.Fonts;

namespace AvaWpf.Icons;

/// <summary>
/// The embedded font collection (<c>fonts:AvaWpfIcons</c>) with the full Fluent UI System Icons fonts, Regular and
/// Filled, unmodified. Register it with <see cref="AvaWpfIconsAppBuilderExtensions.WithAvaWpfIcons"/>.
/// </summary>
public sealed class AvaWpfIconsFontCollection : EmbeddedFontCollection
{
    /// <summary>The collection key, <c>fonts:AvaWpfIcons</c>.</summary>
    public static readonly Uri CollectionKey = new("fonts:AvaWpfIcons", UriKind.Absolute);

    /// <summary>The family of the Regular glyphs, for <see cref="Avalonia.Media.FontFamily"/>.</summary>
    public const string RegularFamily = "fonts:AvaWpfIcons#FluentSystemIcons-Regular";

    /// <summary>The family of the Filled glyphs, for <see cref="Avalonia.Media.FontFamily"/>.</summary>
    public const string FilledFamily = "fonts:AvaWpfIcons#FluentSystemIcons-Filled";

    private static readonly Uri s_source = new("avares://AvaWpf.Icons/Assets", UriKind.Absolute);

    /// <summary>Initializes the collection and loads the embedded faces.</summary>
    public AvaWpfIconsFontCollection() : base(CollectionKey, s_source)
    {
    }
}
