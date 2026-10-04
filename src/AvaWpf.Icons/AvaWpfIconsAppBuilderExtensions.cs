using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWpf.Icons;

/// <summary>AppBuilder extensions of AvaWpf.Icons.</summary>
public static class AvaWpfIconsAppBuilderExtensions
{
    /// <summary>The resource key of the Regular icon family.</summary>
    public const string FluentIconsRegularKey = "FluentIconsRegular";

    /// <summary>The resource key of the Filled icon family.</summary>
    public const string FluentIconsFilledKey = "FluentIconsFilled";

    /// <summary>
    /// Registers the <see cref="AvaWpfIconsFontCollection"/>. Once the application is set up, the
    /// <c>FluentIconsRegular</c> and <c>FluentIconsFilled</c> application resources name the two families, so
    /// <c>FontFamily="{DynamicResource FluentIconsRegular}"</c> with <see cref="FluentIcon"/> glyphs renders the icons.
    /// </summary>
    public static AppBuilder WithAvaWpfIcons(this AppBuilder builder) =>
        builder
            .ConfigureFonts(fontManager => fontManager.AddFontCollection(new AvaWpfIconsFontCollection()))
            .AfterSetup(b =>
            {
                if (b.Instance is { } app)
                {
                    AddResources(app.Resources);
                }
            });

    /// <summary>Adds the <c>FluentIconsRegular</c> and <c>FluentIconsFilled</c> font families to a dictionary.</summary>
    public static void AddResources(IResourceDictionary resources)
    {
        resources[FluentIconsRegularKey] = new FontFamily(AvaWpfIconsFontCollection.RegularFamily);
        resources[FluentIconsFilledKey] = new FontFamily(AvaWpfIconsFontCollection.FilledFamily);
    }
}
