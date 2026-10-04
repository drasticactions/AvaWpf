using Avalonia.Media;
using AvaWpf.Samples;

namespace AvaWpf.Explorer.ViewModels;

/// <summary>The sample icons (vector drawings, shared: one per icon).</summary>
public static class Icons
{
    /// <summary>Returns the icon <paramref name="name"/>, shown at <paramref name="size"/> (16 or 32) pixels.</summary>
    public static IImage Get(string name, int size = 16) => SampleImages.Load(name, size);
}
