using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaWpf.Gallery;

public partial class App : Application
{
    /// <summary>Creates the desktop main window; the Desktop head sets it (a ThemeWindow).</summary>
    public static Func<Window>? DesktopWindowFactory { get; set; }

    /// <summary>The theme, family and variant the app starts with (from the launch options).</summary>
    public static (ThemeFamily Family, string? Scheme, ThemeVariant Variant)? StartTheme { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>The AvaWpf theme of the app.</summary>
    public static AvaWpfTheme Theme => Current!.Styles.OfType<AvaWpfTheme>().Single();

    public override void OnFrameworkInitializationCompleted()
    {
        if (StartTheme is { } start)
        {
            Theme.Theme = start.Family;
            Theme.ColorScheme = start.Scheme;
            RequestedThemeVariant = start.Variant;
        }

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = DesktopWindowFactory?.Invoke() ?? new Window { Content = new MainView() };
                break;
            case ISingleViewApplicationLifetime single:
                // The browser has no windows: the shell sits inside a WindowFrame.
                single.MainView = new WindowFrame { Title = "AvaWpf Gallery", CanResize = false, Content = new MainView() };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
