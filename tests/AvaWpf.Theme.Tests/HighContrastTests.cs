using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Avalonia.Styling;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>The WPF high-contrast rule: HC forces Classic for every family but Fluent.</summary>
public class HighContrastTests
{
    private static AvaWpfTheme Theme => TestApplication.Instance.Theme;

    private static PlatformColorValues Values(bool high, bool light = false) => new()
    {
        ThemeVariant = light ? PlatformThemeVariant.Light : PlatformThemeVariant.Dark,
        ContrastPreference = high ? ColorContrastPreference.High : ColorContrastPreference.NoPreference,
    };

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2)]
    [InlineData(ThemeFamily.Luna)]
    [InlineData(ThemeFamily.Aero)]
    public void HighContrast_Forces_Classic_And_Restores(ThemeFamily family)
    {
        Theme.Theme = family;
        Theme.ApplyColorValues(Values(high: true));
        Assert.Equal(ThemeFamily.Classic, Theme.ActualTheme);
        Assert.Equal(ColorSchemes.HighContrastBlack, Theme.ActualColorScheme);
        Assert.Equal(WpfThemeVariants.HighContrast, Application.Current!.RequestedThemeVariant);

        Theme.ApplyColorValues(Values(high: false));
        Assert.Equal(family, Theme.ActualTheme);
        Assert.NotEqual(WpfThemeVariants.HighContrast, Application.Current!.RequestedThemeVariant);
        Theme.Theme = ThemeFamily.Aero2;
    }

    [AvaloniaFact]
    public void Light_HighContrast_Uses_HighContrastWhite()
    {
        Theme.Theme = ThemeFamily.Aero2;
        Theme.ApplyColorValues(Values(high: true, light: true));
        Assert.Equal(ColorSchemes.HighContrastWhite, Theme.ActualColorScheme);
        Theme.ApplyColorValues(Values(high: false, light: true));
    }

    [AvaloniaFact]
    public void Fluent_Keeps_Its_Family_Under_HighContrast()
    {
        Theme.Theme = ThemeFamily.Fluent;
        Theme.ApplyColorValues(Values(high: true));
        Assert.Equal(ThemeFamily.Fluent, Theme.ActualTheme);
        Assert.Equal(WpfThemeVariants.HighContrast, Application.Current!.RequestedThemeVariant);
        Theme.ApplyColorValues(Values(high: false));
        Theme.Theme = ThemeFamily.Aero2;
    }

    [AvaloniaFact]
    public void Opt_Out_Keeps_The_Family()
    {
        Theme.Theme = ThemeFamily.Luna;
        Theme.FollowHighContrast = false;
        Theme.ApplyColorValues(Values(high: true));
        Assert.Equal(ThemeFamily.Luna, Theme.ActualTheme);
        Theme.ApplyColorValues(Values(high: false));
        Theme.FollowHighContrast = true;
        Theme.Theme = ThemeFamily.Aero2;
    }

    [AvaloniaFact]
    public void HighContrast_Variant_Inherits_Dark()
    {
        Assert.Equal(ThemeVariant.Dark, WpfThemeVariants.HighContrast.InheritVariant);
        Assert.True(WpfThemeVariants.IsDark(WpfThemeVariants.HighContrast));
    }

    [AvaloniaFact]
    public void Classic_HighContrast_Scheme_Disables_ClientAreaAnimation()
    {
        var system = new SystemResources(ThemeFamily.Classic, ColorSchemes.HighContrastBlack);
        Assert.True(system.TryGetResource("SystemParameters.ClientAreaAnimation", ThemeVariant.Light, out var v));
        Assert.Equal(false, v);
        Assert.True(system.TryGetResource("SystemParameters.HighContrast", ThemeVariant.Light, out var hc));
        Assert.Equal(true, hc);
    }
}
