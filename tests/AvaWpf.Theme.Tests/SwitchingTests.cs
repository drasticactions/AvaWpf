using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaWpf.Chrome;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>Family switching and ThemeScope.</summary>
public class SwitchingTests
{
    private static AvaWpfTheme Theme => TestApplication.Instance.Theme;

    private static Window Show(Control content)
    {
        var window = new Window { Content = content, Width = 800, Height = 600 };
        window.Show();
        return window;
    }

    [AvaloniaFact]
    public void Family_Switch_ReTemplates_Live_Controls()
    {
        Theme.Theme = ThemeFamily.Aero2;
        var button = new Button { Content = "OK" };
        var window = Show(button);
        var before = button.GetVisualChildren().Single();
        Assert.Equal(ThemeFamily.Aero2, ChromeResources.Family(button));

        Theme.Theme = ThemeFamily.AeroLite;
        window.UpdateLayout();
        var after = button.GetVisualChildren().Single();

        Assert.NotSame(before, after);
        Assert.Equal(ThemeFamily.AeroLite, ChromeResources.Family(button));
        Assert.Equal(ThemeFamily.AeroLite, Theme.ActualTheme);
        Theme.Theme = ThemeFamily.Aero2;
        window.Close();
    }

    [AvaloniaFact]
    public void Scheme_Switch_Does_Not_ReAttach()
    {
        Theme.Theme = ThemeFamily.Luna;
        var loaded = 0;
        var button = new Button { Content = "OK" };
        button.Loaded += (_, _) => loaded++;
        var window = Show(button);
        var count = loaded;

        Theme.ColorScheme = ColorSchemes.Metallic;
        window.UpdateLayout();
        Assert.Equal(count, loaded);
        Assert.Equal(ColorSchemes.Metallic, Theme.ActualColorScheme);

        Theme.ColorScheme = null;
        Theme.Theme = ThemeFamily.Aero2;
        window.Close();
    }

    [AvaloniaFact]
    public void Variant_Switch_Does_Not_ReAttach()
    {
        var loaded = 0;
        var button = new Button { Content = "OK" };
        button.Loaded += (_, _) => loaded++;
        var window = Show(button);
        var count = loaded;

        window.RequestedThemeVariant = ThemeVariant.Dark;
        window.UpdateLayout();
        Assert.Equal(count, loaded);
        window.Close();
    }

    [AvaloniaFact]
    public void Invalid_Scheme_Throws()
    {
        Assert.Throws<ArgumentException>(() => Theme.ColorScheme = "Plaid");
        Theme.ColorScheme = null;
    }

    [AvaloniaFact]
    public void Switch_200_Controls_Under_250ms()
    {
        Theme.Theme = ThemeFamily.Aero2;
        var panel = new WrapPanel();
        for (var i = 0; i < 200; i++)
        {
            panel.Children.Add(i % 2 == 0 ? new Button { Content = "B" + i } : new ToggleButton { Content = "T" + i });
        }

        var window = Show(new ScrollViewer { Content = panel });
        window.UpdateLayout();

        // Warm up both families once, then time nine switches Aero2 <-> AeroLite. The median ignores the few switches a
        // busy machine or a GC slows down; the limit is for a typical switch.
        Theme.Theme = ThemeFamily.AeroLite;
        window.UpdateLayout();
        Theme.Theme = ThemeFamily.Aero2;
        window.UpdateLayout();

        var median = MedianSwitchMilliseconds(window, 9, ThemeFamily.AeroLite, ThemeFamily.Aero2);

        Assert.True(median < 250, $"median switch took {median:F0} ms");
        Theme.Theme = ThemeFamily.Aero2;
        window.Close();
    }

