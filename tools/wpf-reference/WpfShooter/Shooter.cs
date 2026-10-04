using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfShooter;

/// <summary>
/// Renders every scene and state for one family and scheme into <c>out/&lt;scene&gt;/&lt;family&gt;.&lt;scheme&gt;.&lt;state&gt;.png</c>,
/// with a layout dump next to each PNG.
/// </summary>
/// <remarks>
/// The scene sits in a borderless window, so hover and pressed can use real pointer input. For those states the
/// shooter writes <c>request.&lt;n&gt;.json</c> into the handshake directory. run.py moves the pointer (xdotool), answers
/// with <c>ack.&lt;n&gt;</c>, the shooter waits for the property change, captures, writes <c>done.&lt;n&gt;</c>, and run.py
/// releases the button, parks the pointer and writes <c>parked.&lt;n&gt;</c>.
/// </remarks>
public sealed class Shooter(Options opts)
{
    private const int StaticSettleMs = 150;
    private const int InputSettleMs = 600;
    private const int InputTimeoutMs = 8000;

    private static readonly JsonSerializerOptions JsonOut = new() { WriteIndented = true };

    private static readonly PropertyInfo? AlwaysShowFocusVisual =
        typeof(KeyboardNavigation).GetProperty("AlwaysShowFocusVisual", BindingFlags.NonPublic | BindingFlags.Static);

    private int _seq;
    private Window? _window;
    private AdornerDecorator? _decorator;
    private double _scale = 1.0;

    public async Task<int> RunAsync()
    {
        var scenes = LoadScenes();
        if (scenes.Count == 0)
        {
            Log.Error($"no scenes found in {opts.ScenesDir}");
            return 1;
        }

        _decorator = new AdornerDecorator();
        _window = new Window
        {
            // An empty local style keeps the merged theme's Window style (Fluent backdrop, chrome) off the host window.
            Style = new Style(typeof(Window)),
            Title = "WpfShooter",
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = opts.WindowX,
            Top = opts.WindowY,
            ShowInTaskbar = false,
            Topmost = true,
            Background = Brushes.Magenta,
            Content = _decorator,
        };
        _window.Show();
        _window.Activate();
        await Idle();
        _scale = VisualTreeHelper.GetDpi(_window).DpiScaleX;
        Log.Info($"window shown, dpi scale {_scale}");
        WriteSystemDump();

        var failures = 0;
        foreach (var scene in scenes)
        {
            foreach (var state in scene.States)
            {
                try
                {
                    await ShootAsync(scene, state);
                }
                catch (Exception e)
                {
                    failures++;
                    Log.Error($"{scene.Name}/{state.Name}: {e}");
                }
            }
        }

        _window.Close();
        return failures == 0 ? 0 : 4;
    }

    private List<SceneDefinition> LoadScenes()
    {
        var files = Directory.GetFiles(opts.ScenesDir, "*.json").OrderBy(f => f, StringComparer.Ordinal);
        var list = new List<SceneDefinition>();
        foreach (var f in files)
        {
            var scene = SceneDefinition.Load(f);
            if (opts.Scenes.Count == 0 || opts.Scenes.Contains(scene.Name))
            {
                list.Add(scene);
            }
        }

        return list;
    }

    private string BaseName(string state)
    {
        var suffix = Math.Abs(_scale - 1.0) < 0.01 ? string.Empty : "@" + _scale.ToString("0.##", CultureInfo.InvariantCulture) + "x";
        return $"{opts.Family}.{opts.Scheme}.{state}{suffix}";
    }

