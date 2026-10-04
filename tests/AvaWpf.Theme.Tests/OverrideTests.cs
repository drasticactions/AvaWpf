using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>SystemColor overrides, the accent and the app-facing aliases.</summary>
public class OverrideTests
{
    private static AvaWpfTheme Theme => TestApplication.Instance.Theme;

    [AvaloniaFact]
    public void SystemColor_Override_Applies_To_Color_And_Brush_In_Every_Variant()
    {
        var border = new Border();
        var window = new Window { Content = border };
        window.Show();
        Theme.SystemColorOverrides["Highlight"] = Colors.Red;
        foreach (var v in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            Assert.Equal(Colors.Red, (Color)border.FindResource(v, "SystemColors.HighlightColor")!);
            Assert.Equal(Colors.Red, ((ISolidColorBrush)border.FindResource(v, "SystemColors.HighlightBrush")!).Color);
        }

        Theme.SystemColorOverrides.Remove("Highlight");
        Assert.NotEqual(Colors.Red, (Color)border.FindResource(ThemeVariant.Light, "SystemColors.HighlightColor")!);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2, "#0078D7")]
    [InlineData(ThemeFamily.Fluent, "#0078D4")]
    public void Default_Accent_Per_Family(ThemeFamily family, string accent)
    {
        Theme.Theme = family;
        Assert.Equal(Color.Parse(accent), Theme.AccentRamp.Accent);
        Theme.AccentColor = Colors.Purple;
        Assert.Equal(Colors.Purple, Theme.AccentRamp.Accent);
        Theme.AccentColor = null;
        Theme.Theme = ThemeFamily.Aero2;
    }

    [AvaloniaTheory]
    [InlineData("WpfWindowBackgroundBrush")]
    [InlineData("WpfControlForegroundBrush")]
    [InlineData("WpfAccentBrush")]
    [InlineData("WpfBorderBrush")]
    [InlineData("WpfHighlightBrush")]
    [InlineData("WpfGrayTextBrush")]
    public void App_Aliases_Resolve_To_Brushes_In_Every_Family(string key)
    {
        foreach (var family in System.Enum.GetValues<ThemeFamily>())
        {
            var r = new ThemeResources(family, ColorSchemes.DefaultFor(family), null);
            Assert.True(r.TryGetResource(key, ThemeVariant.Light, out var value), $"{key} in {family}");
            Assert.IsAssignableFrom<IBrush>(value);
        }
    }
}
