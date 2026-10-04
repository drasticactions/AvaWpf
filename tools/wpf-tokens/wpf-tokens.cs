#!/usr/bin/env dotnet
// AvaWpf token extractor. The single writer of the generated token tables, the system snapshots and the generated
// test key lists. Run it after changing an input; CI runs it with --check.
//
//   dotnet run tools/wpf-tokens/wpf-tokens.cs -- [--wpf DIR] [--root DIR] [--check]
//
// --wpf (or WPF_SRC) is a dotnet/wpf checkout; the default is ../wpf next to the repository.
// --root (or AVAWPF_ROOT) is the AvaWpf repository; the default is two levels above this file.
// --check regenerates into a temporary directory and fails on any difference.
//
// Inputs:
//   $W/Themes/XAML/*.xaml                         the per-control fragments of the six classic themes
//   $W/Themes/PresentationFramework.Fluent/...    the Fluent resources and styles
//   tools/wpf-tokens/system/*.toml|json           the system snapshots (SystemColors/Parameters/Fonts)
//   tools/wpf-tokens/invented/<family>.toml       tokens for controls with no WPF counterpart
//   tools/wpf-tokens/dark/<family>.toml           hand-tuned Dark overrides
//   tools/wpf-tokens/allowlist.txt                DynamicResource keys a family may reference without defining
//   tools/gen-icons/segoe-to-fluentui.toml        Segoe glyph -> Fluent UI icon map (for Fluent glyph strings)
//
// Outputs (checked in, never edited by hand):
//   src/AvaWpf.Theme/Themes/<Family>/Tokens[.<Scheme>].<Variant>.axaml
//   src/AvaWpf.Theme/System/Snapshots.g.cs
//   tests/AvaWpf.Theme.Tests/Generated/{TokenKeys,Layer0Keys,MotionInventory}.g.cs
//   samples/AvaWpf.Gallery/Generated/TokenKeys.g.cs
//   tools/wpf-tokens/tokens/<family>.map.md       key -> WPF fragment line cross-reference
#:package Tomlyn@0.19.0
#:property PublishAot=false
#:property Nullable=enable
#:property ImplicitUsings=enable

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Tomlyn;
using Tomlyn.Model;

var cli = Cli.Parse(args);
if (cli is null)
{
    return 2;
}

if (!cli.Check)
{
    var report = Generator.Run(cli.Wpf, cli.Root, cli.Root);
    Console.WriteLine(report);
    return Generator.Errors.Count == 0 ? 0 : 1;
}

var tmp = Directory.CreateTempSubdirectory("wpf-tokens-check-").FullName;
try
{
    Generator.Run(cli.Wpf, cli.Root, tmp);
    var diffs = Generator.Compare(cli.Root, tmp);
    foreach (var d in diffs)
    {
        Console.Error.WriteLine($"wpf-tokens --check: {d} is out of date");
    }

    foreach (var e in Generator.Errors)
    {
        Console.Error.WriteLine($"wpf-tokens: error: {e}");
    }

    if (diffs.Count > 0 || Generator.Errors.Count > 0)
    {
        Console.Error.WriteLine("Run dotnet run tools/wpf-tokens/wpf-tokens.cs and commit the result.");
        return 1;
    }

    Console.WriteLine("wpf-tokens --check: up to date");
    return 0;
}
finally
{
    Directory.Delete(tmp, recursive: true);
}

// ---------------------------------------------------------------------------------------------------------------------

sealed record Cli(string Wpf, string Root, bool Check)
{
    public static Cli? Parse(string[] args)
    {
        string? wpf = Environment.GetEnvironmentVariable("WPF_SRC");
        string? root = Environment.GetEnvironmentVariable("AVAWPF_ROOT");
        var check = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--wpf": wpf = args[++i]; break;
                case "--root": root = args[++i]; break;
                case "--check": check = true; break;
                case "-h" or "--help":
                    Console.WriteLine("wpf-tokens [--wpf DIR] [--root DIR] [--check]");
                    return null;
                default:
                    Console.Error.WriteLine($"unknown argument '{args[i]}'");
                    return null;
            }
        }

        root ??= FindRoot();
        wpf ??= Path.Combine(root, "..", "wpf");
        root = Path.GetFullPath(root);
        wpf = Path.GetFullPath(wpf);
        if (!Directory.Exists(Path.Combine(wpf, "src/Microsoft.DotNet.Wpf/src/Themes/XAML")))
        {
            Console.Error.WriteLine($"no WPF checkout at {wpf}; pass --wpf or set WPF_SRC");
            return null;
        }

        return new Cli(wpf, root, check);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AvaWpf.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}

/// <summary>A WPF theme dictionary: one family and scheme, named by its fragment section marker.</summary>
sealed record ThemeId(string Family, string Scheme, string Section);

/// <summary>One generated resource: its key, the Avalonia element (without x:Key) and where it came from.</summary>
sealed class Token
{
    public required string Key { get; init; }
    public required XElement Value { get; set; }
    public required string Source { get; init; }
    public bool IsLiteral { get; init; }
}

/// <summary>One storyboard of the motion inventory.</summary>
sealed record Motion(string Family, string Fragment, string Owner, string Trigger, double DurationMs, bool Forever);

static class Generator
{
    public static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    public static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";
    public static readonly XNamespace Ava = "https://github.com/avaloniaui";
    public static readonly XNamespace AvaWpf = "https://github.com/avawpf";
    public static readonly List<string> Errors = new();
    public static readonly List<string> Warnings = new();

    public static readonly ThemeId[] Themes =
    [
        new("Aero2", "Default", "Aero2.NormalColor"),
        new("AeroLite", "Default", "AeroLite.NormalColor"),
        new("Aero", "Default", "Aero.NormalColor"),
        new("Luna", "NormalColor", "Luna.NormalColor"),
        new("Luna", "Metallic", "Luna.Metallic"),
        new("Luna", "Homestead", "Luna.Homestead"),
        new("Royale", "NormalColor", "Royale.NormalColor"),
        new("Classic", "Default", "Classic"),
    ];

    public static readonly string[] Families = ["Aero2", "AeroLite", "Aero", "Luna", "Royale", "Classic", "Fluent"];

    /// <summary>Fragments of out-of-scope controls (documents, navigation chrome, grouping).</summary>
    public static readonly HashSet<string> SkippedFragments = new(StringComparer.OrdinalIgnoreCase)
    {
        "BrowserWindow", "DocumentViewer", "NavigationWindow", "Frame", "Page", "GroupItem", "CollectionViewGroup",
    };

    private static readonly List<string> s_written = new();

    public static string Run(string wpfRoot, string root, string outRoot)
    {
        Errors.Clear();
        Warnings.Clear();
        s_written.Clear();
        var w = Path.Combine(wpfRoot, "src/Microsoft.DotNet.Wpf/src");
        var tools = Path.Combine(root, "tools/wpf-tokens");
        var glyphs = GlyphMap.Load(root);
        var allow = File.Exists(Path.Combine(tools, "allowlist.txt"))
            ? File.ReadAllLines(Path.Combine(tools, "allowlist.txt")).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith('#')).ToHashSet()
            : new HashSet<string>();

        var sections = Fragments.Split(Path.Combine(w, "Themes/XAML"));
        var familyTokens = new Dictionary<string, Dictionary<string, List<Token>>>(); // family -> scheme -> tokens
        var layer0 = new Dictionary<string, SortedDictionary<string, int>>();
        var motions = new List<Motion>();

        foreach (var theme in Themes)
        {
            var extractor = new Extractor(theme.Family, glyphs);
            foreach (var (fragment, xml, line) in sections.Where(s => s.Themes.Contains(theme.Section)).Select(s => (s.Fragment, s.Xml, s.Line)))
            {
                if (SkippedFragments.Contains(fragment))
                {
                    continue;
                }

                extractor.AddSection(fragment, xml, line);
            }

            extractor.Finish(allow);
            if (!familyTokens.TryGetValue(theme.Family, out var schemes))
            {
                familyTokens[theme.Family] = schemes = new Dictionary<string, List<Token>>();
            }

            schemes[theme.Scheme] = extractor.Tokens;
            MergeCounts(layer0, theme.Family, extractor.SystemReferences);
            if (theme.Scheme is "Default" or "NormalColor")
            {
                motions.AddRange(extractor.Motions);
            }
        }

        // Fluent.
        var fluent = new FluentExtractor(Path.Combine(w, "Themes/PresentationFramework.Fluent"), glyphs);
        fluent.Run(allow);
        MergeCounts(layer0, "Fluent", fluent.SystemReferences);
        motions.AddRange(fluent.Motions);

        var keyLists = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        var summary = new StringBuilder();

        foreach (var (family, schemes) in familyTokens)
        {
            var invented = Invented.Load(tools, family);
            var chrome = Invented.LoadChrome(tools, family);
            invented.Light.AddRange(chrome.Light);
            invented.Dark.AddRange(chrome.Dark);
            var dark = DarkOverrides.Load(tools, family);
            var primary = schemes.ContainsKey("NormalColor") ? "NormalColor" : "Default";
            // The union of the extracted keys of every scheme, before the invented and chrome tokens are appended.
            // Every scheme of a family has the same key set: a key missing from a scheme takes the primary value, or
            // the value of the first scheme that defines it.
            var union = new Dictionary<string, Token>(StringComparer.Ordinal);
            foreach (var t in schemes[primary])
            {
                union[t.Key] = t;
            }

            foreach (var (_, list) in schemes)
            {
                foreach (var t in list)
                {
                    union.TryAdd(t.Key, t);
                }
            }

            var primaryKeys = schemes[primary].Select(t => t.Key).ToHashSet();
            foreach (var (scheme, tokens) in schemes)
            {
                var keys = tokens.Select(t => t.Key).ToHashSet();
                foreach (var t in union.Values.Where(t => !keys.Contains(t.Key)).ToList())
                {
                    tokens.Add(new Token { Key = t.Key, Value = new XElement(t.Value), Source = t.Source + " (filled)", IsLiteral = t.IsLiteral });
                    Warnings.Add($"{family}.{scheme}: {t.Key} missing; filled from {(primaryKeys.Contains(t.Key) ? primary : "another scheme")}");
                }

                // The invented and chrome tokens are added to the output only: adding them to the scheme's own list
                // would make them look like extracted keys to the schemes that follow (and duplicate them there).
                var light = tokens.Concat(invented.Light).OrderBy(t => t.Key, StringComparer.Ordinal).ToList();
                var darkTokens = DarkDerivation.Derive(light, dark, invented.Dark);
                var suffix = schemes.Count > 1 ? "." + scheme : string.Empty;
                Output.WriteTokens(outRoot, family, $"Tokens{suffix}.Light.axaml", light, $"{family} {scheme}, Light");
                Output.WriteTokens(outRoot, family, $"Tokens{suffix}.Dark.axaml", darkTokens, $"{family} {scheme}, Dark (derived; see tools/wpf-tokens/dark/{family.ToLowerInvariant()}.toml)");
                foreach (var t in light)
                {
                    Set(keyLists, family).Add(t.Key);
                }
            }

            Output.WriteMap(outRoot, family, schemes[primary]);
            summary.AppendLine($"{family}: {schemes[primary].Count} tokens ({schemes[primary].Count(t => t.IsLiteral)} literals), {schemes.Count} scheme(s)");
        }

