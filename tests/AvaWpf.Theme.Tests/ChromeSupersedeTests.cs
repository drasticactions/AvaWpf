using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
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
