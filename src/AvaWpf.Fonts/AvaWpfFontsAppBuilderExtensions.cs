using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace AvaWpf;

/// <summary>AppBuilder extensions of AvaWpf.Fonts.</summary>
public static class AvaWpfFontsAppBuilderExtensions
{
    /// <summary>
    /// Registers the <see cref="AvaWpfFontCollection"/> and the aliases in <see cref="AvaWpfFontCollection.FamilyMappings"/>.
    /// </summary>
    /// <remarks>
    /// An app that sets its own <see cref="FontManagerOptions"/> after this call should copy
    /// <see cref="AvaWpfFontCollection.FamilyMappings"/> into them.
    /// </remarks>
    public static AppBuilder WithAvaWpfFonts(this AppBuilder builder)
    {
        var options = new FontManagerOptions
        {
            FontFamilyMappings = new Dictionary<string, FontFamily>(AvaWpfFontCollection.FamilyMappings, System.StringComparer.OrdinalIgnoreCase),
        };
        return builder
            .With(options)
            .ConfigureFonts(fontManager => fontManager.AddFontCollection(new AvaWpfFontCollection()));
    }
}
