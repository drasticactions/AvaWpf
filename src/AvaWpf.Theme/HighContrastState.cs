using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaWpf;

/// <summary>
/// Sets the <c>:highcontrast</c> pseudo-class on a tracked control while the <c>SystemParameters.HighContrast</c>
/// resource is true, because an Avalonia selector cannot read a resource.
/// </summary>
public static class HighContrastState
{
    /// <summary>The pseudo-class set while high contrast is on.</summary>
    public const string PseudoClass = ":highcontrast";

    /// <summary>The resource that holds the high contrast flag.</summary>
    public const string ResourceKey = "SystemParameters.HighContrast";

    /// <summary>Defines the <c>IsTracked</c> attached property.</summary>
    public static readonly AttachedProperty<bool> IsTrackedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsTracked", typeof(HighContrastState));

    private static readonly AttachedProperty<IDisposable?> SubscriptionProperty =
        AvaloniaProperty.RegisterAttached<Control, IDisposable?>("Subscription", typeof(HighContrastState));

    static HighContrastState()
    {
        IsTrackedProperty.Changed.AddClassHandler<Control>((c, e) => OnIsTrackedChanged(c, e.GetNewValue<bool>()));
    }

    /// <summary>Gets whether the control tracks the high contrast flag.</summary>
    public static bool GetIsTracked(Control control) => control.GetValue(IsTrackedProperty);

    /// <summary>Sets whether the control tracks the high contrast flag.</summary>
    public static void SetIsTracked(Control control, bool value) => control.SetValue(IsTrackedProperty, value);

    private static void OnIsTrackedChanged(Control control, bool tracked)
    {
        control.GetValue(SubscriptionProperty)?.Dispose();
        control.SetValue(SubscriptionProperty, null);
        if (!tracked)
        {
            ((IPseudoClasses)control.Classes).Set(PseudoClass, false);
            return;
        }

        var subscription = control.GetResourceObservable(ResourceKey).Subscribe(new Observer(control));
        control.SetValue(SubscriptionProperty, subscription);
    }

    private sealed class Observer(Control control) : IObserver<object?>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(object? value) => ((IPseudoClasses)control.Classes).Set(PseudoClass, value is true);
    }
}
