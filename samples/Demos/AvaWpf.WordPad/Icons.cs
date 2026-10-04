using System;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaWpf.Samples;

namespace AvaWpf.WordPad;

/// <summary>The sample icons (vector drawings, shared: one per icon).</summary>
public static class Icons
{
    /// <summary>Returns the icon <paramref name="name"/>, shown at <paramref name="size"/> (16 or 32) pixels.</summary>
    public static IImage Get(string name, int size = 16) => SampleImages.Load(name, size);
}

/// <summary>XAML access to the sample icons: <c>{local:Icon paste}</c> (16 px) or <c>{local:Icon paste, Size=32}</c>.</summary>
public sealed class IconExtension : MarkupExtension
{
    public IconExtension(string name) => Name = name;

    /// <summary>The icon name (AvaWpf.Samples.SampleImages).</summary>
    public string Name { get; set; }

    /// <summary>16 or 32.</summary>
    public int Size { get; set; } = 16;

    public override object ProvideValue(IServiceProvider serviceProvider) => Icons.Get(Name, Size);
}
