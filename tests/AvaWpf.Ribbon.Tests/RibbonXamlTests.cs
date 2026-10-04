using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>
/// The Ribbon theme files: every resource key they reference resolves in every family and variant, and no template
/// holds a color literal.
/// </summary>
public class RibbonXamlTests
{
    private static readonly string[] s_classic = ["Aero2", "AeroLite", "Aero", "Luna", "Royale", "Classic"];
    private static readonly Regex s_dynamic = new(@"\{DynamicResource\s+(?:ResourceKey=)?(?<k>[^}\s]+)\s*\}", RegexOptions.Compiled);
    private static readonly Regex s_literal = new(@"=""[^""]*#[0-9A-Fa-f]{3,8}\b[^""]*""", RegexOptions.Compiled);

    private static string Root => Path.Combine(AppContext.BaseDirectory, "RibbonXaml");

    public static IEnumerable<object[]> FamiliesOnly() => Enum.GetValues<ThemeFamily>().Select(f => new object[] { f });

    /// <summary>The Ribbon ControlTheme files of a family, the shared files expanded as the build does.</summary>
    private static List<(string Name, string Text)> Files(ThemeFamily family)
    {
        var name = family.ToString();
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var group in Directory.GetDirectories(Path.Combine(Root, "Shared")))
        {
            var g = Path.GetFileName(group);
            var members = g == "AllClassic" ? s_classic : g.Split('+');
            if (members.Contains(name))
            {
                foreach (var f in Directory.GetFiles(group, "*.axaml"))
                {
                    files[Path.GetFileName(f)] = File.ReadAllText(f).Replace("$F", name, StringComparison.Ordinal);
                }
            }
        }

        var own = Path.Combine(Root, name, "Controls");
        if (Directory.Exists(own))
        {
            foreach (var f in Directory.GetFiles(own, "*.axaml"))
            {
                files[Path.GetFileName(f)] = File.ReadAllText(f);
            }
        }

        return files.Select(p => (p.Key, p.Value)).ToList();
    }

    [AvaloniaTheory]
    [MemberData(nameof(FamiliesOnly))]
    public void Every_Template_Resource_Resolves(ThemeFamily family)
    {
        var resources = new ThemeResources(family, ColorSchemes.Resolve(family, null), null);
        var controls = AvaWpfRibbonTheme.LoadControls(family);
        var keys = Files(family).SelectMany(f => s_dynamic.Matches(f.Text).Select(m => m.Groups["k"].Value)).ToHashSet();
        var variants = family is ThemeFamily.Fluent or ThemeFamily.Classic
            ? new[] { ThemeVariant.Light, ThemeVariant.Dark, WpfThemeVariants.HighContrast }
            : new[] { ThemeVariant.Light, ThemeVariant.Dark };
        var missing = new List<string>();
        foreach (var variant in variants)
        {
            foreach (var key in keys)
            {
                if (!resources.TryGetResource(key, variant, out _) && !controls.TryGetResource(key, variant, out _))
                {
                    missing.Add($"{key} ({variant})");
                }
            }
        }

        Assert.True(missing.Count == 0, $"{family}: {missing.Count} Ribbon template keys do not resolve: {string.Join(", ", missing.Take(30))}");
    }

    [AvaloniaTheory]
    [MemberData(nameof(FamiliesOnly))]
    public void Templates_Hold_No_Color_Literals(ThemeFamily family)
    {
        var offenders = new List<string>();
        foreach (var (name, text) in Files(family))
        {
            var withoutComments = Regex.Replace(text, "<!--.*?-->", string.Empty, RegexOptions.Singleline);
            offenders.AddRange(s_literal.Matches(withoutComments).Select(m => $"{name}: {m.Value}"));
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders.Take(20)));
    }

    [AvaloniaFact]
    public void Every_Ribbon_Type_Has_A_Theme_File()
    {
        var names = Files(ThemeFamily.Aero2).Select(f => Path.GetFileNameWithoutExtension(f.Name)).ToHashSet();
        var missing = RibbonTemplateTests.Controls.Select(c => c.Type.Name).Where(n => !names.Contains(n)).ToList();
        Assert.Empty(missing);
    }
}