    [AvaloniaFact]
    public void Switch_Mixed_Controls_Across_All_Families()
    {
        // A gallery-like window (two of each control of the Buttons, Text, Lists, Selection, Range and Containers pages):
        // every family pair must switch, and repeated switching must not slow down (a leak of old templates shows up as
        // growing switch times).
        Theme.Theme = ThemeFamily.Aero2;
        var window = Show(MixedContent.Create(48));
        window.UpdateLayout();
        var families = Enum.GetValues<ThemeFamily>();
        foreach (var family in families)
        {
            Theme.Theme = family;
            window.UpdateLayout();
            Frame(window);
        }

        var first = MedianSwitchMilliseconds(window, 7, ThemeFamily.Aero2, ThemeFamily.Classic);
        foreach (var from in families)
        {
            foreach (var to in families)
            {
                Theme.Theme = from;
                window.UpdateLayout();
                Theme.Theme = to;
                window.UpdateLayout();
                Assert.Equal(to, Theme.ActualTheme);
                Assert.Equal(to, ChromeResources.Family(window.GetVisualDescendants().OfType<Button>().First()));
                Frame(window);
            }
        }

        var last = MedianSwitchMilliseconds(window, 7, ThemeFamily.Aero2, ThemeFamily.Classic);

        Assert.True(last < (first * 2) + 50, $"switching slowed down from {first:F0} ms to {last:F0} ms");
        Theme.Theme = ThemeFamily.Aero2;
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(typeof(ComboBox))]
    [InlineData(typeof(ListBox))]
    [InlineData(typeof(NumericUpDown))]
    [InlineData(typeof(DatePicker))]
    [InlineData(typeof(TreeView))]
    [InlineData(typeof(ScrollViewer))]
    public void No_Leaked_ControlThemes_With_Popups_Items_And_Code_Bindings(Type type)
    {
        // Each of these keeps discarded template parts alive in Avalonia 12.1 unless TemplateFrameCleanup releases them:
        // closed popups, virtualizing items panels, bindings set in OnApplyTemplate, ScrollBar owner bindings.
        Theme.Theme = ThemeFamily.Aero2;
        Control Create() => type == typeof(ScrollViewer)
            ? new ScrollViewer { Content = new Border { Width = 2000, Height = 2000 }, Height = 100 }
            : MixedContent.CreateKind(type, 1);
        var window = Show(new StackPanel { Children = { Create(), Create() } });
        window.UpdateLayout();
        var themes = new List<WeakReference>();
        for (var i = 0; i < 10; i++)
        {
            Theme.Theme = i % 2 == 0 ? ThemeFamily.AeroLite : ThemeFamily.Aero2;
            window.UpdateLayout();
            TrackTheme(themes, window, type);
            Frame(window);
        }

        Theme.Theme = ThemeFamily.Aero2;
        window.UpdateLayout();
        Frame(window);
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        var alive = themes.Count(w => w.IsAlive);

        Assert.True(alive <= 2, $"{alive} {type.Name} ControlTheme instances still alive after 10 switches");
        window.Close();
    }

    [AvaloniaFact]
    public void Switching_Does_Not_Accumulate_Logical_Children()
    {
        // Presenters add the TextBlock they create for string content to the control's logical children, and virtualizing
        // panels add their containers to the items control; a discarded template must take them away again.
        Theme.Theme = ThemeFamily.Aero2;
        var button = new Button { Content = "OK" };
        var toggle = new ToggleSwitch();
        var list = new ListBox { ItemsSource = new[] { "One", "Two", "Three" } };
        var window = Show(new StackPanel { Children = { button, toggle, list } });
        window.UpdateLayout();
        static int Count(Control c) => ((Avalonia.LogicalTree.ILogical)c).LogicalChildren.Count;
        var before = (Count(button), Count(toggle), Count(list));

        foreach (var family in Enum.GetValues<ThemeFamily>())
        {
            Theme.Theme = family;
            window.UpdateLayout();
        }

        Assert.Equal(before, (Count(button), Count(toggle), Count(list)));
        Theme.Theme = ThemeFamily.Aero2;
        window.Close();
    }

