using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace AvaWpf.Gallery.Desktop;

/// <summary>
/// The WPF reference shots (<c>tools/wpf-reference/out</c>, or <c>AVAWPF_REFERENCE</c>) beside the live AvaWpf
/// scenes, for the family in effect. Desktop only: it reads local files.
/// </summary>
public class ReferencePage : UserControl
{
    public ReferencePage()
    {
        var root = Environment.GetEnvironmentVariable("AVAWPF_REFERENCE") ?? FindOut();
        var panel = new StackPanel { Spacing = 12 };
        Content = new StackPanel { Spacing = 8, Children = { new TextBlock { Classes = { "Subtitle" }, Text = "WPF reference" }, panel } };
        if (root is null || !Directory.Exists(root))
        {
            panel.Children.Add(new TextBlock { Text = "No reference shots. Run tools/wpf-reference/run.py first." });
            return;
        }

        var family = App.Theme.ActualTheme.ToString();
        var scenes = FindScenes();
        foreach (var dir in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            var scene = Path.GetFileName(dir);
            foreach (var png in Directory.GetFiles(dir, family + ".*.png").OrderBy(f => f, StringComparer.Ordinal))
            {
                // <family>.<scheme>.<state>[@<scale>x], where the scale may hold a dot (default@1.5x).
                var state = Path.GetFileNameWithoutExtension(png).Split('.', 3)[^1];
                var row = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 12 };
                row.Children.Add(new TextBlock { Text = $"{scene} {state}", Width = 160 });
                row.Children.Add(new Image { Source = new Bitmap(png), Stretch = Avalonia.Media.Stretch.None });
                if (scenes is not null && File.Exists(Path.Combine(scenes, scene + ".json")))
                {
                    var s = SceneBuilder.Load(File.ReadAllText(Path.Combine(scenes, scene + ".json")));
                    row.Children.Add(new Border { Width = s.Width, Height = s.Height, Child = SceneBuilder.Build(s, state.Split('@')[0]) });
                }

                panel.Children.Add(row);
            }
        }
    }

    private static string? FindOut() => FindUp(Path.Combine("tools", "wpf-reference", "out")) ?? FindUp(Path.Combine("artifacts", "wpf-reference"));

    private static string? FindScenes() => FindUp(Path.Combine("tools", "wpf-reference", "scenes"));

    private static string? FindUp(string relative)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
