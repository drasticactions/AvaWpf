// Ported from WPF $W/PresentationFramework/System/Windows/Controls/ProgressBar.cs (MIT, see NOTICE.md).
using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace AvaWpf.Animations;

/// <summary>
/// The moving <c>PART_GlowRect</c> glow of a progress bar, as WPF's <c>ProgressBar.UpdateAnimation</c>: it crosses
/// <c>PART_Indicator</c> at 200 px/s, pauses 1 s and repeats. An indeterminate bar paints the glow in its Foreground.
/// </summary>
public static class ProgressBarGlow
{
    /// <summary>WPF's glow speed: 200 px per second.</summary>
    public const double PixelsPerSecond = 200;

    /// <summary>WPF's pause between two passes: 1 s.</summary>
    public static readonly TimeSpan Pause = TimeSpan.FromSeconds(1);

    /// <summary>The template part that moves.</summary>
    public const string GlowPartName = "PART_GlowRect";

    /// <summary>The template part the glow crosses.</summary>
    public const string IndicatorPartName = "PART_Indicator";

    /// <summary>Defines the <c>IsEnabled</c> attached property.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ProgressBar, bool>("IsEnabled", typeof(ProgressBarGlow));

    private static readonly AttachedProperty<GlowState?> s_stateProperty =
        AvaloniaProperty.RegisterAttached<ProgressBar, GlowState?>("GlowState", typeof(ProgressBarGlow));

    static ProgressBarGlow()
    {
        IsEnabledProperty.Changed.AddClassHandler<ProgressBar>(OnIsEnabledChanged);
    }

    /// <summary>Gets whether the glow runs on a progress bar.</summary>
    public static bool GetIsEnabled(ProgressBar bar) => bar.GetValue(IsEnabledProperty);

    /// <summary>Sets whether the glow runs on a progress bar.</summary>
    public static void SetIsEnabled(ProgressBar bar, bool value) => bar.SetValue(IsEnabledProperty, value);

    /// <summary>The start and end offsets, travel time and period of one pass, computed as WPF does.</summary>
    public static (double Start, double End, TimeSpan Travel, TimeSpan Period) Describe(double indicatorWidth, double glowWidth)
    {
        var end = indicatorWidth + glowWidth;
        var start = -glowWidth;
        var travel = TimeSpan.FromSeconds((int)(end - start) / PixelsPerSecond);
        return (start, end, travel, travel + Pause);
    }

    private static void OnIsEnabledChanged(ProgressBar bar, AvaloniaPropertyChangedEventArgs e)
    {
        bar.GetValue(s_stateProperty)?.Dispose();
        bar.ClearValue(s_stateProperty);
        if (e.GetNewValue<bool>())
        {
            bar.SetValue(s_stateProperty, new GlowState(bar));
        }
    }

    private sealed class GlowState : IDisposable
    {
        private readonly ProgressBar _bar;
        private Control? _glow;
        private Control? _indicator;
        private CancellationTokenSource? _run;
        private (double, double) _last;

        public GlowState(ProgressBar bar)
        {
            _bar = bar;
            bar.TemplateApplied += OnTemplateApplied;
            bar.LayoutUpdated += OnLayoutUpdated;
            bar.PropertyChanged += OnBarPropertyChanged;
        }

        public void Dispose()
        {
            _bar.TemplateApplied -= OnTemplateApplied;
            _bar.LayoutUpdated -= OnLayoutUpdated;
            _bar.PropertyChanged -= OnBarPropertyChanged;
            Stop();
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e)
        {
            Stop();
            _glow = e.NameScope.Find<Control>(GlowPartName);
            _indicator = e.NameScope.Find<Control>(IndicatorPartName);
            _last = default;
            UpdateBrush();
        }

        private void OnBarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.IsVisibleProperty)
            {
                _last = default;
            }
            else if (e.Property == ProgressBar.IsIndeterminateProperty || e.Property == TemplatedControl.ForegroundProperty)
            {
                UpdateBrush();
            }
        }

        // Sizes are read after layout, never mid-arrange.
        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (_glow is null || _indicator is null)
            {
                return;
            }

            var key = (_indicator.Bounds.Width, _glow.Bounds.Width);
            if (key == _last)
            {
                return;
            }

            _last = key;
            Update();
        }

        private void Update()
        {
            if (_glow is null || _indicator is null)
            {
                return;
            }

            var glowWidth = _glow.Bounds.Width;
            var indicatorWidth = _indicator.Bounds.Width;
            if (!_bar.IsVisible || glowWidth <= 0 || indicatorWidth <= 0)
            {
                Stop();
                return;
            }

            var (start, end, travel, period) = Describe(indicatorWidth, glowWidth);

            // A pass under way continues part way through, so the glow does not jump.
            var left = _glow.Margin.Left;
            var offset = left > start && left < end - 1 ? TimeSpan.FromSeconds((left - start) / PixelsPerSecond) : TimeSpan.Zero;

            Stop();
            if (!WpfAnimations.IsMotionEnabled(_bar))
            {
                _glow.Margin = new Thickness(start, 0, 0, 0);
                return;
            }

            var scale = WpfAnimations.TimeScale;
            var animation = new Animation
            {
                Duration = TimeSpan.FromTicks((long)(period.Ticks * scale)),
                IterationCount = IterationCount.Infinite,
                Delay = -TimeSpan.FromTicks((long)(offset.Ticks * scale)),
                Children =
                {
                    new KeyFrame { KeyTime = TimeSpan.Zero, Setters = { new Setter(Layoutable.MarginProperty, new Thickness(start, 0, 0, 0)) } },
                    new KeyFrame { KeyTime = TimeSpan.FromTicks((long)(travel.Ticks * scale)), Setters = { new Setter(Layoutable.MarginProperty, new Thickness(end, 0, 0, 0)) } },
                    new KeyFrame { Cue = new Cue(1), Setters = { new Setter(Layoutable.MarginProperty, new Thickness(end, 0, 0, 0)) } },
                },
            };
            _run = new CancellationTokenSource();
            _ = animation.RunAsync(_glow, _run.Token);
        }

        // WPF ProgressBar.SetProgressBarGlowElementBrush. Clearing the local values restores the template's glow.
        private void UpdateBrush()
        {
            if (_glow is not Shape glow)
            {
                return;
            }

            glow.ClearValue(Visual.OpacityMaskProperty);
            glow.ClearValue(Shape.FillProperty);
            if (!_bar.IsIndeterminate)
            {
                return;
            }

            if (_bar.Foreground is ISolidColorBrush solid)
            {
                glow.Fill = Fade(solid.Color);
            }
            else
            {
                glow.OpacityMask = Fade(Colors.Black);
                glow.Fill = _bar.Foreground;
            }
        }

        private static LinearGradientBrush Fade(Color color) => new()
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Colors.Transparent, 0.0),
                new GradientStop(color, 0.4),
                new GradientStop(color, 0.6),
                new GradientStop(Colors.Transparent, 1.0),
            },
        };

        private void Stop()
        {
            _run?.Cancel();
            _run?.Dispose();
            _run = null;
        }
    }
}
