using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>The themed-control matrix: every control in every family, scheme and variant.</summary>
public class ControlThemeTests
{
    public static IEnumerable<object[]> FamilyControls() =>
        from f in Families.Selected()
        from c in ThemedControls.All
        select new object[] { f, c.Type.Name };

    public static IEnumerable<object[]> MatrixControls() =>
        from m in Families.Matrix
        where Families.Selected().Contains(m.Family)
        from v in Families.Variants(m.Family)
        from c in ThemedControls.All
        where !ThemedControls.ThemeOnly.Contains(c.Type)
        select new object[] { m.Family, m.Scheme, v.Key.ToString()!, c.Type.Name };

    private static (Type Type, Func<Control> Create) Control(string name) => ThemedControls.All.Single(c => c.Type.Name == name);

    private static ThemeVariant Variant(string key) => key switch
    {
        "Dark" => ThemeVariant.Dark,
        "HighContrast" => WpfThemeVariants.HighContrast,
        _ => ThemeVariant.Light,
    };

    [AvaloniaTheory]
    [MemberData(nameof(FamilyControls))]
    public void Family_Has_ControlTheme(ThemeFamily family, string control)
    {
        var type = Control(control).Type;
        var dictionary = FamilyCatalog.LoadControls(family);
        Assert.True(dictionary.TryGetResource(type, null, out var theme), $"{family} has no ControlTheme for {control}");
        Assert.IsType<ControlTheme>(theme);
    }

    [AvaloniaTheory]
    [MemberData(nameof(MatrixControls))]
    public void Template_Builds_With_Required_Parts(ThemeFamily family, string scheme, string variant, string control)
    {
        var (type, create) = Control(control);
        var instance = create();
        var scope = new ThemeScope { Theme = family, ColorScheme = scheme, RequestedThemeVariant = Variant(variant), Child = instance };
        var window = new Window { Content = scope, Width = 400, Height = 300 };
        window.Show();
        window.UpdateLayout();

        Assert.True(instance.TryFindResource(type, instance.ActualThemeVariant, out var theme) && theme is ControlTheme,
            $"{family} has no ControlTheme for {control}");

        if (instance is TemplatedControl templated)
        {
            Assert.True(templated.GetVisualChildren().Any(), $"{control} built an empty visual tree in {family}");
            foreach (var part in type.GetCustomAttributes<TemplatePartAttribute>(inherit: true).Where(p => p.IsRequired))
            {
                Assert.True(templated.GetVisualDescendants().OfType<Control>().Any(c => c.Name == part.Name && c.TemplatedParent == templated),
                    $"{control} in {family} misses the required part {part.Name}");
            }

            if (instance is ContentControl cc && instance is not ToolTip)
            {
                Assert.True(cc.Presenter is not null, $"{control} in {family} has no content presenter");
            }

            if (instance is ItemsControl ic and not ComboBox and not MenuItem and not TreeViewItem)
            {
                Assert.True(ic.Presenter is not null, $"{control} in {family} has no items presenter");
            }
        }

        window.Close();
    }
}
