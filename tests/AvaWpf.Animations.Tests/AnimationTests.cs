using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Xunit;

namespace AvaWpf.Animations.Tests;

public class AnimationTests
{
    private static Window Host(Control content)
    {
        var w = new Window { Content = content, Width = 300, Height = 200 };
        w.Show();
        return w;
    }

    [AvaloniaFact]
    public async Task Window_Open_Ends_At_Identity_With_Full_Opacity()
    {
        WpfAnimations.TimeScale = 0;
        var root = new Border();
        Host(root);
        await WindowAnimations.PlayOpen(root, WindowMotion.Windows10);
        Assert.Equal(1, root.Opacity);
        Assert.True(root.RenderTransform is null or TransformOperations { IsIdentity: true });
    }

    [AvaloniaFact]
    public async Task Window_Close_Ends_Hidden_And_Reset_Restores()
    {
        WpfAnimations.TimeScale = 0;
        var root = new Border();
        Host(root);
        await WindowAnimations.PlayClose(root, WindowMotion.Windows11);
        Assert.Equal(0, root.Opacity);
        WindowAnimations.Reset(root);
        Assert.Equal(1, root.Opacity);
    }

    [AvaloniaFact]
    public async Task No_Motion_For_Classic_Era()
    {
        var root = new Border();
        Host(root);
        await WindowAnimations.PlayOpen(root, WindowMotion.None);
        Assert.Equal(1, root.Opacity);
    }

    [AvaloniaFact]
    public async Task Superseded_Run_Reports_False_And_Last_Wins()
    {
        WpfAnimations.TimeScale = 1;
        try
        {
            var root = new Border();
            Host(root);
            var first = AnimationRunner.Run(root, [AnimationRunner.Opacity(0, 1, 0, 1000, WpfEasing.Linear)], System.Threading.CancellationToken.None);
            var second = AnimationRunner.Run(root, [AnimationRunner.Opacity(1, 0.5, 0, 1, WpfEasing.Linear)], System.Threading.CancellationToken.None);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.True(first.IsCompleted);
            Assert.False(await first);
            for (var i = 0; i < 50 && !second.IsCompleted; i++)
            {
                Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick(16);
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            }

            Assert.True(await second);
            Assert.Equal(0.5, root.Opacity, 3);
        }
        finally
        {
            WpfAnimations.TimeScale = 0;
        }
    }

    [AvaloniaFact]
    public void ClientAreaAnimation_Resource_Disables_Motion()
    {
        var root = new Border();
        var w = Host(root);
        Assert.True(WpfAnimations.IsClientAreaAnimationEnabled(root));
        w.Resources[WpfAnimations.ClientAreaAnimationKey] = false;
        Assert.False(WpfAnimations.IsClientAreaAnimationEnabled(root));
    }

    [AvaloniaFact]
    public void PopupAnimation_Duration_Matches_WPF()
    {
        // WPF Popup.AnimationDelay = 150 ms (Popup.cs).
        Assert.Equal(TimeSpan.FromMilliseconds(150), PopupAnimation.Duration);
    }

    [AvaloniaFact]
    public void PopupAnimation_Fade_Ends_Opaque()
    {
        WpfAnimations.TimeScale = 0;
        var content = new Border();
        Host(content);
        PopupAnimation.Play(content, PopupAnimationKind.Fade, null);
        Assert.Equal(1, content.Opacity);
    }

    [AvaloniaFact]
    public void PopupAnimation_Kind_Attached_Property_Round_Trips()
    {
        var c = new ContextMenu();
        PopupAnimation.SetKind(c, PopupAnimationKind.Slide);
        Assert.Equal(PopupAnimationKind.Slide, PopupAnimation.GetKind(c));
    }

    [AvaloniaFact]
    public void ProgressGlow_Keyframes_Match_WPF()
    {
        // ProgressBar.UpdateAnimation: start -glow, end indicator + glow, travel (int)(end-start)/200 s, +1 s pause.
        var (start, end, travel, period) = ProgressBarGlow.Describe(indicatorWidth: 300, glowWidth: 72);
        Assert.Equal(-72, start);
        Assert.Equal(372, end);
        Assert.Equal(TimeSpan.FromSeconds(444 / 200.0), travel);
        Assert.Equal(travel + TimeSpan.FromSeconds(1), period);
    }

    [AvaloniaFact]
    public void Fluent_Durations_Match_WPF()
    {
        // PresentationFramework.Fluent/Resources/Variables.xaml.
        Assert.Equal(TimeSpan.FromMilliseconds(250), FluentTimings.Normal);
        Assert.Equal(TimeSpan.FromMilliseconds(167), FluentTimings.Fast);
        Assert.Equal(TimeSpan.FromMilliseconds(168), FluentTimings.FastAfter);
        Assert.Equal(TimeSpan.FromMilliseconds(83), FluentTimings.Faster);
        var spline = Assert.IsType<Avalonia.Animation.Easings.SplineEasing>(WpfEasing.FastOutSlowIn);
        Assert.Equal((0d, 0d, 0d, 1d), (spline.X1, spline.Y1, spline.X2, spline.Y2));
    }

    [AvaloniaFact]
    public void ChromeAnimator_Jumps_To_End_Without_Motion()
    {
        WpfAnimations.TimeScale = 0;
        var root = new Border();
        Host(root);
        var animator = new ChromeAnimator(root);
        var value = animator.CreateDouble(0);
        value.AnimateTo(1, TimeSpan.FromSeconds(0.3));
        Assert.Equal(1, value.Value);
        Assert.False(value.IsAnimating);
    }

