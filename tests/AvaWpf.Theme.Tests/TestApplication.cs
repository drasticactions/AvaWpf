using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using AvaWpf.Animations;

[assembly: AvaloniaTestApplication(typeof(AvaWpf.Theme.Tests.TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace AvaWpf.Theme.Tests;

public class TestApplication : Application
{
    public TestApplication()
    {
        WpfAnimations.TimeScale = 0;
        Theme = new AvaWpfTheme();
        Styles.Add(Theme);
    }

    public AvaWpfTheme Theme { get; }

    public static TestApplication Instance => (TestApplication)Current!;

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .WithAvaWpfFonts()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
