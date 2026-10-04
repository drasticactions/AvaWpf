using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>Text lines take WPF's height: rounded to the nearest device pixel, not up.</summary>
public class TextLineRoundingTests
{
    private const string Tahoma = "Tahoma, fonts:AvaWpf#Tahoma";

    [AvaloniaFact]
    public void Tahoma_11px_Line_Rounds_Down_Like_WPF()
    {
        // Tahoma's line is 13.28 px at 11 px: WPF lays it out 13 px tall.
        var text = new TextBlock { Text = "Ada Lovelace", FontFamily = new FontFamily(Tahoma), FontSize = 11 };
        using var scene = ChromeScene.Show(ThemeFamily.Luna, null, text, 200, 60);
        Assert.Equal(13, text.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Line_That_Rounds_Up_Is_Left_Alone()
    {
        // 13.28 px at 11 px is 15.69 px at 13 px, which rounds up in both.
        var text = new TextBlock { Text = "Ada Lovelace", FontFamily = new FontFamily(Tahoma), FontSize = 13 };
        using var scene = ChromeScene.Show(ThemeFamily.Luna, null, text, 200, 60);
        Assert.True(double.IsNaN(text.LineHeight));
        Assert.Equal(16, text.Bounds.Height);
    }

    [AvaloniaFact]
    public void The_Apps_Own_Line_Height_Is_Kept()
    {
        var text = new TextBlock { Text = "Ada Lovelace", FontFamily = new FontFamily(Tahoma), FontSize = 11, LineHeight = 20 };
        using var scene = ChromeScene.Show(ThemeFamily.Luna, null, text, 200, 60);
        text.FontSize = 11.5;
        Assert.Equal(20, text.LineHeight);

        var inherited = new TextBlock { Text = "Ada Lovelace", FontFamily = new FontFamily(Tahoma), FontSize = 11 };
        var panel = new StackPanel { Children = { inherited } };
        TextBlock.SetLineHeight(panel, 24);
        using var second = ChromeScene.Show(ThemeFamily.Luna, null, panel, 200, 60);
        Assert.Equal(24, inherited.LineHeight);
    }

    [AvaloniaFact]
    public void A_Font_Change_Rounds_Again()
    {
        var text = new TextBlock { Text = "Ada Lovelace", FontFamily = new FontFamily(Tahoma), FontSize = 11 };
        using var scene = ChromeScene.Show(ThemeFamily.Luna, null, text, 200, 60);
        Assert.Equal(13, text.LineHeight);
        text.FontSize = 13;
        Assert.True(double.IsNaN(text.LineHeight));
    }
}