    [AvaloniaFact]
    public void ChromeAnimator_Rapid_Retarget_Leaves_End_State()
    {
        WpfAnimations.TimeScale = 0;
        var root = new Border();
        Host(root);
        var animator = new ChromeAnimator(root);
        var color = animator.CreateColor(Colors.White);
        for (var i = 0; i < 20; i++)
        {
            color.AnimateTo(i % 2 == 0 ? Colors.Red : Colors.Blue, TimeSpan.FromSeconds(0.3));
        }

        Assert.Equal(Colors.Blue, color.Value);
        Assert.False(animator.IsAnimating);
    }

    [Theory]
    [InlineData(PlacementMode.Bottom, false)]
    [InlineData(PlacementMode.BottomEdgeAlignedLeft, false)]
    [InlineData(PlacementMode.Right, false)]
    [InlineData(PlacementMode.Pointer, false)]
    [InlineData(PlacementMode.Top, true)]
    [InlineData(PlacementMode.TopEdgeAlignedLeft, true)]
    [InlineData(PlacementMode.TopEdgeAlignedRight, true)]
    public void Slide_Direction_Follows_The_Placement(PlacementMode placement, bool above)
    {
        Assert.Equal(above, PopupAnimation.OpensAbove(placement, Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.Bottom));
    }

    [Fact]
    public void Anchor_And_Gravity_Opens_Above_For_Top_Gravity()
    {
        Assert.True(PopupAnimation.OpensAbove(PlacementMode.AnchorAndGravity, Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.TopLeft));
        Assert.False(PopupAnimation.OpensAbove(PlacementMode.AnchorAndGravity, Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight));
    }

    [AvaloniaFact]
    public void Slide_Below_The_Target_Starts_Above_Its_Final_Place()
    {
        WpfAnimations.TimeScale = 10;
        try
        {
            var content = new Border { Width = 100, Height = 80 };
            Host(content);
            PopupAnimation.Play(content, PopupAnimationKind.Slide, null, opensAbove: false);
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var offset = (content.RenderTransform as TransformOperations)?.Value.M32 ?? 0;
            Assert.True(offset < 0, $"the content starts at {offset}; a popup below its target must slide down from above");
        }
        finally
        {
            WpfAnimations.TimeScale = 0;
        }
    }

    [AvaloniaFact]
    public void Indeterminate_Glow_Is_Painted_In_The_Foreground()
    {
        // WPF ProgressBar.SetProgressBarGlowElementBrush: the template's white glow is invisible on a pale track, so an
        // indeterminate bar paints it in its Foreground, fading out at both ends.
        Rectangle? glow = null;
        var bar = new Avalonia.Controls.ProgressBar
        {
            Width = 200,
            Height = 16,
            Foreground = Avalonia.Media.Brushes.Green,
            Template = new Avalonia.Controls.Templates.FuncControlTemplate<Avalonia.Controls.ProgressBar>((_, scope) =>
            {
                glow = new Rectangle { Name = ProgressBarGlow.GlowPartName, Width = 50 };
                glow.SetValue(Shape.FillProperty, Avalonia.Media.Brushes.White, Avalonia.Data.BindingPriority.Template); // as a XAML template does
                var indicator = new Border { Name = ProgressBarGlow.IndicatorPartName, Child = glow };
                scope.Register(glow.Name, glow);
                scope.Register(indicator.Name, indicator);
                return indicator;
            }),
        };
        ProgressBarGlow.SetIsEnabled(bar, true);
        var window = new Window { Content = bar };
        window.Show();

        bar.IsIndeterminate = true;
        var fill = Assert.IsType<Avalonia.Media.LinearGradientBrush>(glow!.Fill);
        Assert.Equal(Avalonia.Media.Colors.Green, fill.GradientStops[1].Color);
        Assert.Equal(Avalonia.Media.Colors.Transparent, fill.GradientStops[0].Color);

        bar.IsIndeterminate = false;
        Assert.Same(Avalonia.Media.Brushes.White, glow.Fill);
        window.Close();
    }

    [AvaloniaFact]
    public void Glow_Restarts_Mid_Pass_Without_A_Negative_Delay()
    {
        // A relayout while the glow is part way across used to start the new cycle with a negative Delay, which
        // Avalonia rejects ("Delay value cannot be negative") on the UI thread (a publish progress page crashed).
        WpfAnimations.TimeScale = 1;
        Rectangle? glow = null;
        var bar = new Avalonia.Controls.ProgressBar
        {
            Width = 200,
            Height = 16,
            Value = 50,
            Template = new Avalonia.Controls.Templates.FuncControlTemplate<Avalonia.Controls.ProgressBar>((_, scope) =>
            {
                glow = new Rectangle { Name = ProgressBarGlow.GlowPartName, Width = 50, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
                var indicator = new Border { Name = ProgressBarGlow.IndicatorPartName, Child = glow };
                scope.Register(glow.Name, glow);
                scope.Register(indicator.Name, indicator);
                return indicator;
            }),
        };
        ProgressBarGlow.SetIsEnabled(bar, true);
        var window = new Window { Content = bar };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // Put the glow mid-pass, then change the indicator size so the cycle is rebuilt from that phase.
        glow!.Margin = new Thickness(40, 0, 0, 0);
        bar.Width = 260;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        bar.Width = 300;
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.Close();
    }
}
