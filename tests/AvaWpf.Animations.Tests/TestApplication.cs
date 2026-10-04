using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

[assembly: AvaloniaTestApplication(typeof(AvaWpf.Animations.Tests.TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace AvaWpf.Animations.Tests;

public class TestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
