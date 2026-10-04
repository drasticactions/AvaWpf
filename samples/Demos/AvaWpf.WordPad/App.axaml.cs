using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AvaWpf.Ribbon;

namespace AvaWpf.WordPad;

public partial class App : Application
{
    /// <summary>Creates the desktop main window; the Desktop head sets it (a RibbonWindow).</summary>
    public static Func<Window>? DesktopWindowFactory { get; set; }

    /// <summary>The theme, scheme and variant the app starts with (from the launch options).</summary>
    public static (ThemeFamily Family, string? Scheme, ThemeVariant Variant)? StartTheme { get; set; }

    /// <summary>The AvaWpf theme of the app.</summary>
    public static AvaWpfTheme Theme => Current!.Styles.OfType<AvaWpfTheme>().Single();

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <summary>Applies <see cref="StartTheme"/>. Returns an error message for an unknown scheme.</summary>
    public static string? ApplyStartTheme()
    {
        if (StartTheme is { } start)
        {
            try
            {
                Theme.Theme = start.Family;
                Theme.ColorScheme = start.Scheme;
                Current!.RequestedThemeVariant = start.Variant;
            }
            catch (ArgumentException e)
            {
                return e.Message;
            }
        }

        return null;
    }

    /// <summary>
    /// Wraps the shell in a <see cref="WindowFrame"/> that lends its caption to the Ribbon, as a RibbonWindow does
    /// (Browser, headless <c>--framed</c>). The frame title follows the document name.
    /// </summary>
    public static WindowFrame CreateFramedShell()
    {
        var view = new MainView();
        var frame = new WindowFrame
        {
            Title = view.Title,
            Icon = Icons.Get("wordpad"),
            CanResize = false,
            Content = view,
        };
        RibbonWindow.SetHostsRibbon(frame, true);
        view.TitleChanged += (_, _) => frame.Title = view.Title;
        return frame;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplyStartTheme() is { } error)
        {
            Console.Error.WriteLine(error);
        }

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = DesktopWindowFactory?.Invoke() ?? new Window { Content = new MainView() };
                break;
            case ISingleViewApplicationLifetime single:
                // The browser has no windows: the shell sits inside a WindowFrame.
                single.MainView = CreateFramedShell();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
