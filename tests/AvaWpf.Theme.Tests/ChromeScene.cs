using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// Builds a WPF reference scene (tools/wpf-reference/scenes/*.json) in a headless window, the way the gallery's
/// SceneBuilder does: the control centered with an 8 px margin in a panel of the window's client size, on the family
/// surface. Bounds are then comparable with the shooter's layout dumps (tools/wpf-reference/out/*/*.json).
/// </summary>
internal sealed class ChromeScene : IDisposable
{
    private ChromeScene(Window window, Control target)
    {
        Window = window;
        Target = target;
    }

    public Window Window { get; }

    public Control Target { get; }

    public static ChromeScene Show(ThemeFamily family, string? scheme, Control target, double width, double height, double scaling = 1.0)
    {
        var theme = TestApplication.Instance.Theme;
        theme.Theme = family;
        theme.ColorScheme = scheme;
        target.Margin = new Thickness(8);
        target.HorizontalAlignment = HorizontalAlignment.Center;
        target.VerticalAlignment = VerticalAlignment.Center;
        var surface = family == ThemeFamily.Fluent ? "Fluent.ApplicationBackgroundBrush" : "SystemColors.ControlBrush";
        var panel = new Panel { [!Panel.BackgroundProperty] = new DynamicResourceExtension(surface), Children = { target } };
        var window = new Window { Width = width, Height = height, Content = panel, SizeToContent = SizeToContent.Manual };
        window.Show();
        if (Math.Abs(scaling - 1.0) > 0.001)
        {
            window.SetRenderScaling(scaling);
        }

        Pump();
        return new ChromeScene(window, target);
    }

    public static void Pump(int frames = 3)
    {
        for (var i = 0; i < frames; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    /// <summary>The <paramref name="index"/>-th visual named <paramref name="name"/> in the target's template tree.</summary>
    public Visual Part(string name, int index = 0) =>
        Target.GetVisualDescendants().Where(v => v is StyledElement { Name: { } n } && n == name).ElementAt(index);

    /// <summary>The bounds of <paramref name="visual"/> relative to the scene (the window client area), in DIPs.</summary>
    public Rect BoundsOf(Visual visual)
    {
        var origin = visual.TranslatePoint(default, Window) ?? throw new InvalidOperationException("not in the window");
        return new Rect(origin, visual.Bounds.Size);
    }

    public void Dispose()
    {
        Window.Close();
        var theme = TestApplication.Instance.Theme;
        theme.ColorScheme = null;
        theme.Theme = ThemeFamily.Aero2;
    }
}