    private async Task ShootAsync(SceneDefinition scene, SceneState state)
    {
        if (state.Input != InputKind.None && opts.IpcDir == null)
        {
            Log.Warn($"{scene.Name}/{state.Name}: needs pointer input, but no --ipc directory; skipped");
            return;
        }

        var built = SceneBuilder.Build(scene, opts.Surface);
        built.Host.Focusable = true;
        built.Host.FocusVisualStyle = null;
        SceneBuilder.ApplyState(built, state);
        _decorator!.Child = built.Host;
        await Idle();

        // Focus visuals: WPF shows them when SystemParameters.KeyboardCues is on or the keyboard was the last input.
        // The focused state uses Keyboard.Focus, so it turns WPF's internal "always show" switch on; every other
        // state turns it off, as with mouse use (a click focuses a button without a focus rectangle).
        AlwaysShowFocusVisual?.SetValue(null, state.Focus);

        // Keep keyboard focus inside the scene so default buttons show as defaulted; the focused state moves it to the target.
        var target = built.Target;
        Keyboard.Focus(state.Focus ? target : built.Host);
        await Idle();
        if (state.Input != InputKind.Hover && target.IsMouseOver && !await WaitFor(() => !target.IsMouseOver, 2000))
        {
            Log.Warn($"{scene.Name}/{state.Name}: the pointer is still over the control");
        }

        var outDir = Path.Combine(opts.OutDir, scene.Name);
        Directory.CreateDirectory(outDir);
        var baseName = BaseName(state.Name);
        var layoutPath = Path.Combine(outDir, baseName + ".json");

        bool? reached = null;
        var seq = 0;
        if (state.Input != InputKind.None)
        {
            // run.py takes the element center from this first dump.
            WriteLayout(layoutPath, scene, state, built, null);
            seq = ++_seq;
            var request = new JsonObject
            {
                ["seq"] = seq,
                ["action"] = state.Input == InputKind.Hover ? "hover" : "press",
                ["layout"] = Path.GetFullPath(layoutPath),
                ["target"] = BuiltScene.TargetName,
            };
            WriteAtomic(Path.Combine(opts.IpcDir!, $"request.{seq}.json"), request.ToJsonString());
            await WaitFile($"ack.{seq}");
            reached = await WaitFor(() => state.Input == InputKind.Hover ? target.IsMouseOver : IsPressed(target), InputTimeoutMs);
            if (reached == false)
            {
                Log.Warn($"{scene.Name}/{state.Name}: {state.Input} not reached within {InputTimeoutMs} ms; capturing anyway");
            }
        }

        await Task.Delay(state.Input != InputKind.None ? InputSettleMs : StaticSettleMs);
        await Idle();

        var png = Path.Combine(outDir, baseName + ".png");
        Capture(png);
        WriteLayout(layoutPath, scene, state, built, reached);
        Log.Info($"wrote {png}");

        if (seq != 0)
        {
            WriteAtomic(Path.Combine(opts.IpcDir!, $"done.{seq}"), "done");
            await WaitFile($"parked.{seq}");
            await Idle();
        }
    }

    private static bool IsPressed(FrameworkElement fe) => fe switch
    {
        ButtonBase b => b.IsPressed,
        _ => fe.IsMouseOver && Mouse.LeftButton == MouseButtonState.Pressed,
    };

    private static async Task<bool> WaitFor(Func<bool> condition, int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs)
            {
                return false;
            }

