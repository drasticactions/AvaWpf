using System;
using System.Collections.Generic;
using System.Globalization;

namespace AvaWpf.Gallery.Desktop;

/// <summary>
/// Command-line switches: <c>--page Buttons --theme Luna --scheme Metallic --variant Dark --size 1200x800
/// --disable-animations --time-scale 20 --screenshot out.png</c>. With <c>--screenshot</c> the gallery renders the page with the
/// headless platform and exits. <c>--scene scene.json --out DIR</c> renders a WPF reference scene the same way
/// (<c>--scene</c> also takes a folder or a comma-separated list; <c>--shot-prefix Classic.Dark</c> names the shots).
/// </summary>
public sealed class LaunchOptions
{
    public static LaunchOptions Current { get; private set; } = new();

    public string? Page { get; init; }

    public ThemeFamily? Theme { get; init; }

    public string? Scheme { get; init; }

    public string Variant { get; init; } = "Light";

    public double Width { get; init; } = 1200;

    public double Height { get; init; } = 800;

    public bool DisableAnimations { get; init; }

    /// <summary>Multiplies every animation duration (<c>--time-scale 20</c> plays a 150 ms popup over 3 s), for review.</summary>
    public double TimeScale { get; init; } = 1;

    public string? Screenshot { get; init; }

    public string? Scene { get; init; }

    public string? Out { get; init; }

    /// <summary>
    /// The <c>&lt;family&gt;.&lt;scheme&gt;</c> part of the scene shot file names. Default: the family and scheme in
    /// effect. The WPF shooter names variants as schemes (<c>Classic.Dark</c>, <c>Fluent.HighContrast</c>).
    /// </summary>
    public string? ShotPrefix { get; init; }

    public bool Headless => Screenshot is not null || Scene is not null;

    public static void Parse(IReadOnlyList<string> args)
    {
        string? page = null, scheme = null, shot = null, scene = null, outDir = null, shotPrefix = null;
        ThemeFamily? theme = null;
        var variant = "Light";
        double w = 1200, h = 800;
        var disable = false;
        var timeScale = 1.0;
        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--page" when i + 1 < args.Count: page = args[++i]; break;
                case "--theme" when i + 1 < args.Count: theme = Enum.Parse<ThemeFamily>(args[++i], ignoreCase: true); break;
                case "--scheme" when i + 1 < args.Count: scheme = args[++i]; break;
                case "--variant" when i + 1 < args.Count: variant = args[++i]; break;
                case "--screenshot" when i + 1 < args.Count: shot = args[++i]; break;
                case "--scene" when i + 1 < args.Count: scene = args[++i]; break;
                case "--out" when i + 1 < args.Count: outDir = args[++i]; break;
                case "--shot-prefix" when i + 1 < args.Count: shotPrefix = args[++i]; break;
                case "--disable-animations": disable = true; break;
                case "--time-scale" when i + 1 < args.Count: timeScale = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--size" when i + 1 < args.Count:
                    var parts = args[++i].Split('x');
                    w = double.Parse(parts[0], CultureInfo.InvariantCulture);
                    h = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    break;
            }
        }

        Current = new LaunchOptions { Page = page, Theme = theme, Scheme = scheme, Variant = variant, Width = w, Height = h, DisableAnimations = disable, TimeScale = timeScale, Screenshot = shot, Scene = scene, Out = outDir, ShotPrefix = shotPrefix };
    }
}
