using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AvaWpf.Explorer.Desktop;

/// <summary>Renders the Explorer window (<c>--screenshot</c>) with the headless platform.</summary>
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

        if (App.ApplyStartTheme() is { } error)
        {
            Console.Error.WriteLine(error);
            return 2;
        }

        Window window = o.Framed
            ? new Window { Width = o.Width, Height = o.Height, Content = App.CreateFramedShell() }
            : new MainWindow();
        window.Show();
        for (var i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        using var frame = window.CaptureRenderedFrame();
        if (frame is null)
        {
            Console.Error.WriteLine("nothing was rendered");
            return 1;
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(o.Screenshot!));
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        frame.Save(o.Screenshot!, PngBitmapEncoderOptions.Default);
        window.Close();
        Console.WriteLine(o.Screenshot);
        return 0;
    }
}
