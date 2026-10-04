using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// Reads the theme XAML copied to the test output (ThemeXaml/), expanding the shared files per family as the build
/// does, so the tests can scan what each family's templates reference.
/// </summary>
internal static class ThemeXaml
{
    private static readonly string[] s_classic = ["Aero2", "AeroLite", "Aero", "Luna", "Royale", "Classic"];

    public static string Root => Path.Combine(AppContext.BaseDirectory, "ThemeXaml");

    /// <summary>The ControlTheme files of a family: (file name, expanded text).</summary>
    public static IReadOnlyList<(string Name, string Text)> ControlFiles(ThemeFamily family)
    {
        var name = family.ToString();
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        var shared = Path.Combine(Root, "Shared");
        if (Directory.Exists(shared))
        {
            foreach (var group in Directory.GetDirectories(shared))
            {
                var g = Path.GetFileName(group);
                var members = g == "AllClassic" ? s_classic : g == "All" ? [.. s_classic, "Fluent"] : g.Split('+');
                if (!members.Contains(name))
                {
                    continue;
                }

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

        return files.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (p.Key, p.Value)).ToList();
    }

    private static readonly Regex s_dynamic = new(@"\{DynamicResource\s+(?:ResourceKey=)?(?<k>[^}\s]+)\s*\}", RegexOptions.Compiled);
    private static readonly Regex s_static = new(@"\{StaticResource\s+(?:ResourceKey=)?(?<k>[^}\s{]+)\s*\}", RegexOptions.Compiled);

    /// <summary>Every DynamicResource/StaticResource string key the family's templates reference.</summary>
    public static IReadOnlySet<string> ReferencedKeys(ThemeFamily family)
    {
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (_, text) in ControlFiles(family))
        {
            foreach (Match m in s_dynamic.Matches(text))
            {
                keys.Add(m.Groups["k"].Value);
            }

            foreach (Match m in s_static.Matches(text))
            {
                keys.Add(m.Groups["k"].Value);
            }
        }

        return keys;
    }
}