        {
            var invented = Invented.Load(tools, "Fluent");
            foreach (var (variant, tokens) in fluent.Variants)
            {
                // Each variant takes its own invented table; a missing high-contrast table falls back to Dark (the HC variant
                // inherits Dark), and a missing Dark table to Light.
                var dark = invented.Dark.Count > 0 ? invented.Dark : invented.Light;
                tokens.AddRange(variant switch
                {
                    "Dark" => dark,
                    "HighContrast" => invented.HighContrast.Count > 0 ? invented.HighContrast : dark,
                    _ => invented.Light,
                });
                var sorted = tokens.OrderBy(t => t.Key, StringComparer.Ordinal).ToList();
                Output.WriteTokens(outRoot, "Fluent", $"Tokens.{variant}.axaml", sorted, $"Fluent {variant}");
                foreach (var t in sorted)
                {
                    Set(keyLists, "Fluent").Add(t.Key);
                }
            }

            Output.WriteMap(outRoot, "Fluent", fluent.Variants["Light"]);
            summary.AppendLine($"Fluent: {fluent.Variants["Light"].Count} tokens");
        }

        var snapshots = Snapshots.Load(tools);
        Output.WriteSnapshots(outRoot, snapshots);
        Output.WriteTokenKeys(outRoot, "tests/AvaWpf.Theme.Tests/Generated/TokenKeys.g.cs", "AvaWpf.Theme.Tests", keyLists);
        Output.WriteTokenKeys(outRoot, "samples/AvaWpf.Gallery/Generated/TokenKeys.g.cs", "AvaWpf.Gallery", keyLists);
        Output.WriteLayer0(outRoot, layer0);
        Output.WriteMotions(outRoot, motions);

        foreach (var w2 in Warnings.Distinct().Take(0))
        {
            Console.Error.WriteLine("warning: " + w2);
        }

        Output.WriteText(outRoot, "tools/wpf-tokens/tokens/warnings.txt", string.Join('\n', Warnings.Distinct().OrderBy(x => x, StringComparer.Ordinal)) + "\n");
        summary.AppendLine($"{snapshots.Count} system snapshots, {motions.Count} storyboards, {Warnings.Distinct().Count()} warnings (tools/wpf-tokens/tokens/warnings.txt), {Errors.Count} errors");
        foreach (var e in Errors)
        {
            summary.AppendLine("error: " + e);
        }

