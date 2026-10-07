using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using AvaWpf.Animations;
using AvaWpf.Chrome.Aero;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// Chrome supersede: rapid hover in and out on the Aero ButtonChrome, with motion on (TimeScale 1) and real
/// frames, leaves the chrome exactly in the end state: once the animations have run out, it renders pixel for pixel
/// as a chrome that was set to that state with motion off. A superseded hover-in or hover-out must not leave its
/// overlay or inner-border colors behind.
/// </summary>
public class ChromeSupersedeTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rapid_Hover_In_And_Out_Ends_In_The_Final_State(bool final)
    {
        byte[] animated;
        WpfAnimations.TimeScale = 1;
        try
        {
            var chrome = NewChrome();
            using var scene = ChromeScene.Show(ThemeFamily.Aero, null, chrome, 91, 39);
            for (var i = 0; i < 6; i++)
            {
                chrome.RenderMouseOver = true;
                RunFrames(TimeSpan.FromMilliseconds(30));
                chrome.RenderMouseOver = false;
                RunFrames(TimeSpan.FromMilliseconds(i % 2 == 0 ? 10 : 90));
            }

            chrome.RenderMouseOver = true;
            RunFrames(TimeSpan.FromMilliseconds(50));
            chrome.RenderMouseOver = final;

            // ChromeTimings.HoverIn is 300 ms and HoverOut 200 ms; give both time to finish.
            RunFrames(TimeSpan.FromMilliseconds(700));
            animated = Capture(scene);
        }
        finally
        {
            WpfAnimations.TimeScale = 0;
        }

        var still = NewChrome();
        still.RenderMouseOver = final;
        using var reference = ChromeScene.Show(ThemeFamily.Aero, null, still, 91, 39);
        var expected = Capture(reference);
        Assert.Equal(expected.Length, animated.Length);
        var differing = 0;
        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i] != animated[i])
            {
                differing++;
            }
        }

        Assert.True(differing == 0, $"{differing} channel values differ from the static RenderMouseOver={final} chrome");
    }

    /// <summary>The check above is not vacuous: half way through a hover-in the chrome differs from its end state.</summary>
    [AvaloniaFact]
    public void Hover_In_Animates_With_Motion_On()
    {
        var still = NewChrome();
        still.RenderMouseOver = true;
        byte[] end;
        using (var reference = ChromeScene.Show(ThemeFamily.Aero, null, still, 91, 39))
        {
            end = Capture(reference);
        }

        WpfAnimations.TimeScale = 1;
        try
        {
            var chrome = NewChrome();
            using var scene = ChromeScene.Show(ThemeFamily.Aero, null, chrome, 91, 39);
            chrome.RenderMouseOver = true;
            RunFrames(TimeSpan.FromMilliseconds(60));
            Assert.NotEqual(end, Capture(scene));
            RunFrames(TimeSpan.FromMilliseconds(500));
            Assert.Equal(end, Capture(scene));
        }
        finally
        {
            WpfAnimations.TimeScale = 0;
        }
    }

    /// <summary>
    /// Hovering a checked, indeterminate or checked round bullet keeps its glyph, also after a resource change has
    /// dropped the chrome's animation state: once the hover-in has run, the chrome renders as one set to that state with
    /// motion off. As WPF, the animated inner border of a radio button is the check box one, and the indeterminate
    /// highlight keeps one stop of its base color, so those differ a little from the static look; a missing glyph
    /// differs by 88 or more in a channel.
    /// </summary>
    [AvaloniaTheory]
    [InlineData(true, false)]
    [InlineData(null, false)]
    [InlineData(true, true)]
    public void Hovering_A_Bullet_Keeps_Its_Glyph(bool? isChecked, bool isRound)
    {
        var still = NewBullet(isChecked, isRound);
        still.RenderMouseOver = true;
        var chrome = NewBullet(isChecked, isRound);
        var largest = LargestDifferenceAfter(still, chrome, 40, 40, () => chrome.RenderMouseOver = true);
        Assert.True(largest <= 32, $"a channel differs by {largest} from the static hovered chrome");
    }

    /// <summary>The hover-in on a checked bullet still animates: part way through, the chrome is not in its end state.</summary>
    [AvaloniaFact]
    public void Hovering_A_Checked_Bullet_Animates()
    {
        var still = NewBullet(true, false);
        still.RenderMouseOver = true;
        byte[] end;
        using (var reference = ChromeScene.Show(ThemeFamily.Aero, null, still, 40, 40))
        {
            end = Capture(reference);
        }

        WpfAnimations.TimeScale = 1;
        try
        {
            var chrome = NewBullet(true, false);
            using var scene = ChromeScene.Show(ThemeFamily.Aero, null, chrome, 40, 40);
            RunFrames(TimeSpan.FromMilliseconds(100));
            chrome.RenderMouseOver = true;
            RunFrames(TimeSpan.FromMilliseconds(60));
            Assert.NotEqual(end, Capture(scene));
        }
        finally
        {
            WpfAnimations.TimeScale = 0;
        }
    }

    /// <summary>
    /// A defaulted button hovered across a resource change keeps its defaulted inner border through a press: once the
    /// release has run, the chrome renders as a hovered defaulted one with motion off. The animated colors may round 1
    /// or 2 off the static ones; the plain inner border differs by far more.
    /// </summary>
    [AvaloniaFact]
    public void A_Defaulted_Button_Keeps_Its_Inner_Border_Through_A_Press()
    {
        var still = NewChrome();
        still.RenderDefaulted = true;
        still.RenderMouseOver = true;
        var chrome = NewChrome();
        chrome.RenderDefaulted = true;
        chrome.RenderMouseOver = true;
        var largest = LargestDifferenceAfter(still, chrome, 91, 39, () =>
        {
            chrome.RenderPressed = true;
            RunFrames(TimeSpan.FromMilliseconds(300));
            chrome.RenderPressed = false;
        });
        Assert.True(largest <= 4, $"a channel differs by {largest} from the static hovered defaulted chrome");
    }

    /// <summary>
    /// Renders <paramref name="still"/> with motion off; then, with motion on, shows <paramref name="chrome"/>, changes a
    /// resource on it (which drops its animation state), runs <paramref name="act"/> and lets the animations finish.
    /// Returns the largest channel difference between the two renders.
    /// </summary>
    private static int LargestDifferenceAfter(Control still, Control chrome, double width, double height, Action act)
    {
        byte[] expected;
        using (var reference = ChromeScene.Show(ThemeFamily.Aero, null, still, width, height))
        {
            expected = Capture(reference);
        }

        WpfAnimations.TimeScale = 1;
        try
        {
            using var scene = ChromeScene.Show(ThemeFamily.Aero, null, chrome, width, height);
            RunFrames(TimeSpan.FromMilliseconds(100));
            chrome.Resources["AvaWpf.Tests.Unused"] = 0;
            act();

            // ChromeTimings.HoverIn is 300 ms.
            RunFrames(TimeSpan.FromMilliseconds(600));
            var animated = Capture(scene);
            Assert.Equal(expected.Length, animated.Length);
            var largest = 0;
            for (var i = 0; i < expected.Length; i++)
            {
                largest = Math.Max(largest, Math.Abs(expected[i] - animated[i]));
            }

            return largest;
        }
        finally
        {
            WpfAnimations.TimeScale = 0;
        }
    }

    private static BulletChrome NewBullet(bool? isChecked, bool isRound) => new()
    {
        IsChecked = isChecked,
        IsRound = isRound,
        [!BulletChrome.BackgroundProperty] = new DynamicResourceExtension("Aero.CheckBoxFillNormal"),
        [!BulletChrome.BorderBrushProperty] = new DynamicResourceExtension("Aero.CheckBoxStroke"),
    };

    private static ButtonChrome NewChrome() => new()
    {
        Width = 75,
        Height = 23,
        RoundCorners = true,
        [!ButtonChrome.BackgroundProperty] = new DynamicResourceExtension("Aero.ButtonNormalBackground"),
        [!ButtonChrome.BorderBrushProperty] = new DynamicResourceExtension("Aero.ButtonNormalBorder"),
    };

    /// <summary>Runs the dispatcher (animation frames on the wall clock) for <paramref name="duration"/>.</summary>
    private static void RunFrames(TimeSpan duration)
    {
        using var cts = new CancellationTokenSource(duration);
        Dispatcher.UIThread.MainLoop(cts.Token);
        ChromeScene.Pump(1);
    }

    private static byte[] Capture(ChromeScene scene)
    {
        ChromeScene.Pump();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        using var frame = scene.Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("nothing rendered");
        var size = frame.PixelSize;
        var stride = size.Width * 4;
        var data = new byte[stride * size.Height];
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            frame.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), data.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        return data;
    }
}
