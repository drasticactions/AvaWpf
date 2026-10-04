using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaWpf;

/// <summary>
/// WPF's Slider track press: steps the value by <see cref="Avalonia.Controls.Primitives.RangeBase.LargeChange"/>
/// towards the pointer and repeats while held, instead of Avalonia's jump to the pointer.
/// </summary>
public static class SliderState
{
    /// <summary>WPF's RepeatButton delay before repeating (SystemParameters.KeyboardDelay 1: 500 ms).</summary>
    public static readonly TimeSpan RepeatDelay = TimeSpan.FromMilliseconds(500);

    /// <summary>WPF's RepeatButton interval (SystemParameters.KeyboardSpeed 31: about 33 ms).</summary>
    public static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(1000.0 / 30);

    /// <summary>Defines the <c>IsMoveToPointEnabled</c> attached property (WPF <c>Slider.IsMoveToPointEnabled</c>).</summary>
    public static readonly AttachedProperty<bool> IsMoveToPointEnabledProperty =
        AvaloniaProperty.RegisterAttached<Slider, bool>("IsMoveToPointEnabled", typeof(SliderState));

    private static bool s_registered;
    private static Press? s_press;

    /// <summary>Gets whether a track press jumps the thumb to the pointer.</summary>
    public static bool GetIsMoveToPointEnabled(Slider slider) => slider.GetValue(IsMoveToPointEnabledProperty);

    /// <summary>Sets whether a track press jumps the thumb to the pointer.</summary>
    public static void SetIsMoveToPointEnabled(Slider slider, bool value) => slider.SetValue(IsMoveToPointEnabledProperty, value);

    internal static void EnsureRegistered()
    {
        if (s_registered)
        {
            return;
        }

        s_registered = true;

        // Tunnelling, so the press is taken before Avalonia's Slider sees it on the track button.
        InputElement.PointerPressedEvent.AddClassHandler<Slider>(OnPointerPressed, RoutingStrategies.Tunnel);
        InputElement.PointerMovedEvent.AddClassHandler<Slider>((_, e) => s_press?.Moved(e), RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<Slider>((_, e) => s_press?.End(e.Pointer), RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerCaptureLostEvent.AddClassHandler<Slider>((_, e) => s_press?.CaptureLost(e.Pointer), RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private static void OnPointerPressed(Slider slider, PointerPressedEventArgs e)
    {
        if (GetIsMoveToPointEnabled(slider) || !slider.IsEffectivelyEnabled || !e.GetCurrentPoint(slider).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var button = PageButton(slider, e.Source as Visual);
        if (button is null)
        {
            return;
        }

        e.Handled = true;

        // WPF's Slider takes the focus when one of its unfocusable parts is pressed.
        if (!slider.IsKeyboardFocusWithin)
        {
            slider.Focus(NavigationMethod.Pointer);
        }

        s_press?.Stop();
        s_press = new Press(slider, button, e.Pointer, e.GetPosition(slider));
        e.Pointer.Capture(button);
    }

    /// <summary>The track's decrease or increase button of <paramref name="slider"/> that holds <paramref name="source"/>.</summary>
    private static Button? PageButton(Slider slider, Visual? source)
    {
        for (var v = source; v is not null && v != slider; v = v.GetVisualParent())
        {
            if (v is Button { Name: "PART_DecreaseButton" or "PART_IncreaseButton" } button && button.TemplatedParent == slider)
            {
                return button;
            }
        }

        return null;
    }

    /// <summary>One step: WPF's <c>MoveToNextTick(±LargeChange)</c>.</summary>
    internal static void Step(Slider slider, bool increase)
    {
        var direction = increase ? slider.LargeChange : -slider.LargeChange;
        if (direction == 0.0)
        {
            return;
        }

        var value = slider.Value;
        var next = SnapToTick(slider, Math.Max(slider.Minimum, Math.Min(slider.Maximum, value + direction)));

        // If snapping brought the value back, move to the next tick in the direction instead.
        if (next == value && !(increase && value == slider.Maximum) && !(!increase && value == slider.Minimum))
        {
            if (slider.Ticks is { Count: > 0 } ticks)
            {
                foreach (var tick in ticks)
                {
                    if ((increase && tick > value && (tick < next || next == value))
                        || (!increase && tick < value && (tick > next || next == value)))
                    {
                        next = tick;
                    }
                }
            }
            else if (slider.TickFrequency > 0)
            {
                var tickNumber = Math.Round((value - slider.Minimum) / slider.TickFrequency) + (increase ? 1.0 : -1.0);
                next = slider.Minimum + (tickNumber * slider.TickFrequency);
            }
        }

        if (next != value)
        {
            slider.SetCurrentValue(Slider.ValueProperty, next);
        }
    }

    /// <summary>WPF's <c>Slider.SnapToTick</c>: the nearest tick when snapping is on (a tie goes up).</summary>
    private static double SnapToTick(Slider slider, double value)
    {
        if (!slider.IsSnapToTickEnabled)
        {
            return value;
        }

        var previous = slider.Minimum;
        var next = slider.Maximum;
        if (slider.Ticks is { Count: > 0 } ticks)
        {
            foreach (var tick in ticks)
            {
                if (tick == value)
                {
                    return value;
                }

                if (tick < value && tick > previous)
                {
                    previous = tick;
                }
                else if (tick > value && tick < next)
                {
                    next = tick;
                }
            }
        }
        else if (slider.TickFrequency > 0)
        {
            previous = slider.Minimum + (Math.Round((value - slider.Minimum) / slider.TickFrequency) * slider.TickFrequency);
            next = Math.Min(slider.Maximum, previous + slider.TickFrequency);
        }

        return value >= (previous + next) * 0.5 ? next : previous;
    }

    /// <summary>A held track press: the first step, then repeats after the delay while the pointer is over the button.</summary>
    private sealed class Press
    {
        private readonly Slider _slider;
        private readonly Button _button;
        private readonly IPointer _pointer;
        private readonly bool _increase;
        private readonly DispatcherTimer _timer;
        private Point _position; // relative to the slider: the button moves as the thumb does

        public Press(Slider slider, Button button, IPointer pointer, Point position)
        {
            _slider = slider;
            _button = button;
            _pointer = pointer;
            _position = position;
            _increase = button.Name == "PART_IncreaseButton";
            Step(slider, _increase);
            _timer = new DispatcherTimer(RepeatDelay, DispatcherPriority.Input, OnTick);
            _timer.Start();
        }

        public void Moved(PointerEventArgs e)
        {
            if (e.Pointer == _pointer)
            {
                _position = e.GetPosition(_slider);
            }
        }

        public void End(IPointer pointer)
        {
            if (pointer == _pointer)
            {
                Stop();
                if (pointer.Captured == _button)
                {
                    pointer.Capture(null);
                }
            }
        }

        // The capture moves between the button and the pressed element inside it (Avalonia's implicit capture); only
        // losing it to something else ends the press.
        public void CaptureLost(IPointer pointer)
        {
            if (pointer == _pointer && !(pointer.Captured is Visual v && (v == _button || _button.IsVisualAncestorOf(v))))
            {
                Stop();
            }
        }

        public void Stop()
        {
            _timer.Stop();
            if (s_press == this)
            {
                s_press = null;
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _timer.Interval = RepeatInterval;

            // Step only while the pointer is over the button, which shrinks as the thumb approaches the pointer.
            if (_slider.TranslatePoint(_position, _button) is { } p && new Rect(_button.Bounds.Size).Contains(p) && _slider.IsEffectivelyEnabled)
            {
                Step(_slider, _increase);
            }
        }
    }
}
