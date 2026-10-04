using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using AvaWpf;
using AvaWpf.Explorer;

internal static class Program
{
    private static Task Main(string[] args) =>
        BuildAvaloniaApp().WithAvaWpfFonts().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
}
