using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace AvaWpf.WordPad.Desktop;

/// <summary>Renders the WordPad window (<c>--screenshot</c>) with the headless platform.</summary>
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

        Window window;
        if (o.Framed)
        {
            var frame = App.CreateFramedShell();
            if (o.Picture && frame.Content is MainView view)
            {
                view.InsertSamplePicture();
            }

            window = new Window { Width = o.Width, Height = o.Height, Content = frame };
        }
        else
        {
            window = new MainWindow();
        }

        window.Show();

        // The Ribbon places the contextual tab group headers after a layout pass, so run a few.
        for (var i = 0; i < 8; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        using var frameBitmap = window.CaptureRenderedFrame();
        if (frameBitmap is null)
        {
            Console.Error.WriteLine("nothing was rendered");
            return 1;
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(o.Screenshot!));
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        frameBitmap.Save(o.Screenshot!, PngBitmapEncoderOptions.Default);
        window.Close();
        Console.WriteLine(o.Screenshot);
        return 0;
    }
}
