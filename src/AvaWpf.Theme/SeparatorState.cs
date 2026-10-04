using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaWpf;

/// <summary>
/// WPF's <c>MenuItem.SeparatorStyleKey</c>: sets the <c>:menu</c> pseudo-class on a Separator inside a menu, because
/// Avalonia control themes cannot select on the parent.
/// </summary>
public static class SeparatorState
{
    /// <summary>The pseudo-class set on a Separator item of a menu.</summary>
    public const string MenuPseudoClass = ":menu";

    private static bool s_registered;

    internal static void EnsureRegistered()
    {
        if (s_registered)
        {
            return;
        }

        s_registered = true;
        StyledElement.ParentProperty.Changed.AddClassHandler<Separator>((separator, _) =>
            ((IPseudoClasses)separator.Classes).Set(MenuPseudoClass, separator.Parent is MenuItem or MenuBase));
    }
}
