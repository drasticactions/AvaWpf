using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using AvaWpf.Animations;

[assembly: AvaloniaTestApplication(typeof(AvaWpf.Controls.Tests.TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace AvaWpf.Controls.Tests;

public class TestApplication : Application
{
    public TestApplication()
    {
        WpfAnimations.TimeScale = 0;
        Theme = new AvaWpfTheme();
        Styles.Add(Theme);
        Styles.Add(new AvaWpfControlsTheme());
    }

    public AvaWpfTheme Theme { get; }

    public static TestApplication Instance => (TestApplication)Current!;

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .WithAvaWpfFonts()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
