using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AvaWpf.Explorer.ViewModels;

namespace AvaWpf.Explorer;

public partial class App : Application
{
    /// <summary>Creates the desktop main window; the Desktop head sets it (a ThemeWindow).</summary>
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

    /// <summary>Wraps a shell in a <see cref="WindowFrame"/> whose title follows the current folder (Browser, headless).</summary>
    public static WindowFrame CreateFramedShell()
    {
        var view = new MainView();
        var frame = new WindowFrame
        {
            Title = view.ViewModel.Title,
            Icon = Icons.Get("folder-open"),
            CanResize = false,
            Content = view,
        };
        view.ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExplorerViewModel.Title))
            {
                frame.Title = view.ViewModel.Title;
            }
        };
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
