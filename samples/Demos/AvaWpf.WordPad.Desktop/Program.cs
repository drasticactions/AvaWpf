using System;
using Avalonia;
using Avalonia.Styling;
using AvaWpf.Animations;

namespace AvaWpf.WordPad.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        LaunchOptions.Parse(args);
        var o = LaunchOptions.Current;
        if (o.Theme is { } theme)
        {
            App.StartTheme = (theme, o.Scheme, Variant(o.Variant));
        }
        else if (o.Scheme is not null || o.Variant != "Light")
        {
            App.StartTheme = (ThemeFamily.Aero, o.Scheme, Variant(o.Variant));
        }

        if (o.DisableAnimations || o.Headless)
        {
            WpfAnimations.IsEnabled = false;
        }

        if (o.Headless)
        {
            return Headless.Run(o);
        }

        App.DesktopWindowFactory = () => new MainWindow();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static ThemeVariant Variant(string name) => name.ToLowerInvariant() switch
    {
        "dark" => ThemeVariant.Dark,
        "highcontrast" => WpfThemeVariants.HighContrast,
        _ => ThemeVariant.Light,
    };

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseWaylandWithFallback()
            .WithAvaWpfFonts()
            .LogToTrace();
}
