using Avalonia.Media;

namespace AvaWpf;

/// <summary>The accent color and the six shades WPF Fluent and Avalonia derive from it.</summary>
/// <param name="Accent">The accent.</param>
/// <param name="Light1">One step lighter.</param>
/// <param name="Light2">Two steps lighter.</param>
/// <param name="Light3">Three steps lighter.</param>
/// <param name="Dark1">One step darker.</param>
/// <param name="Dark2">Two steps darker.</param>
/// <param name="Dark3">Three steps darker.</param>
public readonly record struct AccentRamp(Color Accent, Color Light1, Color Light2, Color Light3, Color Dark1, Color Dark2, Color Dark3);
