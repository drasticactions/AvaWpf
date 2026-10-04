using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AvaWpf.Gallery.Desktop;

/// <summary>Renders a page (<c>--screenshot</c>) or a reference scene (<c>--scene</c>) with the headless platform.</summary>
internal static class Headless
{
    public static int Run(LaunchOptions o)
    {
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHarfBuzz()
            .WithAvaWpfFonts()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();

        if (o.StartThemeApplied() is { } error)
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        if (o.Scene is { } scene)
        {
            return SceneRenderer.Render(scene, o.Out ?? ".", o);
        }

        var window = new Window { Width = o.Width, Height = o.Height, Content = new MainView() };
        return Capture(window, o.Screenshot!);
    }

    private static string? StartThemeApplied(this LaunchOptions o)
    {
        if (App.StartTheme is { } start)
        {
            try
            {
                App.Theme.Theme = start.Family;
                App.Theme.ColorScheme = start.Scheme;
                Application.Current!.RequestedThemeVariant = start.Variant;
            }
            catch (ArgumentException e)
            {
                return e.Message;
            }
        }

        return null;
    }

    public static int Capture(Window window, string path, Action<Window>? input = null, Action<Window>? afterRender = null, int settleMs = 0)
    {
        window.Show();
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        if (input is not null)
        {
            input(window);
            for (var i = 0; i < 3; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }
        }

        // Let Avalonia transitions (which run on the wall clock) finish, as the WPF shooter waits 600 ms for theme
        // animations before it captures.
        if (settleMs > 0)
        {
            // The dispatcher main loop runs the animation timer and the render timer on the wall clock.
            using var cts = new System.Threading.CancellationTokenSource(settleMs);
            Dispatcher.UIThread.MainLoop(cts.Token);
            for (var i = 0; i < 3; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }
        }

        using var frame = window.CaptureRenderedFrame();
        if (frame is null)
        {
            Console.Error.WriteLine("nothing was rendered");
            return 1;
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        frame.Save(path, PngBitmapEncoderOptions.Default);
        afterRender?.Invoke(window);
        window.Close();
        Console.WriteLine(path);
        return 0;
    }
}
