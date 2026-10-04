using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.VisualTree;

namespace AvaWpf.Gallery.Desktop;

/// <summary>
/// Renders a WPF reference scene (<c>tools/wpf-reference/scenes/*.json</c>) in AvaWpf, to
/// <c>&lt;out&gt;/&lt;scene&gt;/&lt;family&gt;.&lt;scheme&gt;.&lt;state&gt;.png</c>, for the side-by-side report.
/// </summary>
internal static class SceneRenderer
{
    private static readonly bool s_dumpAll = Environment.GetEnvironmentVariable("AVAWPF_SCENE_DUMP_ALL") == "1";

    private static Avalonia.Point Center(Window window, Control built)
    {
        var target = SceneBuilder.Target(built);
        return Avalonia.VisualExtensions.TranslatePoint(target, new Avalonia.Point(target.Bounds.Width / 2, target.Bounds.Height / 2), (Avalonia.Visual)window) ?? default;
    }

    /// <param name="scenePaths">A scene file, a folder of scene files, or a comma-separated list of either.</param>
    public static int Render(string scenePaths, string outDir, LaunchOptions o)
    {
        var code = 0;
        foreach (var item in scenePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var files = Directory.Exists(item) ? Directory.GetFiles(item, "*.json").Order(StringComparer.Ordinal).ToArray() : [item];
            foreach (var file in files)
            {
                code |= RenderOne(file, outDir, o);
            }
        }

        return code;
    }

    /// <summary>
    /// Writes the layout dump next to the shot: the bounds (in DIPs, relative to the window) of the target and of every
    /// named element inside it, like the WPF shooter's dump.
    /// </summary>
    private static void WriteLayout(Window window, Control built, string path)
    {
        var target = SceneBuilder.Target(built);
        var items = new System.Collections.Generic.List<string>();
        void Add(Visual v, string id)
        {
            if (v.TranslatePoint(default, window) is not { } p)
            {
                return;
            }

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            items.Add(string.Format(inv, "    {{ \"id\": \"{0}\", \"type\": \"{1}\", \"x\": {2}, \"y\": {3}, \"width\": {4}, \"height\": {5} }}",
                id, v.GetType().Name, Math.Round(p.X, 2), Math.Round(p.Y, 2), Math.Round(v.Bounds.Width, 2), Math.Round(v.Bounds.Height, 2)));
        }

        Add(target, "target");
        if (s_dumpAll)
        {
            Console.Error.WriteLine($"target classes: {string.Join(" ", target.Classes)} pointerover={target.IsPointerOver} bg={(target as TemplatedControl)?.Background} " +
                string.Join(" ", target.GetVisualDescendants().OfType<Border>().Select(b => $"{b.Name}:{b.Background}")) +
                $" adorners={Avalonia.Controls.Primitives.AdornerLayer.GetAdornerLayer(target)?.Children.Count} fa={target.FocusAdorner is not null}");
        }

        foreach (var v in target.GetVisualDescendants())
        {
            if (v is StyledElement { Name: { Length: > 0 } n } && v.IsEffectivelyVisible)
            {
                Add(v, n);
            }
            else if (s_dumpAll && v.IsEffectivelyVisible)
            {
                // AVAWPF_SCENE_DUMP_ALL=1: unnamed elements too, for layout debugging.
                Add(v, "");
            }
        }

        File.WriteAllText(path, "{\n  \"elements\": [\n" + string.Join(",\n", items) + "\n  ]\n}\n");
    }

    private static int RenderOne(string scenePath, string outDir, LaunchOptions o)
    {
        var scene = SceneBuilder.Load(File.ReadAllText(scenePath));
        var prefix = o.ShotPrefix ?? $"{App.Theme.ActualTheme}.{App.Theme.ActualColorScheme}";
        var name = Path.GetFileNameWithoutExtension(scenePath);
        var code = 0;
        foreach (var state in scene.States)
        {
            var content = SceneBuilder.Build(scene, state);
            var window = new Window { Width = scene.Width, Height = scene.Height, Content = content, SizeToContent = SizeToContent.Manual };
            Action<Window>? input = state switch
            {
                "hover" => w => w.MouseMove(Center(w, content)),
                "pressed" => w =>
                {
                    w.MouseMove(Center(w, content));
                    w.MouseDown(Center(w, content), Avalonia.Input.MouseButton.Left);
                },
                _ => null,
            };
            var png = Path.Combine(outDir, name, $"{prefix}.{state}.png");
            var settleMs = state is "hover" or "pressed" or "focused" ? 600 : 0;
            code |= Headless.Capture(window, png, input, w => WriteLayout(w, content, Path.ChangeExtension(png, ".json")), settleMs);
        }

        return code;
    }
}
