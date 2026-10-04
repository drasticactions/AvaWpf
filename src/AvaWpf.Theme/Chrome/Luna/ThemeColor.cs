namespace AvaWpf.Chrome.Luna;

/// <summary>
/// The Luna color scheme a Luna chrome draws, as WPF's <c>ThemeColor</c>. Templates bind it to the
/// <c>Luna.ThemeColor</c> resource, so a scheme switch needs no re-template.
/// </summary>
public enum ThemeColor
{
    /// <summary>Luna Blue.</summary>
    NormalColor,

    /// <summary>Luna Olive Green.</summary>
    Homestead,

    /// <summary>Luna Silver.</summary>
    Metallic,
}
