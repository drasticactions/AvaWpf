using System;
using System.Collections.Generic;
using System.Globalization;

namespace AvaWpf.WordPad.Desktop;

/// <summary>
/// Command-line switches: <c>--theme Luna --scheme Metallic --variant Dark --size 1040x620 --disable-animations
/// --screenshot out.png</c>. With <c>--screenshot</c> WordPad renders with the headless platform and exits;
/// <c>--framed</c> renders the shell in a WindowFrame, as the Browser head shows it, and <c>--picture</c> inserts a
/// sample picture, so the Picture Tools contextual tab shows.
/// </summary>
public sealed class LaunchOptions
{
    public static LaunchOptions Current { get; private set; } = new();

    public ThemeFamily? Theme { get; init; }

    public string? Scheme { get; init; }

    public string Variant { get; init; } = "Light";

    public double Width { get; init; } = 1040;

    public double Height { get; init; } = 620;

    public bool DisableAnimations { get; init; }

    public string? Screenshot { get; init; }

    public bool Framed { get; init; }

    public bool Picture { get; init; }

    public bool Headless => Screenshot is not null;

    public static void Parse(IReadOnlyList<string> args)
    {
        string? scheme = null, shot = null;
        ThemeFamily? theme = null;
        var variant = "Light";
        double w = 1040, h = 620;
        var disable = false;
        var framed = false;
        var picture = false;
        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--theme" when i + 1 < args.Count: theme = Enum.Parse<ThemeFamily>(args[++i], ignoreCase: true); break;
                case "--scheme" when i + 1 < args.Count: scheme = args[++i]; break;
                case "--variant" when i + 1 < args.Count: variant = args[++i]; break;
                case "--screenshot" when i + 1 < args.Count: shot = args[++i]; break;
                case "--disable-animations": disable = true; break;
                case "--framed": framed = true; break;
                case "--picture": picture = true; break;
                case "--size" when i + 1 < args.Count:
                    var parts = args[++i].Split('x');
                    w = double.Parse(parts[0], CultureInfo.InvariantCulture);
                    h = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    break;
            }
        }

        Current = new LaunchOptions
        {
            Theme = theme, Scheme = scheme, Variant = variant, Width = w, Height = h, DisableAnimations = disable, Screenshot = shot,
            Framed = framed, Picture = picture,
        };
    }
}
