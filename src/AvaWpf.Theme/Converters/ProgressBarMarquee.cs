// Ported from WPF $W/Themes/Shared/Microsoft/Windows/Themes/ProgressBarBrushConverter.cs (MIT, see NOTICE.md).
using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvaWpf.Converters;

/// <summary>
/// Steps the indeterminate marquee of <see cref="ProgressBarBrushConverter"/> one block every 100 ms with one shared
/// timer. Brushes are held weakly; the timer stops when none are left. Each step sets a new TranslateTransform, because
/// Avalonia does not render a change to the X of a DrawingBrush's existing transform.
/// </summary>
internal static class ProgressBarMarquee
{
    /// <summary>The time each marquee position is shown (WPF's 100 ms per key frame).</summary>
    public static readonly TimeSpan Step = TimeSpan.FromMilliseconds(100);

    private static readonly List<(WeakReference<TileBrush> Target, int Blocks, double BlockTotal, int Index)> s_entries = new();
    private static DispatcherTimer? s_timer;

    /// <summary>Starts stepping <paramref name="brush"/> from X = 0 by <paramref name="blockTotal"/> per step, wrapping after <paramref name="blocks"/> steps.</summary>
    /// <param name="brush">The brush to animate.</param>
    /// <param name="blocks">The number of positions in one cycle.</param>
    /// <param name="blockTotal">The distance of one step (block plus gap).</param>
    public static void Start(TileBrush brush, int blocks, double blockTotal)
    {
        if (blocks <= 0)
        {
            return;
        }

        brush.Transform = new TranslateTransform();
        s_entries.Add((new WeakReference<TileBrush>(brush), blocks, blockTotal, 0));

        if (s_timer is null)
        {
            s_timer = new DispatcherTimer(Step, DispatcherPriority.Render, OnTick);
            s_timer.Start();
        }
        else if (!s_timer.IsEnabled)
        {
            s_timer.Start();
        }
    }

    private static void OnTick(object? sender, EventArgs e)
    {
        for (var i = s_entries.Count - 1; i >= 0; i--)
        {
            var (target, blocks, blockTotal, index) = s_entries[i];
            if (!target.TryGetTarget(out var brush))
            {
                s_entries.RemoveAt(i);
                continue;
            }

            // Discrete key frames at i × blockTotal for i = 1..blocks; the last one ends the cycle and X returns to 0.
            index = (index + 1) % blocks;
            brush.Transform = new TranslateTransform(index * blockTotal, 0);
            s_entries[i] = (target, blocks, blockTotal, index);
        }

        if (s_entries.Count == 0)
        {
            s_timer?.Stop();
        }
    }
}
