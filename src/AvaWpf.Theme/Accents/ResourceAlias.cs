using Avalonia.Styling;

namespace AvaWpf;

/// <summary>
/// A resource entry that resolves to another key at lookup time, so it follows the family, scheme and variant in
/// effect where it is used.
/// </summary>
/// <remarks>Only the theme resources resolve aliases; in an app dictionary the alias object itself is returned.</remarks>
public sealed class ResourceAlias
{
    /// <summary>Initializes an empty alias.</summary>
    public ResourceAlias()
    {
    }

    /// <summary>Initializes an alias to <paramref name="target"/>.</summary>
    public ResourceAlias(string target)
    {
        Target = target;
    }

    /// <summary>The key this alias resolves to.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Optional: the key used instead of <see cref="Target"/> under a Dark variant.</summary>
    public string? DarkTarget { get; set; }

    internal string TargetFor(ThemeVariant? theme) =>
        DarkTarget is not null && WpfThemeVariants.IsDark(theme) ? DarkTarget : Target;
}
