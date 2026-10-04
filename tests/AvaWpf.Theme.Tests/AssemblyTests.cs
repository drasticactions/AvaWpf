using System;
using System.Linq;
using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Metadata;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>xmlns mapping of every assembly and the font registration.</summary>
public class AssemblyTests
{
    private const string Xmlns = "https://github.com/avawpf";

    public static TheoryData<string> Assemblies() => new()
    {
        "AvaWpf.Animations", "AvaWpf.Theme", "AvaWpf.Fonts", "AvaWpf.Icons", "AvaWpf.Controls", "AvaWpf.DataGrid", "AvaWpf.Ribbon",
    };

    [Theory]
    [MemberData(nameof(Assemblies))]
    public void Every_Public_Namespace_Is_Mapped(string name)
    {
        var assembly = Assembly.Load(name);
        var mapped = assembly.GetCustomAttributes<XmlnsDefinitionAttribute>().Where(a => a.XmlNamespace == Xmlns).Select(a => a.ClrNamespace).ToHashSet();
        var namespaces = assembly.GetExportedTypes().Select(t => t.Namespace!).Where(n => n is not null && n != "CompiledAvaloniaXaml" && !n.Contains(".Themes.", StringComparison.Ordinal)).ToHashSet();
        var missing = namespaces.Except(mapped).ToList();
        Assert.True(missing.Count == 0, $"{name}: unmapped namespaces {string.Join(", ", missing)}");
        Assert.Contains(assembly.GetCustomAttributes<XmlnsPrefixAttribute>(), a => a.XmlNamespace == Xmlns && a.Prefix == "wpf");
    }

    [AvaloniaFact]
    public void Selawik_Semilight_Is_Distinct_From_Light()
    {
        var fm = FontManager.Current;
        Assert.True(fm.TryGetGlyphTypeface(new Typeface("fonts:AvaWpf#Selawik", FontStyle.Normal, FontWeight.SemiLight), out var semilight));
        Assert.True(fm.TryGetGlyphTypeface(new Typeface("fonts:AvaWpf#Selawik", FontStyle.Normal, FontWeight.Light), out var light));
        // Both faces declare weight 300; Semilight is registered under its own key, so the two requests get different faces.
        Assert.NotSame(semilight, light);
    }

    [AvaloniaTheory]
    [InlineData("Tahoma", "Tahoma")]
    [InlineData("MS Sans Serif", "Tahoma")]
    [InlineData("Segoe UI", "Selawik")]
    public void Family_Alias_Resolves_To_A_Bundled_Or_Installed_Face(string requested, string bundled)
    {
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(requested), out var face));
        Assert.True(face!.FamilyName == requested || face.FamilyName == bundled, $"{requested} resolved to {face.FamilyName}");
    }

    [AvaloniaFact]
    public void Fluent_Glyph_Subset_Has_Every_Glyph()
    {
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("fonts:AvaWpf#AvaWpf Fluent Glyphs"), out var face));
        foreach (var field in typeof(FluentGlyph).GetFields())
        {
            var text = (string)field.GetValue(null)!;
            Assert.True(face!.CharacterToGlyphMap.TryGetGlyph(char.ConvertToUtf32(text, 0), out var glyph) && glyph != 0, $"{field.Name} is missing");
        }
    }

    [AvaloniaFact]
    public void Tahoma_Uses_The_Windows_Line_Height()
    {
        // Microsoft's Tahoma lays out at 1.207 em (usWin metrics); Wine's sets USE_TYPO_METRICS (1 em), which gives
        // 11 px list and menu rows instead of 13. AvaWpf.Fonts reads Wine's Tahoma as Windows reads Microsoft's.
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("fonts:AvaWpf#Tahoma"), out var face));
        var m = face!.Metrics;
        var lineHeight = (m.Descent - m.Ascent + m.LineGap) / (double)m.DesignEmHeight;
        Assert.InRange(lineHeight, 1.19, 1.22);
    }
}