        return summary.ToString();
    }

    private static SortedSet<string> Set(SortedDictionary<string, SortedSet<string>> d, string k)
    {
        if (!d.TryGetValue(k, out var s))
        {
            d[k] = s = new SortedSet<string>(StringComparer.Ordinal);
        }

        return s;
    }

    private static void MergeCounts(Dictionary<string, SortedDictionary<string, int>> into, string family, Dictionary<string, int> counts)
    {
        if (!into.TryGetValue(family, out var d))
        {
            into[family] = d = new SortedDictionary<string, int>(StringComparer.Ordinal);
        }

        foreach (var (k, v) in counts)
        {
            d[k] = Math.Max(d.GetValueOrDefault(k), v);
        }
    }

    public static void Record(string relative) => s_written.Add(relative.Replace('\\', '/'));

    public static List<string> Compare(string root, string tmp)
    {
        var diffs = new List<string>();
        foreach (var rel in s_written.Distinct())
        {
            var a = Path.Combine(root, rel);
            var b = Path.Combine(tmp, rel);
            if (!File.Exists(a) || File.ReadAllText(a) != File.ReadAllText(b))
            {
                diffs.Add(rel);
            }
        }

        // A generated token file with no generator output any more is stale too.
        var themes = Path.Combine(root, "src/AvaWpf.Theme/Themes");
        if (Directory.Exists(themes))
        {
            foreach (var f in Directory.EnumerateFiles(themes, "Tokens*.axaml*", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(root, f).Replace('\\', '/');
                if (!s_written.Contains(rel))
                {
                    diffs.Add(rel + " (no longer generated)");
                }
            }
        }

        return diffs;
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// Fragment splitting, as ThemeGenerator.pl does.

sealed record Section(string Fragment, HashSet<string> Themes, string Xml, int Line);

static class Fragments
{
    private static readonly Regex s_marker = new(@"<!--\s*\[\[(?<t>[^\]]*)\]\]\s*-->", RegexOptions.Compiled);

    public static List<Section> Split(string dir)
    {
        var result = new List<Section>();
        foreach (var file in Directory.GetFiles(dir, "*.xaml").OrderBy(f => f, StringComparer.Ordinal))
        {
            var text = File.ReadAllText(file);
            var fragment = Path.GetFileNameWithoutExtension(file);
            var matches = s_marker.Matches(text);
            for (var i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                var start = m.Index + m.Length;
                var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
                var themes = m.Groups["t"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Normalize).ToHashSet(StringComparer.Ordinal);
                var line = text.AsSpan(0, start).Count('\n') + 1;
                result.Add(new Section(fragment, themes, text[start..end], line));
            }
        }

        return result;
    }

    /// <summary>Both <c>HomeStead</c> and <c>Homestead</c> occur in the markers.</summary>
    private static string Normalize(string t) => t.Replace("HomeStead", "Homestead", StringComparison.Ordinal);

    public static XElement Parse(string xml, string fragment, int line)
    {
        const string head = "<Root xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
            "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" " +
            "xmlns:theme=\"clr-namespace:Microsoft.Windows.Themes\" " +
            "xmlns:ui=\"clr-namespace:System.Windows.Documents;assembly=PresentationUI\" " +
            "xmlns:framework=\"clr-namespace:MS.Internal;assembly=PresentationFramework\" " +
            "xmlns:base=\"clr-namespace:System.Windows;assembly=WindowsBase\" " +
            "xmlns:sys=\"clr-namespace:System;assembly=mscorlib\" " +
            "xmlns:system=\"clr-namespace:System;assembly=mscorlib\">";
        try
        {
            return XElement.Parse(head + xml + "</Root>", LoadOptions.SetLineInfo);
        }
        catch (XmlException e)
        {
            throw new InvalidOperationException($"{fragment}.xaml section at line {line}: {e.Message}", e);
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// Colors.

static class Colors
{
    private static readonly Regex s_hex = new("^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.Compiled);

    public static bool IsHex(string s) => s_hex.IsMatch(s.Trim());

    public static bool TryParse(string s, out uint argb)
    {
        s = s.Trim();
        argb = 0;
        if (s_hex.IsMatch(s))
        {
            var h = s[1..];
            if (h.Length is 3 or 4)
            {
                h = string.Concat(h.Select(c => new string(c, 2)));
            }

            if (h.Length == 6)
            {
                h = "FF" + h;
            }

            argb = uint.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return true;
        }

        var known = System.Drawing.Color.FromName(s);
        if (known.IsKnownColor && !known.IsSystemColor)
        {
            argb = (uint)known.ToArgb();
            return true;
        }

        return false;
    }

    public static string Format(uint argb) => "#" + argb.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>Normalizes a WPF color string to <c>#AARRGGBB</c>; keeps it unchanged when it is not a color.</summary>
    public static string Normalize(string s) => TryParse(s, out var c) ? Format(c) : s;

    // OKLab / OKLCH (Björn Ottosson's reference formulas).
    public static (double L, double C, double H) ToOklch(uint argb)
    {
        double r = Lin(((argb >> 16) & 0xFF) / 255.0), g = Lin(((argb >> 8) & 0xFF) / 255.0), b = Lin((argb & 0xFF) / 255.0);
        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
        var L = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
        var A = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
        var B = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
        return (L, Math.Sqrt(A * A + B * B), Math.Atan2(B, A));
    }

    public static uint FromOklch(double L, double C, double H, byte alpha)
    {
        var A = C * Math.Cos(H);
        var B = C * Math.Sin(H);
        var l = Math.Pow(L + 0.3963377774 * A + 0.2158037573 * B, 3);
        var m = Math.Pow(L - 0.1055613458 * A - 0.0638541728 * B, 3);
        var s = Math.Pow(L - 0.0894841775 * A - 1.2914855480 * B, 3);
        var r = 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s;
        var g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s;
        var b = -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s;
        return ((uint)alpha << 24) | ((uint)Byte(r) << 16) | ((uint)Byte(g) << 8) | Byte(b);
    }

    private static double Lin(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static byte Byte(double c)
    {
        c = Math.Clamp(c, 0, 1);
        c = c <= 0.0031308 ? 12.92 * c : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;
        return (byte)Math.Clamp(Math.Round(c * 255), 0, 255);
    }

    /// <summary>
    /// The derive_dark rule: light surfaces become dark (L' = 1.12 − L, clamped to [0.12, 0.32]),
    /// dark text becomes light (L' = 1 − L, at least 0.85), and saturated colors keep hue and chroma with
    /// L' = min(L, 0.62).
    /// </summary>
    public static uint DeriveDark(uint argb)
    {
        var a = (byte)(argb >> 24);
        var (l, c, h) = ToOklch(argb);

        // Translucent black and white are shadows, glosses and overlays: they darken or lighten whatever is below in
        // either variant, so they keep their color.
        if (a < 0xFF && c < 0.02 && (l < 0.1 || l > 0.95))
        {
            return argb;
        }

        double nl;
        if (c >= 0.06)
        {
            nl = Math.Min(l, 0.62);
        }
        else if (l > 0.6)
        {
            nl = Math.Clamp(1.12 - l, 0.12, 0.32);
        }
        else if (l < 0.35)
        {
            nl = Math.Max(1 - l, 0.85);
        }
        else
        {
            // Mid greys (borders, glyph strokes): mirror around the middle so they keep their contrast.
            nl = Math.Clamp(1.0 - l + 0.05, 0.35, 0.7);
        }

        return FromOklch(nl, c, h, a);
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// Fluent glyphs.

sealed class GlyphMap
{
    private readonly Dictionary<int, int> _map = new();

    public static GlyphMap Load(string root)
    {
        var g = new GlyphMap();
        var tomlPath = Path.Combine(root, "tools/gen-icons/segoe-to-fluentui.toml");
        var jsonPath = Path.Combine(root, "tools/gen-icons/upstream/FluentSystemIcons-Regular.json");
        if (!File.Exists(tomlPath) || !File.Exists(jsonPath))
        {
            return g;
        }

        var json = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(jsonPath))!;
        var model = Toml.ToModel(File.ReadAllText(tomlPath));
        foreach (var section in new[] { "wpf", "extra" })
        {
            if (model.TryGetValue(section, out var o) && o is TomlTable t)
            {
                foreach (var (cp, v) in t)
                {
                    var icon = (string)((TomlTable)v)["icon"];
                    if (json.TryGetValue("ic_fluent_" + icon, out var code))
                    {
                        g._map[int.Parse(cp, NumberStyles.HexNumber, CultureInfo.InvariantCulture)] = code;
                    }
                }
            }
        }

        return g;
    }

    /// <summary>Maps every Segoe Fluent Icons private-use character in <paramref name="s"/> to its Fluent UI codepoint.</summary>
    public string Map(string s, string where)
    {
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (ch >= '' && ch <= '')
            {
                if (_map.TryGetValue(ch, out var code))
                {
                    sb.Append(char.ConvertFromUtf32(code));
                    continue;
                }

                Generator.Warnings.Add($"{where}: Segoe glyph U+{(int)ch:X4} has no Fluent UI mapping");
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// WPF -> Avalonia conversion of resource values and markup.

static class Convert
{
    private static readonly Regex s_static = new(@"^\{x:Static\s+(?:\w+:)?(?<k>[A-Za-z]+)\.(?<n>[A-Za-z0-9]+)\}$", RegexOptions.Compiled);
    private static readonly Regex s_dynStatic = new(@"^\{(?:DynamicResource|StaticResource)\s+\{x:Static\s+(?:\w+:)?(?<k>[A-Za-z]+)\.(?<n>[A-Za-z0-9]+)\}\s*\}$", RegexOptions.Compiled);
    private static readonly Regex s_res = new(@"^\{(?:DynamicResource|StaticResource)\s+(?:ResourceKey=)?(?<n>[^{}\s]+)\s*\}$", RegexOptions.Compiled);

    /// <summary>
    /// The Avalonia key a WPF <c>{x:Static SystemColors.ControlBrushKey}</c> names: <c>SystemColors.ControlBrush</c>.
    /// </summary>
    public static string? SystemKey(string kind, string name)
    {
        if (kind is not ("SystemColors" or "SystemParameters" or "SystemFonts"))
        {
            return null;
        }

        return kind + "." + (name.EndsWith("Key", StringComparison.Ordinal) ? name[..^3] : name);
    }

    /// <summary>
    /// Converts a WPF attribute value to Avalonia. Resource references become <c>DynamicResource</c> to the
    /// family-prefixed key; system keys keep their WPF name without <c>Key</c>. Returns false for bindings and other
    /// markup that has no static meaning (the caller skips them).
    /// </summary>
    public static bool TryValue(string family, string value, Action<string> systemRef, out string result)
    {
        var v = value.Trim();
        result = v;
        Match m;
        if ((m = s_dynStatic.Match(v)).Success)
        {
            var key = SystemKey(m.Groups["k"].Value, m.Groups["n"].Value);
            if (key is null)
            {
                result = "{DynamicResource " + family + "." + m.Groups["k"].Value + "." + m.Groups["n"].Value + "}";
                return true;
            }

            systemRef(key);
            result = "{DynamicResource " + key + "}";
            return true;
        }

        if ((m = s_static.Match(v)).Success)
        {
            var key = SystemKey(m.Groups["k"].Value, m.Groups["n"].Value);
            if (key is not null)
            {
                systemRef(key);
                result = "{DynamicResource " + key + "}";
                return true;
            }

            return false;
        }

        if ((m = s_res.Match(v)).Success)
        {
            result = "{DynamicResource " + family + "." + Keys.Sanitize(m.Groups["n"].Value) + "}";
            return true;
        }

        if (v.StartsWith('{') && !v.StartsWith("{}", StringComparison.Ordinal))
        {
            return false;
        }

        result = Colors.Normalize(v);
        return true;
    }

    /// <summary>WPF relative points (<c>0.5,1</c>) become Avalonia percentages (<c>50%,100%</c>).</summary>
    public static string RelativePoint(string v)
    {
        var parts = v.Split(',', ' ').Where(p => p.Length > 0).ToArray();
        if (parts.Length != 2)
        {
            return v;
        }

        return string.Join(',', parts.Select(p => Num(double.Parse(p, CultureInfo.InvariantCulture) * 100) + "%"));
    }

    public static string Num(double d) => d.ToString("0.######", CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts a WPF resource element (brush, color, thickness, geometry, double, pen) into its Avalonia equivalent.
    /// Returns null for anything that is not a token (styles, templates, converters).
    /// </summary>
    public static XElement? Resource(string family, XElement e, Action<string> systemRef, GlyphMap glyphs, string where)
    {
        var name = e.Name.LocalName;
        switch (name)
        {
            case "SolidColorBrush":
            {
                var brush = new XElement(Generator.Ava + "SolidColorBrush");
                var color = (string?)e.Attribute("Color") ?? e.Elements().FirstOrDefault(c => c.Name.LocalName == "SolidColorBrush.Color")?.Value;
                if (color is not null)
                {
                    if (!TryValue(family, color, systemRef, out var c))
                    {
                        return null;
                    }

                    brush.SetAttributeValue("Color", c);
                }

                CopyOpacity(family, e, brush, systemRef);
                return brush;
            }

            case "LinearGradientBrush":
            case "RadialGradientBrush":
                return Gradient(family, e, systemRef, where);

            case "Color":
                return new XElement(Generator.Ava + "Color", Colors.Normalize(e.Value.Trim()));

            case "Thickness":
                return new XElement(Generator.Ava + "Thickness", e.Value.Trim());

            case "CornerRadius":
                return new XElement(Generator.Ava + "CornerRadius", e.Value.Trim());

            case "Geometry":
            case "StreamGeometry":
                return new XElement(Generator.Ava + "StreamGeometry", Geometry(e.Value.Trim()));

            case "PathGeometry":
            {
                var figures = (string?)e.Attribute("Figures") ?? PathFigures(e);
                return figures is null ? null : new XElement(Generator.Ava + "StreamGeometry", Geometry(figures));
            }

            case "Double":
                return new XElement(Generator.X + "Double", e.Value.Trim());

            case "Boolean":
                return new XElement(Generator.X + "Boolean", e.Value.Trim());

            case "String":
                return new XElement(Generator.X + "String", glyphs.Map(e.Value, where));

            case "FontFamily":
                return new XElement(Generator.Ava + "FontFamily", e.Value.Trim());

            case "Pen":
            {
                var pen = new XElement(Generator.Ava + "Pen");
                foreach (var a in e.Attributes())
                {
                    if (a.Name.LocalName is "Brush" or "Thickness" && TryValue(family, a.Value, systemRef, out var v))
                    {
                        pen.SetAttributeValue(a.Name.LocalName, v);
                    }
                }

                var inner = e.Elements().FirstOrDefault(c => c.Name.LocalName == "Pen.Brush")?.Elements().FirstOrDefault();
                if (inner is not null && Resource(family, inner, systemRef, glyphs, where) is { } b)
                {
                    pen.Add(new XElement(Generator.Ava + "Pen.Brush", b));
                }

                return pen;
            }

            case "DynamicResource":
            case "StaticResource":
            {
                // <DynamicResource x:Key="SystemAccentColor" ResourceKey="{x:Static SystemColors.AccentColorKey}"/>:
                // a key that forwards to another key. AvaWpf resolves it with a ResourceAlias at lookup time.
                var target = (string?)e.Attribute("ResourceKey");
                if (target is null || !TryValue(family, "{DynamicResource " + target + "}", systemRef, out var r))
                {
                    return null;
                }

                var m = Regex.Match(r, @"^\{DynamicResource (?<k>[^}]+)\}$");
                return m.Success ? new XElement(Generator.AvaWpf + "ResourceAlias", new XAttribute("Target", m.Groups["k"].Value)) : null;
            }

            case "DrawingBrush":
                return DrawingBrush(family, e, systemRef, glyphs, where);

            default:
                return null;
        }
    }

    /// <summary>
    /// A WPF DrawingBrush (the tiled ResizeGrip and ToolBar gripper dots) becomes an Avalonia DrawingBrush: Viewbox →
    /// SourceRect, Viewport → DestinationRect, and the DrawingGroup/GeometryDrawing tree as is.
    /// </summary>
    private static XElement? DrawingBrush(string family, XElement e, Action<string> systemRef, GlyphMap glyphs, string where)
    {
        var brush = new XElement(Generator.Ava + "DrawingBrush");
        string Rect(string v, string? units)
        {
            if (units == "Absolute")
            {
                return v;
            }

            var p = v.Split(',', ' ').Where(x => x.Length > 0).Select(x => Num(double.Parse(x, CultureInfo.InvariantCulture) * 100) + "%");
            return string.Join(',', p);
        }

        if ((string?)e.Attribute("Viewbox") is { } vb)
        {
            brush.SetAttributeValue("SourceRect", Rect(vb, (string?)e.Attribute("ViewboxUnits") ?? "RelativeToBoundingBox"));
        }

        if ((string?)e.Attribute("Viewport") is { } vp)
        {
            brush.SetAttributeValue("DestinationRect", Rect(vp, (string?)e.Attribute("ViewportUnits") ?? "RelativeToBoundingBox"));
        }

        foreach (var a in new[] { "TileMode", "Stretch", "AlignmentX", "AlignmentY" })
        {
            if ((string?)e.Attribute(a) is { } v)
            {
                brush.SetAttributeValue(a, v);
            }
        }

        var drawing = e.Elements().FirstOrDefault(c => c.Name.LocalName == "DrawingBrush.Drawing")?.Elements().FirstOrDefault();
        var converted = drawing is null ? null : Drawing(family, drawing, systemRef, glyphs, where);
        if (converted is null)
        {
            Generator.Warnings.Add($"{where}: DrawingBrush drawing not converted");
            return null;
        }

        brush.Add(new XElement(Generator.Ava + "DrawingBrush.Drawing", converted));
        return brush;
    }

    private static XElement? Drawing(string family, XElement d, Action<string> systemRef, GlyphMap glyphs, string where)
    {
        switch (d.Name.LocalName)
        {
            case "DrawingGroup":
            {
                var group = new XElement(Generator.Ava + "DrawingGroup");
                var children = d.Elements().FirstOrDefault(c => c.Name.LocalName == "DrawingGroup.Children")?.Elements() ?? d.Elements().Where(c => !c.Name.LocalName.Contains('.'));
                foreach (var c in children)
                {
                    if (Drawing(family, c, systemRef, glyphs, where) is { } x)
                    {
                        group.Add(x);
                    }
                }

                return group;
            }

            case "GeometryDrawing":
            {
                var g = new XElement(Generator.Ava + "GeometryDrawing");
                if ((string?)d.Attribute("Brush") is { } b && TryValue(family, b, systemRef, out var bv))
                {
                    g.SetAttributeValue("Brush", bv);
                }

                if ((string?)d.Attribute("Geometry") is { } geo)
                {
                    g.SetAttributeValue("Geometry", Geometry(geo));
                }

                foreach (var pe in d.Elements())
                {
                    var inner = pe.Elements().FirstOrDefault();
                    if (inner is null)
                    {
                        continue;
                    }

                    var prop = pe.Name.LocalName.Split('.').Last();
                    if (Resource(family, inner, systemRef, glyphs, where) is { } v)
                    {
                        g.Add(new XElement(Generator.Ava + ("GeometryDrawing." + prop), v));
                    }
                }

                return g;
            }

            default:
                return null;
        }
    }

    private static void CopyOpacity(string family, XElement from, XElement to, Action<string> systemRef)
    {
        if ((string?)from.Attribute("Opacity") is { } o && TryValue(family, o, systemRef, out var v))
        {
            to.SetAttributeValue("Opacity", v);
        }
    }

    private static XElement? Gradient(string family, XElement e, Action<string> systemRef, string where)
    {
        var linear = e.Name.LocalName == "LinearGradientBrush";
        var brush = new XElement(Generator.Ava + e.Name.LocalName);
        var absolute = (string?)e.Attribute("MappingMode") == "Absolute";
        string P(string v) => absolute ? v : RelativePoint(v);

        if (linear)
        {
            brush.SetAttributeValue("StartPoint", P((string?)e.Attribute("StartPoint") ?? "0,0"));
            brush.SetAttributeValue("EndPoint", P((string?)e.Attribute("EndPoint") ?? "1,1"));
        }
        else
        {
            if ((string?)e.Attribute("Center") is { } c)
            {
                brush.SetAttributeValue("Center", P(c));
            }

            if ((string?)e.Attribute("GradientOrigin") is { } go)
            {
                brush.SetAttributeValue("GradientOrigin", P(go));
            }

            foreach (var r in new[] { "RadiusX", "RadiusY" })
            {
                if ((string?)e.Attribute(r) is { } rv)
                {
                    brush.SetAttributeValue(r, absolute ? rv : Num(double.Parse(rv, CultureInfo.InvariantCulture) * 100) + "%");
                }
            }
        }

        if ((string?)e.Attribute("SpreadMethod") is { } sm)
        {
            brush.SetAttributeValue("SpreadMethod", sm);
        }

        CopyOpacity(family, e, brush, systemRef);
        var stops = e.Descendants().Where(d => d.Name.LocalName == "GradientStop").ToList();
        foreach (var s in stops)
        {
            var stop = new XElement(Generator.Ava + "GradientStop");
            var color = (string?)s.Attribute("Color") ?? "Transparent";
            if (!TryValue(family, color, systemRef, out var cv))
            {
                Generator.Warnings.Add($"{where}: gradient stop color '{color}' not converted");
                cv = "#00000000";
            }

            stop.SetAttributeValue("Color", cv);
            stop.SetAttributeValue("Offset", (string?)s.Attribute("Offset") ?? "0");
            brush.Add(stop);
        }

        // A RelativeTransform works in the brush's unit box: its CenterX/CenterY (0..1) become Avalonia's relative
        // TransformOrigin, and the transform itself is applied without a center. A plain Transform keeps its absolute
        // center inside the transform.
        var relative = e.Elements().FirstOrDefault(c => c.Name.LocalName is "LinearGradientBrush.RelativeTransform" or "RadialGradientBrush.RelativeTransform" or "LinearGradientBrush.Transform" or "RadialGradientBrush.Transform");
        if (relative?.Elements().FirstOrDefault() is { } t)
        {
            var isRelative = relative.Name.LocalName.EndsWith("RelativeTransform", StringComparison.Ordinal);
            var transform = Transform(t, ignoreCenter: isRelative);
            if (transform is not null)
            {
                brush.SetAttributeValue("Transform", transform);
                if (isRelative)
                {
                    double C(string n) => (string?)t.Attribute(n) is { } v ? double.Parse(v, CultureInfo.InvariantCulture) : 0;
                    brush.SetAttributeValue("TransformOrigin", $"{Num(C("CenterX") * 100)}%,{Num(C("CenterY") * 100)}%");
                }
            }
            else
            {
                Generator.Warnings.Add($"{where}: brush transform {t.Name.LocalName} not converted");
            }
        }

        return brush;
    }

    private static string? Transform(XElement t, bool ignoreCenter = false)
    {
        double A(string n, double d = 0) => (string?)t.Attribute(n) is { } v ? double.Parse(v, CultureInfo.InvariantCulture) : d;
        switch (t.Name.LocalName)
        {
            case "RotateTransform":
            {
                var angle = A("Angle");
                var cx = ignoreCenter ? 0 : A("CenterX");
                var cy = ignoreCenter ? 0 : A("CenterY");
                return cx == 0 && cy == 0
                    ? $"rotate({Num(angle)}deg)"
                    : $"translate({Num(cx)}px, {Num(cy)}px) rotate({Num(angle)}deg) translate({Num(-cx)}px, {Num(-cy)}px)";
            }

            case "ScaleTransform":
                return $"scale({Num(A("ScaleX", 1))}, {Num(A("ScaleY", 1))})";
            case "TranslateTransform":
                return $"translate({Num(A("X"))}px, {Num(A("Y"))}px)";
            default:
                return null;
        }
    }

    /// <summary>Path mini-language is shared by WPF and Avalonia; only spacing is normalized.</summary>
    public static string Geometry(string data) => Regex.Replace(data.Trim(), @"\s+", " ");

    private static string? PathFigures(XElement geometry)
    {
        var sb = new StringBuilder();
        foreach (var fig in geometry.Descendants().Where(d => d.Name.LocalName == "PathFigure"))
        {
            sb.Append("M ").Append((string?)fig.Attribute("StartPoint") ?? "0,0").Append(' ');
            foreach (var seg in fig.Descendants())
            {
                switch (seg.Name.LocalName)
                {
                    case "LineSegment":
                        sb.Append("L ").Append((string?)seg.Attribute("Point")).Append(' ');
                        break;
                    case "PolyLineSegment":
                        sb.Append("L ").Append((string?)seg.Attribute("Points")).Append(' ');
                        break;
                    case "BezierSegment":
                        sb.Append("C ").Append((string?)seg.Attribute("Point1")).Append(' ').Append((string?)seg.Attribute("Point2")).Append(' ').Append((string?)seg.Attribute("Point3")).Append(' ');
                        break;
                    case "ArcSegment":
                        var size = ((string?)seg.Attribute("Size") ?? "0,0").Replace(',', ' ');
                        var large = (string?)seg.Attribute("IsLargeArc") == "True" ? 1 : 0;
                        var sweep = (string?)seg.Attribute("SweepDirection") == "Clockwise" ? 1 : 0;
                        sb.Append($"A {size} {(string?)seg.Attribute("RotationAngle") ?? "0"} {large} {sweep} {(string?)seg.Attribute("Point")} ");
                        break;
                }
            }

            if ((string?)fig.Attribute("IsClosed") == "True")
            {
                sb.Append("Z ");
            }
        }

        return sb.Length == 0 ? null : sb.ToString().Trim();
    }
}

static class Keys
{
    private static readonly Regex s_bad = new("[^A-Za-z0-9_.#]", RegexOptions.Compiled);
    private static readonly Regex s_static = new(@"\{x:Static\s+(?:\w+:)?(?<n>[A-Za-z0-9.]+)\}", RegexOptions.Compiled);
    private static readonly Regex s_component = new(@"ResourceId\s*=\s*(?<n>[A-Za-z0-9_]+)", RegexOptions.Compiled);
    private static readonly Regex s_type = new(@"\{x:Type\s+(?:\w+:)?(?<n>[A-Za-z0-9_.]+)\}", RegexOptions.Compiled);

    /// <summary>Turns a WPF x:Key (a string, an x:Static key or a ComponentResourceKey) into a token key segment.</summary>
    public static string Sanitize(string key)
    {
        Match m;
        if ((m = s_component.Match(key)).Success)
        {
            key = m.Groups["n"].Value;
        }
        else if ((m = s_static.Match(key)).Success)
        {
            key = m.Groups["n"].Value;
        }
        else if ((m = s_type.Match(key)).Success)
        {
            key = m.Groups["n"].Value;
        }

        return s_bad.Replace(key, "_");
    }

    public static string? TypeName(string? target)
    {
        if (target is null)
        {
            return null;
        }

        var m = s_type.Match(target);
        var name = m.Success ? m.Groups["n"].Value : target;
        return Sanitize(name[(name.LastIndexOf(':') + 1)..]);
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// Extraction from the classic fragments.

sealed class Extractor(string family, GlyphMap glyphs)
{
    private static readonly HashSet<string> s_brushProperties = new(StringComparer.Ordinal)
    {
        "Fill", "Stroke", "Background", "BorderBrush", "Foreground", "OpacityMask", "Color", "CaretBrush",
        "SelectionBrush", "SeparatorBrush", "Brush", "SelectionTextBrush",
    };

    private static readonly HashSet<string> s_geometryProperties = new(StringComparer.Ordinal) { "Data", "Clip" };

    private static readonly HashSet<string> s_tokenElements = new(StringComparer.Ordinal)
    {
        "SolidColorBrush", "LinearGradientBrush", "RadialGradientBrush", "Color", "Thickness", "CornerRadius",
        "Geometry", "StreamGeometry", "PathGeometry", "Double", "Pen", "DrawingBrush", "Boolean", "String", "FontFamily",
        "DynamicResource",
    };

    private readonly Dictionary<string, Token> _tokens = new(StringComparer.Ordinal);
    private readonly HashSet<string> _referenced = new(StringComparer.Ordinal);
    private readonly HashSet<string> _defined = new(StringComparer.Ordinal);

    public List<Token> Tokens => _tokens.Values.ToList();

    public Dictionary<string, int> SystemReferences { get; } = new(StringComparer.Ordinal);

    public List<Motion> Motions { get; } = new();

    private void SystemRef(string key) => SystemReferences[key] = SystemReferences.GetValueOrDefault(key) + 1;

    public void AddSection(string fragment, string xml, int line)
    {
        AddRoot(fragment, Fragments.Parse(xml, fragment, line), line);
    }

    /// <summary>Adds the resources of an already parsed dictionary (the Fluent styles keep their own namespaces).</summary>
    public void AddRoot(string fragment, XElement root, int line)
    {
        CountSystemReferences(root);
        foreach (var e in root.Elements())
        {
            AddTopLevel(fragment, e, line);
        }
    }

    private void CountSystemReferences(XElement root)
    {
        foreach (var a in root.DescendantsAndSelf().SelectMany(d => d.Attributes()))
        {
            foreach (Match m in Regex.Matches(a.Value, @"System(Colors|Parameters|Fonts)\.([A-Za-z0-9]+)"))
            {
                // SystemParameters.FocusVisualStyleKey and the NavigationChrome keys name styles the theme itself
                // defines, not system values.
                if (m.Groups[2].Value.EndsWith("StyleKey", StringComparison.Ordinal))
                {
                    continue;
                }

                SystemRef(Convert.SystemKey("System" + m.Groups[1].Value, m.Groups[2].Value)!);
            }
        }
    }

    private void AddTopLevel(string fragment, XElement e, int sectionLine)
    {
        var key = (string?)e.Attribute(Generator.X + "Key");
        var where = $"{fragment}.xaml:{Line(e, sectionLine)}";
        if (key is not null && s_tokenElements.Contains(e.Name.LocalName))
        {
            var tokenKey = family + "." + Keys.Sanitize(key);
            var value = Convert.Resource(family, e, SystemRef, glyphs, where);
            if (value is not null)
            {
                Add(new Token { Key = tokenKey, Value = value, Source = where });
                _defined.Add(Keys.Sanitize(key));
            }

            return;
        }

        if (key is not null)
        {
            _defined.Add(Keys.Sanitize(key));
        }

        var owner = key is not null ? Keys.Sanitize(key) : Keys.TypeName((string?)e.Attribute("TargetType")) ?? e.Name.LocalName;
        if (e.Name.LocalName is "Style" or "ControlTemplate" or "DataTemplate")
        {
            Walk(fragment, owner, e, sectionLine);
        }
    }

    private void Walk(string fragment, string owner, XElement scope, int sectionLine)
    {
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        var prefix = $"{family}.{fragment}.{owner}";

        foreach (var e in scope.Descendants())
        {
            var local = e.Name.LocalName;
            if (local.Contains('.'))
            {
                continue;
            }

            // Nested keyed resources (Style.Resources, ControlTemplate.Resources).
            if (e.Attribute(Generator.X + "Key") is { } nestedKey && e.Parent?.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal) == true)
            {
                if (s_tokenElements.Contains(local) && Convert.Resource(family, e, SystemRef, glyphs, $"{fragment}.xaml:{Line(e, sectionLine)}") is { } nv)
                {
                    Add(new Token { Key = family + "." + Keys.Sanitize(nestedKey.Value), Value = nv, Source = $"{fragment}.xaml:{Line(e, sectionLine)}" });
                    _defined.Add(Keys.Sanitize(nestedKey.Value));
                }
                else
                {
                    _defined.Add(Keys.Sanitize(nestedKey.Value));
                }

                continue;
            }

            if (local is "Storyboard")
            {
                AddMotion(fragment, owner, e);
            }

            foreach (var a in e.Attributes())
            {
                if (a.IsNamespaceDeclaration)
                {
                    continue;
                }

                foreach (Match m in Regex.Matches(a.Value, @"\{(?:DynamicResource|StaticResource)\s+(?:ResourceKey=)?(?<n>[A-Za-z][A-Za-z0-9_.]*)\s*\}"))
                {
                    _referenced.Add(m.Groups["n"].Value);
                }
            }

            if (local == "Setter")
            {
                AddSetter(fragment, prefix, e, sectionLine);
                continue;
            }

            if (IsInsideSetterOrResource(e) || IsGeometryPart(e))
            {
                continue;
            }

            var elementName = (string?)e.Attribute(Generator.X + "Name") ?? (string?)e.Attribute("Name");
            if (elementName is null)
            {
                counters[local] = counters.GetValueOrDefault(local) + 1;
            }

            var id = elementName ?? $"{local}#{counters.GetValueOrDefault(local)}";

            foreach (var a in e.Attributes())
            {
                var prop = a.Name.LocalName;
                var where = $"{fragment}.xaml:{Line(e, sectionLine)}";
                if (s_brushProperties.Contains(prop) && IsLiteralColor(a.Value))
                {
                    Add(new Token
                    {
                        Key = $"{prefix}.{id}.{prop}",
                        Value = new XElement(Generator.Ava + (prop == "Color" ? "Color" : "SolidColorBrush"), prop == "Color" ? Colors.Normalize(a.Value) : null, prop == "Color" ? null : new XAttribute("Color", Colors.Normalize(a.Value))),
                        Source = where,
                        IsLiteral = true,
                    });
                }
                else if (s_geometryProperties.Contains(prop) && IsGeometry(a.Value))
                {
                    Add(new Token { Key = $"{prefix}.{id}.{prop}", Value = new XElement(Generator.Ava + "StreamGeometry", Convert.Geometry(a.Value)), Source = where, IsLiteral = true });
                }
                else if (prop == "Points" && local is "Polygon" or "Polyline")
                {
                    Add(new Token { Key = $"{prefix}.{id}.{prop}", Value = new XElement(Generator.X + "String", a.Value.Trim()), Source = where, IsLiteral = true });
                }
            }

            // Property elements holding a brush or geometry: <Border.Background><LinearGradientBrush .../></Border.Background>.
            foreach (var pe in e.Elements().Where(c => c.Name.LocalName.StartsWith(local + ".", StringComparison.Ordinal)))
            {
                var prop = pe.Name.LocalName[(local.Length + 1)..];
                var inner = pe.Elements().FirstOrDefault();
                if (inner is null || !s_tokenElements.Contains(inner.Name.LocalName))
                {
                    continue;
                }

                var where = $"{fragment}.xaml:{Line(inner, sectionLine)}";
                if (Convert.Resource(family, inner, SystemRef, glyphs, where) is { } v)
                {
                    Add(new Token { Key = $"{prefix}.{id}.{prop}", Value = v, Source = where, IsLiteral = true });
                }
            }
        }
    }

    private void AddSetter(string fragment, string prefix, XElement setter, int sectionLine)
    {
        var prop = ((string?)setter.Attribute("Property") ?? "?").Split('.').Last();
        var target = (string?)setter.Attribute("TargetName") ?? "Self";
        var trigger = TriggerSignature(setter);
        var key = trigger is null ? $"{prefix}.Setter.{target}.{prop}" : $"{prefix}.{target}.{prop}.{trigger}";
        var where = $"{fragment}.xaml:{Line(setter, sectionLine)}";
        var value = (string?)setter.Attribute("Value");
        if (value is not null)
        {
            if (s_brushProperties.Contains(prop) && IsLiteralColor(value))
            {
                Add(new Token { Key = key, Value = new XElement(Generator.Ava + "SolidColorBrush", new XAttribute("Color", Colors.Normalize(value))), Source = where, IsLiteral = true });
            }
            else if (s_geometryProperties.Contains(prop) && IsGeometry(value))
            {
                Add(new Token { Key = key, Value = new XElement(Generator.Ava + "StreamGeometry", Convert.Geometry(value)), Source = where, IsLiteral = true });
            }

            return;
        }

        var inner = setter.Elements().FirstOrDefault(c => c.Name.LocalName == "Setter.Value")?.Elements().FirstOrDefault();
        if (inner is not null && s_tokenElements.Contains(inner.Name.LocalName) && Convert.Resource(family, inner, SystemRef, glyphs, where) is { } v)
        {
            Add(new Token { Key = key, Value = v, Source = where, IsLiteral = true });
        }
    }

    private static string? TriggerSignature(XElement setter)
    {
        var parts = new List<string>();
        for (var p = setter.Parent; p is not null; p = p.Parent)
        {
            switch (p.Name.LocalName)
            {
                case "Trigger":
                    parts.Insert(0, Cond((string?)p.Attribute("Property"), (string?)p.Attribute("Value")));
                    break;
                case "MultiTrigger":
                case "MultiDataTrigger":
                    parts.Insert(0, string.Join("_", p.Descendants().Where(d => d.Name.LocalName == "Condition")
                        .Select(c => Cond((string?)c.Attribute("Property") ?? BindingPath((string?)c.Attribute("Binding")), (string?)c.Attribute("Value")))));
                    break;
                case "DataTrigger":
                    parts.Insert(0, Cond(BindingPath((string?)p.Attribute("Binding")), (string?)p.Attribute("Value")));
                    break;
                case "Style" or "ControlTemplate" or "DataTemplate":
                    return parts.Count == 0 ? null : string.Join(".", parts);
            }
        }

        return parts.Count == 0 ? null : string.Join(".", parts);
    }

    private static string BindingPath(string? binding)
    {
        if (binding is null)
        {
            return "Binding";
        }

        var m = Regex.Match(binding, @"(?:Path=)?(?<p>[A-Za-z][A-Za-z0-9.()]*)(?=[,}])");
        var path = Regex.Match(binding, @"Path=(?<p>[^,}]+)");
        var s = path.Success ? path.Groups["p"].Value : (m.Success ? m.Groups["p"].Value : "Binding");
        return s.Replace("Binding ", string.Empty, StringComparison.Ordinal);
    }

    private static string Cond(string? prop, string? value)
    {
        var p = (prop ?? "?").Split('.').Last().Trim('(', ')');
        return Regex.Replace($"{p}_{value}", "[^A-Za-z0-9_]", string.Empty);
    }

    private void AddMotion(string fragment, string owner, XElement storyboard)
    {
        string trigger;
        var state = storyboard.Ancestors().FirstOrDefault(a => a.Name.LocalName is "VisualState" or "VisualTransition" or "EventTrigger" or "Trigger" or "MultiTrigger" or "DataTrigger");
        switch (state?.Name.LocalName)
        {
            case "VisualState":
                var group = state.Ancestors().FirstOrDefault(a => a.Name.LocalName == "VisualStateGroup");
                trigger = $"VisualState:{(string?)group?.Attribute(Generator.X + "Name")}.{(string?)state.Attribute(Generator.X + "Name")}";
                break;
            case "VisualTransition":
                trigger = $"VisualTransition:{(string?)state.Attribute("From") ?? "*"}->{(string?)state.Attribute("To") ?? "*"}";
                break;
            case "EventTrigger":
                trigger = $"EventTrigger:{((string?)state.Attribute("RoutedEvent") ?? "?").Split('.').Last()}";
                break;
            case null:
                trigger = "Resource:" + ((string?)storyboard.Attribute(Generator.X + "Key") ?? "?");
                break;
            default:
                var enter = storyboard.Ancestors().Any(a => a.Name.LocalName.EndsWith(".EnterActions", StringComparison.Ordinal)) ? "Enter" : "Exit";
                trigger = $"Trigger:{Cond((string?)state.Attribute("Property") ?? BindingPath((string?)state.Attribute("Binding")), (string?)state.Attribute("Value"))}.{enter}";
                break;
        }

        Motions.Add(new Motion(family, fragment, owner, trigger, StoryboardDuration(storyboard), (string?)storyboard.Attribute("RepeatBehavior") == "Forever"));
    }

    public static double StoryboardDuration(XElement storyboard)
    {
        double max = 0;
        foreach (var e in storyboard.DescendantsAndSelf())
        {
            var begin = Time((string?)e.Attribute("BeginTime"));
            foreach (var attr in new[] { "Duration", "KeyTime" })
            {
                if ((string?)e.Attribute(attr) is { } t)
                {
                    max = Math.Max(max, begin + Time(t));
                }
            }
        }

        return Math.Round(max, 3);
    }

    private static double Time(string? t)
    {
        if (string.IsNullOrWhiteSpace(t) || t is "Automatic" or "Forever" || t.StartsWith('{'))
        {
            return 0;
        }

        if (!t.Contains(':'))
        {
            return double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ? s * 1000 : 0;
        }

        return TimeSpan.TryParse(t, CultureInfo.InvariantCulture, out var ts) ? ts.TotalMilliseconds : 0;
    }

    private static bool IsInsideSetterOrResource(XElement e) =>
        e.Ancestors().Any(a => a.Name.LocalName is "Setter.Value" || a.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal)) &&
        !e.Ancestors().Any(a => a.Name.LocalName == "ControlTemplate" && a.Ancestors().Any(x => x.Name.LocalName == "Setter.Value") && a.Ancestors().TakeWhile(x => x.Name.LocalName != "Setter.Value").All(x => !x.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal)));

    /// <summary>Elements that are themselves part of a brush or geometry value (stops, figures, segments).</summary>
    private static bool IsGeometryPart(XElement e) =>
        e.Ancestors().Any(a => s_tokenElements.Contains(a.Name.LocalName) || a.Name.LocalName is "Storyboard" or "VisualStateManager.VisualStateGroups");

    private static bool IsLiteralColor(string v)
    {
        v = v.Trim();
        if (v.StartsWith('{'))
        {
            return false;
        }

        return Colors.IsHex(v) || (v != "Transparent" && Colors.TryParse(v, out _));
    }

    private static bool IsGeometry(string v) => !v.TrimStart().StartsWith('{') && Regex.IsMatch(v.TrimStart(), "^(F[01] )?[MmFf]");

    private static int Line(XElement e, int sectionLine) => ((IXmlLineInfo)e).HasLineInfo() ? sectionLine + ((IXmlLineInfo)e).LineNumber - 1 : sectionLine;

    private void Add(Token t)
    {
        if (_tokens.ContainsKey(t.Key))
        {
            // Two literals in the same slot (e.g. two triggers with the same signature): number them.
            var i = 2;
            while (_tokens.ContainsKey($"{t.Key}#{i}"))
            {
                i++;
            }

            _tokens[$"{t.Key}#{i}"] = new Token { Key = $"{t.Key}#{i}", Value = t.Value, Source = t.Source, IsLiteral = t.IsLiteral };
            return;
        }

        _tokens[t.Key] = t;
    }

    /// <summary>Marks keys that another dictionary of the family defines (the Fluent theme dictionaries).</summary>
    public void Define(IEnumerable<string> keys)
    {
        foreach (var k in keys)
        {
            _defined.Add(k);
        }
    }

    public void Finish(HashSet<string> allow)
    {
        foreach (var r in _referenced)
        {
            if (!_defined.Contains(Keys.Sanitize(r)) && !allow.Contains(r) && !allow.Contains(family + "." + r))
            {
                Generator.Warnings.Add($"{family}: DynamicResource/StaticResource '{r}' is not defined by the family");
            }
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// Fluent.

sealed class FluentExtractor(string dir, GlyphMap glyphs)
{
    private readonly HashSet<string> _otherKeys = new(StringComparer.Ordinal);

    public Dictionary<string, List<Token>> Variants { get; } = new() { ["Light"] = new(), ["Dark"] = new(), ["HighContrast"] = new() };

    public Dictionary<string, int> SystemReferences { get; } = new(StringComparer.Ordinal);

    public List<Motion> Motions { get; } = new();

    private void SystemRef(string key) => SystemReferences[key] = SystemReferences.GetValueOrDefault(key) + 1;

    public void Run(HashSet<string> allow)
    {
        foreach (var (variant, file) in new[] { ("Light", "Light"), ("Dark", "Dark"), ("HighContrast", "HC") })
        {
            AddDictionary(Path.Combine(dir, "Resources/Theme", file + ".xaml"), Variants[variant]);
        }

        var shared = new List<Token>();
        foreach (var f in new[] { "Variables", "Fonts", "StaticColors", "DefaultFocusVisualStyle", "DefaultContextMenu" })
        {
            var p = Path.Combine(dir, "Resources", f + ".xaml");
            if (File.Exists(p))
            {
                AddDictionary(p, shared);
            }
        }

        var extractor = new Extractor("Fluent", glyphs);
        extractor.Define(_otherKeys);
        foreach (var f in Directory.GetFiles(Path.Combine(dir, "Styles"), "*.xaml").OrderBy(f => f, StringComparer.Ordinal))
        {
            var fragment = Path.GetFileNameWithoutExtension(f);
            if (Generator.SkippedFragments.Contains(fragment) || fragment is "RichTextBox")
            {
                continue;
            }

            extractor.AddRoot(fragment, XElement.Parse(File.ReadAllText(f), LoadOptions.SetLineInfo), 1);
        }

        extractor.Finish(allow);
        foreach (var (k, v) in extractor.SystemReferences)
        {
            SystemReferences[k] = SystemReferences.GetValueOrDefault(k) + v;
        }

        Motions.AddRange(extractor.Motions);
        shared.AddRange(extractor.Tokens);

        // The bundled glyph subset replaces Segoe Fluent Icons on every platform.
        foreach (var t in shared.Where(t => t.Key == "Fluent.SymbolThemeFontFamily").ToList())
        {
            t.Value = new XElement(Generator.Ava + "FontFamily", "fonts:AvaWpf#AvaWpf Fluent Glyphs");
        }

        foreach (var t in shared.Where(t => t.Key == "Fluent.ContentControlThemeFontFamily").ToList())
        {
            t.Value = new XElement(Generator.Ava + "FontFamily", "Segoe UI Variable, Segoe UI, fonts:AvaWpf#Selawik, sans-serif");
        }

        foreach (var list in Variants.Values)
        {
            var keys = list.Select(t => t.Key).ToHashSet();
            list.AddRange(shared.Where(t => !keys.Contains(t.Key)).Select(t => new Token { Key = t.Key, Value = new XElement(t.Value), Source = t.Source, IsLiteral = t.IsLiteral }));
        }
    }

    private void AddDictionary(string path, List<Token> into)
    {
        var root = XElement.Parse(File.ReadAllText(path), LoadOptions.SetLineInfo);
        var name = Path.GetFileName(path);
        foreach (var e in root.Elements())
        {
            var key = (string?)e.Attribute(Generator.X + "Key");
            if (key is null)
            {
                continue;
            }

            _otherKeys.Add(Keys.Sanitize(key));
            var where = $"Fluent/{name}:{((IXmlLineInfo)e).LineNumber}";
            if (Convert.Resource("Fluent", e, SystemRef, glyphs, where) is { } v)
            {
                into.Add(new Token { Key = "Fluent." + Keys.Sanitize(key), Value = v, Source = where });
            }
        }
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// Dark derivation, invented tokens and overrides.

static class DarkDerivation
{
    public static List<Token> Derive(List<Token> light, Dictionary<string, string> overrides, List<Token> inventedDark)
    {
        var inventedKeys = inventedDark.ToDictionary(t => t.Key, t => t);
        var result = new List<Token>(light.Count);
        foreach (var t in light)
        {
            if (inventedKeys.TryGetValue(t.Key, out var hand))
            {
                result.Add(hand);
                continue;
            }

            var value = new XElement(t.Value);
            if (overrides.TryGetValue(t.Key, out var o))
            {
                Override(value, o);
            }
            else
            {
                Map(value);
            }

            result.Add(new Token { Key = t.Key, Value = value, Source = t.Source, IsLiteral = t.IsLiteral });
        }

        return result;
    }

    /// <summary>An override is a single color (applied to every stop) or a list of stop colors separated by spaces.</summary>
    private static void Override(XElement value, string spec)
    {
        var colors = spec.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Colors.Normalize).ToArray();
        switch (value.Name.LocalName)
        {
            case "Color":
                value.Value = colors[0];
                break;
            case "SolidColorBrush":
                value.SetAttributeValue("Color", colors[0]);
                break;
            default:
                var stops = value.Elements().Where(e => e.Name.LocalName == "GradientStop").ToList();
                for (var i = 0; i < stops.Count; i++)
                {
                    stops[i].SetAttributeValue("Color", colors[Math.Min(i, colors.Length - 1)]);
                }

                break;
        }
    }

    private static void Map(XElement value)
    {
        if (value.Name.LocalName == "Color" && Colors.TryParse(value.Value, out var c))
        {
            value.Value = Colors.Format(Colors.DeriveDark(c));
            return;
        }

        foreach (var e in value.DescendantsAndSelf())
        {
            if ((string?)e.Attribute("Color") is { } s && Colors.TryParse(s, out var cc))
            {
                e.SetAttributeValue("Color", Colors.Format(Colors.DeriveDark(cc)));
            }
        }
    }
}

static class DarkOverrides
{
    public static Dictionary<string, string> Load(string tools, string family)
    {
        var path = Path.Combine(tools, "dark", family.ToLowerInvariant() + ".toml");
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return d;
        }

        foreach (var (k, v) in Toml.ToModel(File.ReadAllText(path)))
        {
            if (v is string s)
            {
                d[k.StartsWith(family + ".", StringComparison.Ordinal) ? k : family + "." + k] = s;
            }
        }

        return d;
    }
}

sealed record InventedSet(List<Token> Light, List<Token> Dark)
{
    /// <summary>The <c>[highcontrast]</c> table (Fluent only); empty means Dark is used.</summary>
    public List<Token> HighContrast { get; } = new();
}

static class Invented
{
    /// <summary>
    /// Reads <c>invented/&lt;family&gt;.toml</c>: <c>[light]</c> and <c>[dark]</c> tables of
    /// <c>Control.Part.State = "value"</c>. A value is a color (a SolidColorBrush token, or a Color token when the key
    /// ends in <c>Color</c>), a stop list <c>"linear 0,0 0,1 #A@0 #B@1"</c>, a thickness/corner radius
    /// <c>"thickness 1,2"</c> / <c>"radius 3"</c>, a number, or a geometry <c>"geometry M 0 0 L 4 4"</c>. A dark key that
    /// is missing is derived from the light one.
    /// <para>
    /// <c>"linear-abs 0,0 0,20 #A@0 #B@1"</c> is a linear gradient with absolute points (WPF's
    /// <c>MappingMode="Absolute"</c>). A brush value (solid, <c>linear</c>, <c>linear-abs</c> or <c>radial</c>) may end
    /// with <c>opacity=0.63</c>, the brush <c>Opacity</c> (the WPF chrome sets it on some code-built brushes).
    /// </para>
    /// </summary>
    public static InventedSet Load(string tools, string family) =>
        LoadFile(Path.Combine(tools, "invented", family.ToLowerInvariant() + ".toml"), family, family + ".Invented.", $"invented/{family.ToLowerInvariant()}.toml");

    /// <summary>
    /// The colors the code-drawn chrome builds from literals in WPF, from <c>chrome/&lt;name&gt;.toml</c>. Each file
    /// lists the families it serves (<c>families = ["Aero", "Aero2"]</c>); its keys become
    /// <c>&lt;Family&gt;.Chrome.&lt;key&gt;</c> in each of them. Values use the invented-token formats.
    /// </summary>
    public static InventedSet LoadChrome(string tools, string family)
    {
        var set = new InventedSet(new(), new());
        var dir = Path.Combine(tools, "chrome");
        if (!Directory.Exists(dir))
        {
            return set;
        }

        foreach (var f in Directory.GetFiles(dir, "*.toml").OrderBy(f => f, StringComparer.Ordinal))
        {
            var model = Toml.ToModel(File.ReadAllText(f));
            var families = model.TryGetValue("families", out var fo) && fo is TomlArray arr ? arr.Select(x => (string)x!).ToList() : new List<string>();
            if (!families.Contains(family))
            {
                continue;
            }

            var part = LoadFile(f, family, family + ".Chrome.", "chrome/" + Path.GetFileName(f));
            set.Light.AddRange(part.Light);
            set.Dark.AddRange(part.Dark);
        }

        return set;
    }

    private static InventedSet LoadFile(string path, string family, string prefix, string source)
    {
        var set = new InventedSet(new(), new());
        if (!File.Exists(path))
        {
            return set;
        }

        var model = Toml.ToModel(File.ReadAllText(path));
        foreach (var (variant, list) in new[] { ("light", set.Light), ("dark", set.Dark), ("highcontrast", set.HighContrast) })
        {
            if (!model.TryGetValue(variant, out var o) || o is not TomlTable table)
            {
                continue;
            }

            foreach (var (k, v) in Flatten(table, string.Empty))
            {
                list.Add(new Token { Key = prefix + k, Value = Value(k, v), Source = source });
            }
        }

        return set;
    }

    private static IEnumerable<(string, object)> Flatten(TomlTable t, string prefix)
    {
        foreach (var (k, v) in t)
        {
            var key = prefix.Length == 0 ? k : prefix + "." + k;
            if (v is TomlTable inner)
            {
                foreach (var x in Flatten(inner, key))
                {
                    yield return x;
                }
            }
            else
            {
                yield return (key, v);
            }
        }
    }

    private static XElement Value(string key, object v)
    {
        if (v is long or double)
        {
            return new XElement(Generator.X + "Double", System.Convert.ToDouble(v, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
        }

        var s = ((string)v).Trim();
        if (s.StartsWith("{", StringComparison.Ordinal))
        {
            return new XElement(Generator.Ava + "SolidColorBrush", new XAttribute("Color", s));
        }

        // A trailing "opacity=N" sets the brush Opacity.
        string? opacity = null;
        var lastSpace = s.LastIndexOf(' ');
        if (lastSpace > 0 && s.AsSpan(lastSpace + 1).StartsWith("opacity=", StringComparison.Ordinal))
        {
            opacity = s[(lastSpace + 1 + "opacity=".Length)..];
            s = s[..lastSpace].TrimEnd();
        }

        var value = BrushValue(key, s);
        if (opacity is not null)
        {
            value.SetAttributeValue("Opacity", opacity);
        }

        return value;
    }

    private static XElement BrushValue(string key, string s)
    {
        var parts = s.Split(' ', 2);
        switch (parts[0])
        {
            case "thickness": return new XElement(Generator.Ava + "Thickness", parts[1]);
            case "radius": return new XElement(Generator.Ava + "CornerRadius", parts[1]);
            case "geometry": return new XElement(Generator.Ava + "StreamGeometry", parts[1]);
            case "string": return new XElement(Generator.X + "String", parts[1]);
            case "shadow": return new XElement(Generator.Ava + "BoxShadows", parts[1]);
            case "radial":
            {
                // radial cx,cy rx,ry #A@0 #B@1 (relative units)
                var p = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var radius = p[1].Split(',');
                var b = new XElement(Generator.Ava + "RadialGradientBrush",
                    new XAttribute("Center", Convert.RelativePoint(p[0])),
                    new XAttribute("GradientOrigin", Convert.RelativePoint(p[0])),
                    new XAttribute("RadiusX", Convert.Num(double.Parse(radius[0], CultureInfo.InvariantCulture) * 100) + "%"),
                    new XAttribute("RadiusY", Convert.Num(double.Parse(radius[1], CultureInfo.InvariantCulture) * 100) + "%"));
                foreach (var stop in p.Skip(2))
                {
                    var cs = stop.Split('@');
                    b.Add(new XElement(Generator.Ava + "GradientStop", new XAttribute("Color", Colors.Normalize(cs[0])), new XAttribute("Offset", cs[1])));
                }

                return b;
            }

            case "linear":
            case "linear-abs":
            {
                // linear x1,y1 x2,y2 #A@0 #B@1 (relative units); linear-abs takes absolute points.
                var p = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var absolute = parts[0] == "linear-abs";
                var b = new XElement(Generator.Ava + "LinearGradientBrush",
                    new XAttribute("StartPoint", absolute ? p[0] : Convert.RelativePoint(p[0])),
                    new XAttribute("EndPoint", absolute ? p[1] : Convert.RelativePoint(p[1])));
                foreach (var stop in p.Skip(2))
                {
                    var cs = stop.Split('@');
                    b.Add(new XElement(Generator.Ava + "GradientStop", new XAttribute("Color", Colors.Normalize(cs[0])), new XAttribute("Offset", cs[1])));
                }

                return b;
            }
        }

        if (key.EndsWith("Color", StringComparison.Ordinal))
        {
            return new XElement(Generator.Ava + "Color", Colors.Normalize(s));
        }

        return new XElement(Generator.Ava + "SolidColorBrush", new XAttribute("Color", Colors.Normalize(s)));
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// System snapshots.

sealed class Snapshot
{
    public required string Name { get; init; }
    public SortedDictionary<string, uint> Colors { get; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, double> Numbers { get; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, string> Strings { get; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, string> Sources { get; } = new(StringComparer.Ordinal);
}

static class Snapshots
{
    /// <summary>The 22 base Classic schemes and the scheme names in classic_schemes.json they come from.</summary>
    public static readonly (string Id, string Source)[] ClassicSchemes =
    [
        ("WindowsStandard", "Windows Standard"), ("WindowsClassic", "Windows Classic"), ("Brick", "Brick"),
        ("Desert", "Desert"), ("Eggplant", "Eggplant"), ("Lilac", "Lilac"), ("Maple", "Maple"),
        ("Marine", "Marine (high color)"), ("Plum", "Plum (high color)"),
        // XP ships Pumpkin only in its large size; that is its base.
        ("Pumpkin", "Pumpkin (large)"),
        ("RainyDay", "Rainy Day"), ("RedWhiteBlue", "Red, White, and Blue (VGA)"), ("Rose", "Rose"),
        ("Slate", "Slate"), ("Spruce", "Spruce"), ("Storm", "Storm (VGA)"), ("Teal", "Teal (VGA)"), ("Wheat", "Wheat"),
        ("HighContrast1", "High Contrast #1"), ("HighContrast2", "High Contrast #2"),
        ("HighContrastBlack", "High Contrast Black"), ("HighContrastWhite", "High Contrast White"),
    ];

    /// <summary>GetSysColor role names (registry) → WPF SystemColors names.</summary>
    private static readonly Dictionary<string, string> s_roles = new(StringComparer.Ordinal)
    {
        ["Scrollbar"] = "ScrollBar", ["Background"] = "Desktop", ["ActiveCaption"] = "ActiveCaption",
        ["InactiveCaption"] = "InactiveCaption", ["Menu"] = "Menu", ["Window"] = "Window", ["WindowFrame"] = "WindowFrame",
        ["MenuText"] = "MenuText", ["WindowText"] = "WindowText", ["CaptionText"] = "ActiveCaptionText",
        ["ActiveBorder"] = "ActiveBorder", ["InactiveBorder"] = "InactiveBorder", ["AppWorkspace"] = "AppWorkspace",
        ["Highlight"] = "Highlight", ["HighlightText"] = "HighlightText", ["ButtonFace"] = "Control",
        ["ButtonShadow"] = "ControlDark", ["GrayText"] = "GrayText", ["ButtonText"] = "ControlText",
        ["InactiveCaptionText"] = "InactiveCaptionText", ["ButtonHighlight"] = "ControlLightLight",
        ["ButtonDkShadow"] = "ControlDarkDark", ["ButtonLight"] = "ControlLight", ["InfoText"] = "InfoText",
        ["InfoWindow"] = "Info", ["HotTrackingColor"] = "HotTrack", ["GradientActiveTitle"] = "GradientActiveCaption",
        ["GradientInactiveTitle"] = "GradientInactiveCaption", ["MenuHilight"] = "MenuHighlight", ["MenuBar"] = "MenuBar",
    };

    public static List<Snapshot> Load(string tools)
    {
        var dir = Path.Combine(tools, "system");
        var list = new List<Snapshot>();
        if (!Directory.Exists(dir))
        {
            return list;
        }

        var json = Path.Combine(dir, "classic_schemes.json");
        if (File.Exists(json))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(json));
            foreach (var (id, source) in ClassicSchemes)
            {
                list.Add(FromClassic("Classic." + id, doc.RootElement.GetProperty(source), source));
            }
        }

        var raw = new Dictionary<string, TomlTable>(StringComparer.Ordinal);
        foreach (var f in Directory.GetFiles(dir, "*.toml").OrderBy(f => f, StringComparer.Ordinal))
        {
            raw[Path.GetFileNameWithoutExtension(f)] = Toml.ToModel(File.ReadAllText(f));
        }

        foreach (var name in raw.Keys)
        {
            list.Add(FromToml(name, raw, list));
        }

        return list.OrderBy(s => s.Name, StringComparer.Ordinal).ToList();
    }

    private static Snapshot FromClassic(string name, JsonElement scheme, string source)
    {
        var s = new Snapshot { Name = name };
        foreach (var c in scheme.GetProperty("colors").EnumerateObject())
        {
            if (s_roles.TryGetValue(c.Name, out var role) && Colors.TryParse(c.Value.GetString()!, out var argb))
            {
                s.Colors[role] = argb;
                s.Sources[role] = $"classic_schemes.json \"{source}\"";
            }
        }

        // Windows 2000/XP Classic: the flat-menu colors equal the 3D face and the selection color.
        s.Colors.TryAdd("MenuBar", s.Colors.GetValueOrDefault("Menu"));
        s.Colors.TryAdd("MenuHighlight", s.Colors.GetValueOrDefault("Highlight"));

        double I(string p) => scheme.TryGetProperty(p, out var v) ? v.GetDouble() : 0;
        s.Numbers["VerticalScrollBarWidth"] = I("scrollWidth");
        s.Numbers["HorizontalScrollBarHeight"] = I("scrollHeight");
        s.Numbers["VerticalScrollBarButtonHeight"] = I("scrollHeight");
        s.Numbers["HorizontalScrollBarButtonWidth"] = I("scrollWidth");
        s.Numbers["CaptionHeight"] = I("captionHeight");
        s.Numbers["CaptionWidth"] = I("captionWidth");
        s.Numbers["SmallCaptionHeight"] = I("smCaptionHeight");
        s.Numbers["SmallCaptionWidth"] = I("smCaptionWidth");
        s.Numbers["MenuHeight"] = I("menuHeight");
        s.Numbers["BorderWidth"] = I("borderWidth");
        foreach (var (prop, prefix) in new[] { ("captionFont", "CaptionFont"), ("smCaptionFont", "SmallCaptionFont"), ("menuFont", "MenuFont"), ("statusFont", "StatusFont"), ("messageFont", "MessageFont") })
        {
            if (scheme.TryGetProperty(prop, out var f))
            {
                Font(s, prefix, f.GetString()!);
            }
        }

        var hc = name.Contains("HighContrast", StringComparison.Ordinal);
        s.Numbers["HighContrast"] = hc ? 1 : 0;
        s.Numbers["ClientAreaAnimation"] = hc ? 0 : 1;
        s.Numbers["DropShadow"] = 0;
        s.Numbers["KeyboardCues"] = 0;
        s.Strings["MenuPopupAnimation"] = "Slide";
        s.Strings["ComboBoxPopupAnimation"] = "Slide";
        s.Strings["ToolTipAnimation"] = "Fade";
        return s;
    }

    /// <summary>A LOGFONT summary such as <c>Tahoma h=-12 w=700</c>.</summary>
    private static void Font(Snapshot s, string prefix, string spec)
    {
        var m = Regex.Match(spec, @"^(?<f>.*?)\s+h=(?<h>-?\d+)\s+w=(?<w>\d+)");
        if (!m.Success)
        {
            return;
        }

        var face = m.Groups["f"].Value;
        var size = Math.Abs(int.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture));
        if (size == 0)
        {
            return;
        }

        s.Strings[prefix + "Family"] = FontChain(face);
        s.Numbers[prefix + "Size"] = size;
        s.Numbers[prefix + "Weight"] = int.Parse(m.Groups["w"].Value, CultureInfo.InvariantCulture);
        s.Strings[prefix + "Style"] = "Normal";
    }

    /// <summary>The real face first, then the bundled substitute, then the generic family.</summary>
    public static string FontChain(string face) => face switch
    {
        // Wine's MS Sans Serif carries only bitmap strikes, which neither Skia nor WPF scale well; the outline Tahoma
        // stands in for it when the real font is missing, as the WPF reference shooter does.
        "Microsoft Sans Serif" or "MS Sans Serif" => "MS Sans Serif, Microsoft Sans Serif, Tahoma, fonts:AvaWpf#Tahoma, sans-serif",
        "Tahoma" => "Tahoma, fonts:AvaWpf#Tahoma, sans-serif",
        "Trebuchet MS" => "Trebuchet MS, Tahoma, fonts:AvaWpf#Tahoma, sans-serif",
        "Segoe UI" => "Segoe UI, fonts:AvaWpf#Selawik, sans-serif",
        "Segoe UI Variable" => "Segoe UI Variable, Segoe UI, fonts:AvaWpf#Selawik, sans-serif",
        "Arial" => "Arial, Liberation Sans, sans-serif",
        _ => face + ", sans-serif",
    };

    private static Snapshot FromToml(string name, Dictionary<string, TomlTable> raw, List<Snapshot> loaded)
    {
        var t = raw[name];
        var s = new Snapshot { Name = (t.TryGetValue("name", out var n) ? (string)n : name) };
        if (t.TryGetValue("base", out var b))
        {
            var baseName = (string)b;
            var parent = raw.ContainsKey(baseName) ? FromToml(baseName, raw, loaded) : loaded.FirstOrDefault(x => x.Name == baseName)
                ?? throw new InvalidOperationException($"system/{name}.toml: unknown base '{baseName}'");
            foreach (var (k, v) in parent.Colors) { s.Colors[k] = v; }
            foreach (var (k, v) in parent.Numbers) { s.Numbers[k] = v; }
            foreach (var (k, v) in parent.Strings) { s.Strings[k] = v; }
            foreach (var (k, v) in parent.Sources) { s.Sources[k] = v + $" (via {baseName})"; }
        }

        if (t.TryGetValue("colors", out var co) && co is TomlTable colors)
        {
            foreach (var (k, v) in colors)
            {
                var (value, source) = v switch
                {
                    string str => (str, "system/" + name + ".toml"),
                    TomlTable tt => ((string)tt["value"], (string)tt["source"]),
                    _ => throw new InvalidOperationException($"system/{name}.toml: bad color {k}"),
                };
                if (!Colors.TryParse(value, out var argb))
                {
                    throw new InvalidOperationException($"system/{name}.toml: bad color {k} = {value}");
                }

                s.Colors[k] = argb;
                s.Sources[k] = source;
            }
        }

        if (t.TryGetValue("parameters", out var po) && po is TomlTable parameters)
        {
            foreach (var (k, v) in parameters)
            {
                switch (v)
                {
                    case bool bv: s.Numbers[k] = bv ? 1 : 0; break;
                    case long lv: s.Numbers[k] = lv; break;
                    case double dv: s.Numbers[k] = dv; break;
                    case string sv: s.Strings[k] = sv; break;
                }
            }
        }

        if (t.TryGetValue("fonts", out var fo) && fo is TomlTable fonts)
        {
            foreach (var (k, v) in fonts)
            {
                switch (v)
                {
                    case string sv when k.EndsWith("Family", StringComparison.Ordinal): s.Strings[k] = FontChain(sv); break;
                    case string sv: s.Strings[k] = sv; break;
                    case long lv: s.Numbers[k] = lv; break;
                    case double dv: s.Numbers[k] = dv; break;
                }
            }
        }

        return s;
    }
}

// ---------------------------------------------------------------------------------------------------------------------
// Output.

static class Output
{
    private const string Header = "<auto-generated>\n  Generated by tools/wpf-tokens/wpf-tokens.cs from the WPF theme sources. Do not edit.\n</auto-generated>";

    public static void WriteText(string outRoot, string relative, string text)
    {
        var path = Path.Combine(outRoot, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text.Replace("\r\n", "\n"));
        Generator.Record(relative);
    }

    public static void WriteTokens(string outRoot, string family, string file, List<Token> tokens, string title)
    {
        var className = "Tokens" + string.Concat(file["Tokens".Length..^".axaml".Length].Split('.'));
        var dict = new XElement(Generator.Ava + "ResourceDictionary",
            new XAttribute("xmlns", Generator.Ava.NamespaceName),
            new XAttribute(Generator.X + "Class", $"AvaWpf.Themes.{family}.{className}"),
            new XAttribute(Generator.X + "ClassModifier", "internal"),
            new XAttribute(XNamespace.Xmlns + "x", Generator.X.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "wpf", Generator.AvaWpf.NamespaceName));
        foreach (var t in tokens)
        {
            var e = new XElement(t.Value);
            e.SetAttributeValue(Generator.X + "Key", t.Key);
            // x:Key first, for readability.
            var attrs = e.Attributes().ToList();
            e.RemoveAttributes();
            e.Add(attrs.Where(a => a.Name == Generator.X + "Key"));
            e.Add(attrs.Where(a => a.Name != Generator.X + "Key"));
            dict.Add(e);
        }

        var doc = new XDocument(new XComment($"\n  {Header.Replace("\n", "\n  ")}\n  {title}. {tokens.Count} tokens.\n"), dict);
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", OmitXmlDeclaration = true, NewLineChars = "\n", Encoding = new UTF8Encoding(false) };
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, settings))
        {
            doc.Save(w);
        }

        WriteText(outRoot, $"src/AvaWpf.Theme/Themes/{family}/{file}", sb + "\n");
        WriteText(outRoot, $"src/AvaWpf.Theme/Themes/{family}/{file}.cs", $$"""
            // <auto-generated>
            // Generated by tools/wpf-tokens/wpf-tokens.cs. Do not edit.
            // </auto-generated>
            using Avalonia.Controls;
            using Avalonia.Markup.Xaml;

            namespace AvaWpf.Themes.{{family}};

            /// <summary>The {{title}} tokens.</summary>
            internal partial class {{className}} : ResourceDictionary
            {
                public {{className}}()
                {
                    AvaloniaXamlLoader.Load(this);
                }
            }

            """);
    }

    public static void WriteMap(string outRoot, string family, List<Token> tokens)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {family} token map").AppendLine().AppendLine("Generated by tools/wpf-tokens/wpf-tokens.cs. Each key and the WPF source line it came from.").AppendLine();
        sb.AppendLine("| Key | Source |").AppendLine("|---|---|");
        foreach (var t in tokens.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"| `{t.Key}` | {t.Source} |");
        }

        WriteText(outRoot, $"tools/wpf-tokens/tokens/{family.ToLowerInvariant()}.map.md", sb.ToString());
    }

    private static string Lit(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public static void WriteSnapshots(string outRoot, List<Snapshot> snapshots)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>").AppendLine("// Generated by tools/wpf-tokens/wpf-tokens.cs from tools/wpf-tokens/system. Do not edit.").AppendLine("// </auto-generated>");
        sb.AppendLine("using System.Collections.Generic;").AppendLine().AppendLine("namespace AvaWpf;").AppendLine();
        sb.AppendLine("/// <summary>The system snapshots: SystemColors, SystemParameters and SystemFonts per family, scheme and variant.</summary>");
        sb.AppendLine("internal static class Snapshots").AppendLine("{");
        sb.AppendLine("    /// <summary>Every snapshot by name.</summary>");
        sb.AppendLine("    public static readonly IReadOnlyDictionary<string, SystemSnapshot> All = new Dictionary<string, SystemSnapshot>").AppendLine("    {");
        foreach (var s in snapshots)
        {
            sb.AppendLine($"        [{Lit(s.Name)}] = new SystemSnapshot(");
            sb.AppendLine($"            {Lit(s.Name)},");
            sb.AppendLine("            new Dictionary<string, uint>");
            sb.AppendLine("            {");
            foreach (var (k, v) in s.Colors)
            {
                var src = s.Sources.TryGetValue(k, out var so) ? " // " + so : string.Empty;
                sb.AppendLine($"                [{Lit(k)}] = 0x{v:X8},{src}");
            }

            sb.AppendLine("            },");
            sb.AppendLine("            new Dictionary<string, double>");
            sb.AppendLine("            {");
            foreach (var (k, v) in s.Numbers)
            {
                sb.AppendLine($"                [{Lit(k)}] = {Convert.Num(v)},");
            }

            sb.AppendLine("            },");
            sb.AppendLine("            new Dictionary<string, string>");
            sb.AppendLine("            {");
            foreach (var (k, v) in s.Strings)
            {
                sb.AppendLine($"                [{Lit(k)}] = {Lit(v)},");
            }

            sb.AppendLine("            }),");
        }

        sb.AppendLine("    };").AppendLine("}");
        WriteText(outRoot, "src/AvaWpf.Theme/System/Snapshots.g.cs", sb.ToString());
    }

    public static void WriteTokenKeys(string outRoot, string relative, string ns, SortedDictionary<string, SortedSet<string>> keys)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>").AppendLine("// Generated by tools/wpf-tokens/wpf-tokens.cs. Do not edit.").AppendLine("// </auto-generated>");
        sb.AppendLine("using System.Collections.Generic;").AppendLine().AppendLine($"namespace {ns};").AppendLine();
        sb.AppendLine("/// <summary>Every generated token key per family.</summary>");
        sb.AppendLine("internal static class TokenKeys").AppendLine("{");
        sb.AppendLine("    public static readonly IReadOnlyDictionary<string, string[]> ByFamily = new Dictionary<string, string[]>").AppendLine("    {");
        foreach (var (family, set) in keys)
        {
            sb.AppendLine($"        [{Lit(family)}] =").AppendLine("        [");
            foreach (var k in set)
            {
                sb.AppendLine($"            {Lit(k)},");
            }

            sb.AppendLine("        ],");
        }

        sb.AppendLine("    };").AppendLine("}");
        WriteText(outRoot, relative, sb.ToString());
    }

    public static void WriteLayer0(string outRoot, Dictionary<string, SortedDictionary<string, int>> refs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>").AppendLine("// Generated by tools/wpf-tokens/wpf-tokens.cs. Do not edit.").AppendLine("// </auto-generated>");
        sb.AppendLine("using System.Collections.Generic;").AppendLine().AppendLine("namespace AvaWpf.Theme.Tests;").AppendLine();
        sb.AppendLine("/// <summary>The SystemColors, SystemParameters and SystemFonts keys each family's WPF sources reference, with counts.</summary>");
        sb.AppendLine("internal static class Layer0Keys").AppendLine("{");
        sb.AppendLine("    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> ByFamily = new Dictionary<string, IReadOnlyDictionary<string, int>>").AppendLine("    {");
        foreach (var family in refs.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            sb.AppendLine($"        [{Lit(family)}] = new Dictionary<string, int>").AppendLine("        {");
            foreach (var (k, v) in refs[family])
            {
                sb.AppendLine($"            [{Lit(k)}] = {v},");
            }

            sb.AppendLine("        },");
        }

        sb.AppendLine("    };").AppendLine("}");
        WriteText(outRoot, "tests/AvaWpf.Theme.Tests/Generated/Layer0Keys.g.cs", sb.ToString());
    }

    public static void WriteMotions(string outRoot, List<Motion> motions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>").AppendLine("// Generated by tools/wpf-tokens/wpf-tokens.cs. Do not edit.").AppendLine("// </auto-generated>");
        sb.AppendLine("namespace AvaWpf.Theme.Tests;").AppendLine();
        sb.AppendLine("/// <summary>Every storyboard in the WPF sources: family, fragment, owner style or template, trigger, duration (ms), repeat.</summary>");
        sb.AppendLine("internal static class MotionInventory").AppendLine("{");
        sb.AppendLine("    public static readonly (string Family, string Fragment, string Owner, string Trigger, double DurationMs, bool Forever)[] All =").AppendLine("    [");
        foreach (var m in motions.Distinct().OrderBy(m => m.Family, StringComparer.Ordinal).ThenBy(m => m.Fragment, StringComparer.Ordinal).ThenBy(m => m.Owner, StringComparer.Ordinal).ThenBy(m => m.Trigger, StringComparer.Ordinal))
        {
            sb.AppendLine($"        ({Lit(m.Family)}, {Lit(m.Fragment)}, {Lit(m.Owner)}, {Lit(m.Trigger)}, {Convert.Num(m.DurationMs)}, {(m.Forever ? "true" : "false")}),");
        }

        sb.AppendLine("    ];").AppendLine("}");
        WriteText(outRoot, "tests/AvaWpf.Theme.Tests/Generated/MotionInventory.g.cs", sb.ToString());
    }
}