            await Task.Delay(20);
        }

        return true;
    }

    private async Task WaitFile(string name)
    {
        var path = Path.Combine(opts.IpcDir!, name);
        var sw = Stopwatch.StartNew();
        while (!File.Exists(path))
        {
            if (sw.ElapsedMilliseconds > 30000)
            {
                throw new TimeoutException($"run.py did not answer with {name}");
            }

            await Task.Delay(15);
        }
    }

    private static void WriteAtomic(string path, string text)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, text);
        File.Move(tmp, path, overwrite: true);
    }

    private static async Task Idle()
    {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        await Dispatcher.Yield(DispatcherPriority.ContextIdle);
    }

    private void Capture(string path)
    {
        var host = (FrameworkElement)_decorator!.Child;
        var w = (int)Math.Ceiling(host.ActualWidth * _scale);
        var h = (int)Math.Ceiling(host.ActualHeight * _scale);
        var rtb = new RenderTargetBitmap(w, h, 96 * _scale, 96 * _scale, PixelFormats.Pbgra32);

        // The decorator holds the scene and its adorner layer (focus visuals), both at (0,0).
        rtb.Render(_decorator);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path);
        enc.Save(fs);
    }

    private void WriteLayout(string path, SceneDefinition scene, SceneState state, BuiltScene built, bool? reached)
    {
        var elements = new JsonArray();
        Walk(built.Host, built.Host, elements);
        var o = new JsonObject
        {
            ["scene"] = scene.Name,
            ["family"] = opts.Family,
            ["scheme"] = opts.Scheme,
            ["state"] = state.Name,
            ["scale"] = _scale,
            ["width"] = built.Host.ActualWidth,
            ["height"] = built.Host.ActualHeight,
            ["target"] = BuiltScene.TargetName,
            ["control"] = scene.Control,
            ["input"] = state.Input.ToString().ToLowerInvariant(),
            ["inputReached"] = reached,
            ["focused"] = Keyboard.FocusedElement is FrameworkElement f ? f.Name : null,
            ["elements"] = elements,
        };
        File.WriteAllText(path, o.ToJsonString(JsonOut));
    }

    /// <summary>Records every named element and template part: bounds in DIPs relative to the scene, and in screen pixels.</summary>
    private static void Walk(DependencyObject node, FrameworkElement host, JsonArray into)
    {
        if (node is FrameworkElement fe && !string.IsNullOrEmpty(fe.Name) && fe.IsVisible)
        {
            var r = fe.TransformToAncestor(host).TransformBounds(new Rect(0, 0, fe.ActualWidth, fe.ActualHeight));
            var tl = fe.PointToScreen(new Point(0, 0));
            var br = fe.PointToScreen(new Point(fe.ActualWidth, fe.ActualHeight));
            var owner = fe.TemplatedParent as FrameworkElement;
            into.Add(new JsonObject
            {
                ["id"] = owner != null ? $"{(string.IsNullOrEmpty(owner.Name) ? owner.GetType().Name : owner.Name)}/{fe.Name}" : fe.Name,
                ["name"] = fe.Name,
                ["type"] = fe.GetType().Name,
                ["templatePart"] = owner != null,
                ["x"] = Round(r.X),
                ["y"] = Round(r.Y),
                ["width"] = Round(r.Width),
                ["height"] = Round(r.Height),
                ["screen"] = new JsonObject
                {
                    ["x"] = Round(tl.X),
                    ["y"] = Round(tl.Y),
                    ["width"] = Round(br.X - tl.X),
                    ["height"] = Round(br.Y - tl.Y),
                    ["centerX"] = (int)Math.Round((tl.X + br.X) / 2),
                    ["centerY"] = (int)Math.Round((tl.Y + br.Y) / 2),
                },
            });
        }

        var n = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < n; i++)
        {
            Walk(VisualTreeHelper.GetChild(node, i), host, into);
        }
    }

    private static double Round(double v) => Math.Round(v, 3);

    /// <summary>
    /// What WPF actually read from the Wine prefix: SystemColors, SystemFonts (and the font file each face resolved to)
    /// and a few SystemParameters. run.py compares the colors with the snapshot it wrote.
    /// </summary>
    private void WriteSystemDump()
    {
        var colors = new JsonObject();
        foreach (var p in typeof(SystemColors).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(Color)).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var c = (Color)p.GetValue(null)!;
            colors[p.Name.EndsWith("Color", StringComparison.Ordinal) ? p.Name[..^5] : p.Name] = $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        var fonts = new JsonObject();
        foreach (var (name, family, size, weight) in new[]
        {
            ("Message", SystemFonts.MessageFontFamily, SystemFonts.MessageFontSize, SystemFonts.MessageFontWeight),
            ("Menu", SystemFonts.MenuFontFamily, SystemFonts.MenuFontSize, SystemFonts.MenuFontWeight),
            ("Status", SystemFonts.StatusFontFamily, SystemFonts.StatusFontSize, SystemFonts.StatusFontWeight),
            ("Caption", SystemFonts.CaptionFontFamily, SystemFonts.CaptionFontSize, SystemFonts.CaptionFontWeight),
            ("SmallCaption", SystemFonts.SmallCaptionFontFamily, SystemFonts.SmallCaptionFontSize, SystemFonts.SmallCaptionFontWeight),
            ("Icon", SystemFonts.IconFontFamily, SystemFonts.IconFontSize, SystemFonts.IconFontWeight),
        })
        {
            string? file = null;
            if (new Typeface(family, FontStyles.Normal, weight, FontStretches.Normal).TryGetGlyphTypeface(out var gt))
            {
                file = gt.FontUri?.ToString();
            }

            fonts[name] = new JsonObject
            {
                ["family"] = family.Source,
                ["size"] = size,
                ["weight"] = weight.ToOpenTypeWeight(),
                ["resolvedFile"] = file,
            };
        }

        // How the faces the themes name resolve in the prefix (Selawik and the glyph font are substitutes).
        var faces = new JsonObject();
        foreach (var face in new[] { "Segoe UI", "Segoe Fluent Icons", "Tahoma", "MS Sans Serif", "Selawik" })
        {
            faces[face] = new Typeface(new FontFamily(face), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal).TryGetGlyphTypeface(out var g)
                ? g.FontUri?.ToString()
                : null;
        }

        var dict = Application.Current.Resources.MergedDictionaries.FirstOrDefault()?.Source?.ToString();
        var o = new JsonObject
        {
            ["family"] = opts.Family,
            ["scheme"] = opts.Scheme,
            ["dictionary"] = dict,
            ["scale"] = _scale,
            ["colors"] = colors,
            ["fonts"] = fonts,
            ["faces"] = faces,
            ["parameters"] = new JsonObject
            {
                ["HighContrast"] = SystemParameters.HighContrast,
                ["KeyboardCues"] = SystemParameters.KeyboardCues,
                ["VerticalScrollBarWidth"] = SystemParameters.VerticalScrollBarWidth,
                ["HorizontalScrollBarHeight"] = SystemParameters.HorizontalScrollBarHeight,
                ["CaptionHeight"] = SystemParameters.CaptionHeight,
                ["MenuHeight"] = SystemParameters.MenuHeight,
                ["DropShadow"] = SystemParameters.DropShadow,
                ["UxThemeName"] = GetUxThemeName(),
            },
        };
        var dir = Path.Combine(opts.OutDir, "_system");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, BaseName("system") + ".json"), o.ToJsonString(JsonOut));
    }

    private static string? GetUxThemeName()
    {
        try
        {
            var t = typeof(FrameworkElement).Assembly.GetType("MS.Win32.UxThemeWrapper");
            return t?.GetProperty("ThemeName", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null)?.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
