using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using AvaWpf.Animations;

[assembly: AvaloniaTestApplication(typeof(AvaWpf.Ribbon.Tests.TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace AvaWpf.Ribbon.Tests;

public class TestApplication : Application
{
    public TestApplication()
    {
        WpfAnimations.TimeScale = 0;
        Theme = new AvaWpfTheme();
        Styles.Add(Theme);
        Styles.Add(new AvaWpfRibbonTheme());
    }

    public AvaWpfTheme Theme { get; }

    public static TestApplication Instance => (TestApplication)Current!;

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .WithAvaWpfFonts()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
