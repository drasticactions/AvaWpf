// Ported from WPF $W/Themes/PresentationFramework.Aero/Microsoft/Windows/Themes/ProgressBarHighlightConverter.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaWpf.Animations;

namespace AvaWpf.Converters;

/// <summary>
/// Creates the moving highlight brush of an Aero progress bar: 200 px/s to the right, then a 1 s pause, forever.
/// </summary>
/// <remarks>
/// No WPF template uses this converter. The animation stops when the returned brush is garbage collected.
/// </remarks>
public class ProgressBarHighlightConverter : IMultiValueConverter
{
    /// <summary>The speed of the highlight, in pixels per second.</summary>
    private const double PixelsPerSecond = 200.0;

    /// <summary>The pause between two passes of the highlight.</summary>
    private static readonly TimeSpan s_pauseTime = TimeSpan.FromSeconds(1.0);

    private static readonly ConditionalWeakTable<DrawingBrush, Lifetime> s_lifetimes = new();

    /// <summary>Creates the brush for the progress bar.</summary>
    /// <param name="values">The highlight brush (<see cref="IBrush"/>), the width and the height (<see cref="double"/>).</param>
    /// <param name="targetType">The type of the target (unused).</param>
    /// <param name="parameter">The converter parameter (unused).</param>
    /// <param name="culture">The culture (unused).</param>
    /// <returns>The animated brush, or null when the inputs are not valid or the size is empty.</returns>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null ||
            values.Count != 3 ||
            values[0] is not IBrush brush ||
            values[1] is not double width ||
            values[2] is not double height)
        {
            return null;
        }

        if (width <= 0.0 || double.IsInfinity(width) || double.IsNaN(width) ||
            height <= 0.0 || double.IsInfinity(height) || double.IsNaN(height))
        {
            return null;
        }

        // A brush twice the track width, which animates to the right:
        //
        // +-------------+..............
        // | highlight   | empty       :
        // +-------------+.............:
        var twiceWidth = width * 2.0;
        var viewport = new RelativeRect(-width, 0, twiceWidth, height, RelativeUnit.Absolute);
        var translation = new TranslateTransform();
        var newBrush = new DrawingBrush
        {
            Drawing = new GeometryDrawing { Brush = brush, Geometry = new RectangleGeometry(new Rect(-width, 0, width, height)) },
            SourceRect = viewport,
            DestinationRect = viewport,
            TileMode = TileMode.None,
            Stretch = Stretch.None,
            Transform = translation,
        };

        var scale = WpfAnimations.TimeScale;
        if (!WpfAnimations.IsEnabled || scale <= 0)
        {
            return newBrush;
        }

        var translateTime = TimeSpan.FromSeconds(twiceWidth / PixelsPerSecond);
        var animation = new Animation
        {
            Duration = TimeSpan.FromTicks((long)((translateTime + s_pauseTime).Ticks * scale)),
            IterationCount = IterationCount.Infinite,
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(TranslateTransform.XProperty, 0.0) } },
                new KeyFrame { KeyTime = TimeSpan.FromTicks((long)(translateTime.Ticks * scale)), Setters = { new Setter(TranslateTransform.XProperty, twiceWidth) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(TranslateTransform.XProperty, twiceWidth) } },
            },
        };

        var lifetime = new Lifetime();
        s_lifetimes.AddOrUpdate(newBrush, lifetime);

        // Avalonia faults the task of a looping RunAsync at once (the animation still runs); observe the fault.
        _ = animation.RunAsync(translation, lifetime.Token).ContinueWith(
            static t => _ = t.Exception,
            CancellationToken.None,
            System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted | System.Threading.Tasks.TaskContinuationOptions.ExecuteSynchronously,
            System.Threading.Tasks.TaskScheduler.Default);
        return newBrush;
    }

    /// <summary>Cancels the animation of a brush once the brush is garbage collected.</summary>
    private sealed class Lifetime
    {
        private readonly CancellationTokenSource _cts = new();

        ~Lifetime()
        {
            var cts = _cts;
            Dispatcher.UIThread.Post(() =>
            {
                cts.Cancel();
                cts.Dispose();
            });
        }

        public CancellationToken Token => _cts.Token;
    }
}
