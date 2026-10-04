using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>Token coverage, system key coverage and the no-literal-colors rule.</summary>
public class TokenTests
{
    public static IEnumerable<object[]> AllSchemes() =>
        Families.AllSchemes().Select(s => new object[] { s.Family, s.Scheme });

    public static IEnumerable<object[]> FamiliesOnly() =>
        Enum.GetValues<ThemeFamily>().Select(f => new object[] { f });

    private static ThemeResources Resources(ThemeFamily family, string scheme) => new(family, scheme, null);

    [AvaloniaTheory]
    [MemberData(nameof(AllSchemes))]
    public void Every_Generated_Token_Resolves(ThemeFamily family, string scheme)
    {
        var r = Resources(family, scheme);
        var missing = new List<string>();
        foreach (var variant in Families.Variants(family))
        {
            foreach (var key in TokenKeys.ByFamily[family.ToString()])
            {
                if (!r.TryGetResource(key, variant, out _))
                {
                    missing.Add($"{key} ({variant})");
                }
            }
        }

        Assert.True(missing.Count == 0, $"{family}.{scheme}: {missing.Count} tokens do not resolve: {string.Join(", ", missing.Take(20))}");
    }

    [AvaloniaTheory]
    [MemberData(nameof(AllSchemes))]
    public void Every_Template_Resource_Resolves(ThemeFamily family, string scheme)
    {
        var r = Resources(family, scheme);
        var controls = FamilyCatalog.LoadControls(family);
        var missing = new List<string>();
        foreach (var variant in Families.Variants(family))
        {
            foreach (var key in ThemeXaml.ReferencedKeys(family))
            {
                if (!r.TryGetResource(key, variant, out _) && !controls.TryGetResource(key, variant, out _))
                {
                    missing.Add($"{key} ({variant})");
                }
            }
        }

        Assert.True(missing.Count == 0, $"{family}.{scheme}: {missing.Count} template keys do not resolve: {string.Join(", ", missing.Distinct().Take(30))}");
    }

    [AvaloniaTheory]
    [MemberData(nameof(AllSchemes))]
    public void Layer0_Serves_Every_WPF_System_Key(ThemeFamily family, string scheme)
    {
        var system = new SystemResources(family, scheme);
        var accent = new AccentColors(family, null);
        var missing = new List<string>();
        foreach (var variant in Families.Variants(family))
        {
            foreach (var key in Layer0Keys.ByFamily[family.ToString()].Keys)
            {
                if (!system.TryGetResource(key, variant, out _) && !accent.TryGetResource(key, variant, out _))
                {
                    missing.Add($"{key} ({variant})");
                }
            }
        }

        Assert.True(missing.Count == 0, $"{family}.{scheme}: {string.Join(", ", missing.Distinct())}");
    }

    private static readonly Regex s_literal = new(@"=""[^""]*#[0-9A-Fa-f]{3,8}\b[^""]*""", RegexOptions.Compiled);

    [AvaloniaTheory]
    [MemberData(nameof(FamiliesOnly))]
    public void Templates_Hold_No_Color_Literals(ThemeFamily family)
    {
        var offenders = new List<string>();
        foreach (var (name, text) in ThemeXaml.ControlFiles(family))
        {
            var withoutComments = Regex.Replace(text, "<!--.*?-->", string.Empty, RegexOptions.Singleline);
            foreach (Match m in s_literal.Matches(withoutComments))
            {
                offenders.Add($"{name}: {m.Value}");
            }
        }

        Assert.True(offenders.Count == 0, string.Join("\n", offenders.Take(20)));
    }

    // RenderTransformOrigin="0.5,0.5" is WPF's centre; Avalonia reads numbers without % as pixels (half a pixel in).
    private static readonly Regex s_absoluteOrigin = new(@"RenderTransformOrigin(?:=""|"" Value="")(?![^""]*%)(?!0,0"")[^""]*""", RegexOptions.Compiled);

    [AvaloniaTheory]
    [MemberData(nameof(FamiliesOnly))]
    public void Transform_Origins_Are_Relative(ThemeFamily family)
    {
        var offenders = ThemeXaml.ControlFiles(family)
            .SelectMany(f => s_absoluteOrigin.Matches(f.Text).Select(m => $"{f.Name}: {m.Value}"))
            .ToList();

        Assert.True(offenders.Count == 0, string.Join("\n", offenders));
    }
}
