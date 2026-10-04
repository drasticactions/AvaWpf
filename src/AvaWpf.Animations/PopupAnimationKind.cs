namespace AvaWpf.Animations;

/// <summary>How a popup appears, as WPF's <c>System.Windows.Controls.Primitives.PopupAnimation</c>.</summary>
public enum PopupAnimationKind
{
    /// <summary>The popup appears at once.</summary>
    None,

    /// <summary>The popup fades in (opacity 0 → 1).</summary>
    Fade,

    /// <summary>The content slides in vertically from the edge next to the placement target.</summary>
    Slide,

    /// <summary>The content slides in diagonally, from the edge next to the target and from the start side.</summary>
    Scroll,
}
