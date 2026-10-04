using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace AvaWpf;

/// <summary>
/// Opens a button's flyout below the button, left-aligned, as WPF's <c>Placement="Bottom"</c> instead of Avalonia's
/// centred default. Set at style priority so an app's placement wins.
/// </summary>
public static class ButtonFlyoutState
{
    private static bool s_registered;

    internal static void EnsureRegistered()
    {
        if (s_registered)
        {
            return;
        }

        s_registered = true;
        Button.FlyoutProperty.Changed.AddClassHandler<Button>((_, e) => Apply(e.NewValue as FlyoutBase));
        SplitButton.FlyoutProperty.Changed.AddClassHandler<SplitButton>((_, e) => Apply(e.NewValue as FlyoutBase));
    }

    private static void Apply(FlyoutBase? flyout)
    {
        if (flyout is PopupFlyoutBase popup && !popup.IsSet(PopupFlyoutBase.PlacementProperty))
        {
            popup.SetValue(PopupFlyoutBase.PlacementProperty, PlacementMode.BottomEdgeAlignedLeft, BindingPriority.Style);
        }
    }
}