    private static double MedianSwitchMilliseconds(Window window, int count, ThemeFamily a, ThemeFamily b)
    {
        var times = new List<double>(count);
        for (var i = 0; i < count; i++)
        {
            var sw = Stopwatch.StartNew();
            Theme.Theme = i % 2 == 0 ? a : b;
            window.UpdateLayout();
            times.Add(sw.Elapsed.TotalMilliseconds);
            Frame(window);
        }

        times.Sort();
        return times[count / 2];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TrackTheme(List<WeakReference> into, Window window, Type type)
    {
        if (window.TryFindResource(type, out var theme) && theme is ControlTheme ct)
        {
            into.Add(new WeakReference(ct));
        }
    }

    [AvaloniaFact]
    public void No_Leaked_ControlThemes_After_50_Switches()
    {
        Theme.Theme = ThemeFamily.Aero2;
        var window = Show(new StackPanel { Children = { new Button { Content = "A" }, new ToggleButton { Content = "B" } } });
        var themes = new List<WeakReference>();
        for (var i = 0; i < 50; i++)
        {
            Theme.Theme = i % 2 == 0 ? ThemeFamily.AeroLite : ThemeFamily.Aero2;
            window.UpdateLayout();
            Track(themes, (Button)((StackPanel)window.Content!).Children[0]);
            Frame(window);
        }

        Theme.Theme = ThemeFamily.Aero2;
        window.UpdateLayout();
        Frame(window);
        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // Only the themes of the family in effect may remain alive (the last few references can be the live one).
        var alive = themes.Count(w => w.IsAlive);

        Assert.True(alive <= 2, $"{alive} ControlTheme instances still alive after 50 switches");
        window.Close();
    }

    /// <summary>
    /// Commits a frame. The compositor keeps detached visuals until the next commit, as a real render loop does each
    /// frame; headless tests have no render loop.
    /// </summary>
    private static void Frame(Window window)
    {
        Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Avalonia.Headless.HeadlessWindowExtensions.CaptureRenderedFrame(window)?.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Track(List<WeakReference> into, Button button)
    {
        if (button.TryFindResource(typeof(Button), out var theme) && theme is ControlTheme ct)
        {
            into.Add(new WeakReference(ct));
        }
    }

    [AvaloniaFact]
    public void ThemeScopes_Nest_Three_Deep()
    {
        Theme.Theme = ThemeFamily.Aero2;
        var inner = new Button { Content = "inner" };
        var middle = new Button { Content = "middle" };
        var outer = new Button { Content = "outer" };
        var root = new Button { Content = "root" };
        var tree = new StackPanel
        {
            Children =
            {
                root,
                new ThemeScope
                {
                    Theme = ThemeFamily.Luna,
                    ColorScheme = ColorSchemes.Homestead,
                    Child = new StackPanel
                    {
                        Children =
                        {
                            outer,
                            new ThemeScope
                            {
                                Theme = ThemeFamily.AeroLite,
                                Child = new StackPanel
                                {
                                    Children =
                                    {
                                        middle,
                                        new ThemeScope { Theme = ThemeFamily.Fluent, RequestedThemeVariant = ThemeVariant.Dark, Child = inner },
                                    },
                                },
                            },
                        },
                    },
                },
            },
        };
        var window = Show(tree);

        Assert.Equal(ThemeFamily.Aero2, ChromeResources.Family(root));
        Assert.Equal(ThemeFamily.Luna, ChromeResources.Family(outer));
        Assert.Equal(ColorSchemes.Homestead, outer.FindResource(ChromeResources.SchemeKey));
        Assert.Equal(ThemeFamily.AeroLite, ChromeResources.Family(middle));
        Assert.Equal(ThemeFamily.Fluent, ChromeResources.Family(inner));
        Assert.Equal(ThemeVariant.Dark, inner.ActualThemeVariant);

        // SystemColors follow the scope: Luna Olive Green's Highlight, the Fluent Dark window color.
        Assert.Equal(Color.Parse("#93A070"), (Color)outer.FindResource(outer.ActualThemeVariant, "SystemColors.HighlightColor")!);
        Assert.Equal(Color.Parse("#202020"), (Color)inner.FindResource(inner.ActualThemeVariant, "SystemColors.WindowColor")!);

        // A family switch of a scope re-templates its child only.
        var rootTemplate = root.GetVisualChildren().Single();
        var middleTemplate = middle.GetVisualChildren().Single();
        var scope = (ThemeScope)((StackPanel)((ThemeScope)tree.Children[1]).Child!).Children[1];
        scope.Theme = ThemeFamily.Aero2;
        window.UpdateLayout();
        Assert.Same(rootTemplate, root.GetVisualChildren().Single());
        Assert.NotSame(middleTemplate, middle.GetVisualChildren().Single());
        Assert.Equal(ThemeFamily.Aero2, ChromeResources.Family(middle));
        window.Close();
    }
}
