using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>WPF's Slider track press: a LargeChange step towards the press, repeating while held (SliderState).</summary>
public class SliderTrackTests
{
    private static (ChromeScene Scene, Slider Slider) Show(Slider slider)
    {
        slider.Width = 300;
        return (ChromeScene.Show(ThemeFamily.Aero2, null, slider, 340, 60), slider);
    }

    private static Point Near(ChromeScene scene, Slider slider, double fraction) =>
        slider.TranslatePoint(new Point(slider.Bounds.Width * fraction, slider.Bounds.Height / 2), scene.Window)!.Value;

    [AvaloniaFact]
    public void Track_Press_Steps_By_LargeChange()
    {
        var (scene, slider) = Show(new Slider { Minimum = 0, Maximum = 100, Value = 30, LargeChange = 10 });
        using var _ = scene;
        var right = Near(scene, slider, 0.95);

        scene.Window.MouseDown(right, MouseButton.Left);
        scene.Window.MouseUp(right, MouseButton.Left);
        Assert.Equal(40, slider.Value);

        var left = Near(scene, slider, 0.02);
        scene.Window.MouseDown(left, MouseButton.Left);
        scene.Window.MouseUp(left, MouseButton.Left);
        Assert.Equal(30, slider.Value);
    }

    [AvaloniaFact]
    public void Track_Press_On_A_Snapping_Slider_Moves_To_The_Next_Tick()
    {
        var (scene, slider) = Show(new Slider { Minimum = 0, Maximum = 100, Value = 50, LargeChange = 1, TickFrequency = 10, IsSnapToTickEnabled = true });
        using var _ = scene;
        var right = Near(scene, slider, 0.95);

        scene.Window.MouseDown(right, MouseButton.Left);
        scene.Window.MouseUp(right, MouseButton.Left);

        Assert.Equal(60, slider.Value);
    }

    [AvaloniaFact]
    public void Holding_The_Track_Repeats_After_The_Delay()
    {
        var (scene, slider) = Show(new Slider { Minimum = 0, Maximum = 100, Value = 10, LargeChange = 10 });
        using var _ = scene;
        var right = Near(scene, slider, 0.95);

        scene.Window.MouseDown(right, MouseButton.Left);
        Assert.Equal(20, slider.Value);
        // Run the dispatcher loop (timers only fire there) past the repeat delay.
        var frame = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => frame.Continue = false, SliderState.RepeatDelay + System.TimeSpan.FromMilliseconds(300));
        Dispatcher.UIThread.PushFrame(frame);

        scene.Window.MouseUp(right, MouseButton.Left);
        Assert.True(slider.Value > 20, $"value {slider.Value}");
    }

    [AvaloniaFact]
    public void Holding_Stops_When_The_Thumb_Reaches_The_Pointer()
    {
        var (scene, slider) = Show(new Slider { Minimum = 0, Maximum = 100, Value = 0, LargeChange = 10 });
        using var _ = scene;
        var middle = Near(scene, slider, 0.5);

        scene.Window.MouseDown(middle, MouseButton.Left);
        var frame = new DispatcherFrame();
        DispatcherTimer.RunOnce(() => frame.Continue = false, System.TimeSpan.FromSeconds(1.5));
        Dispatcher.UIThread.PushFrame(frame);
        scene.Window.MouseUp(middle, MouseButton.Left);

        Assert.InRange(slider.Value, 40, 60);
    }

    [AvaloniaFact]
    public void IsMoveToPointEnabled_Jumps_To_The_Press()
    {
        var slider = new Slider { Minimum = 0, Maximum = 100, Value = 30, LargeChange = 10 };
        SliderState.SetIsMoveToPointEnabled(slider, true);
        var (scene, _) = Show(slider);
        using var __ = scene;
        var right = Near(scene, slider, 0.95);

        scene.Window.MouseDown(right, MouseButton.Left);
        scene.Window.MouseUp(right, MouseButton.Left);

        Assert.True(slider.Value > 80, $"value {slider.Value}");
    }
}
