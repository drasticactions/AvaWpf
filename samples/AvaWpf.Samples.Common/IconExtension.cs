using System;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Samples;

/// <summary>XAML access to the sample icons: <c>{samples:Icon folder}</c>.</summary>
public sealed class IconExtension : MarkupExtension
{
    /// <summary>Initializes a new instance of the <see cref="IconExtension"/> class.</summary>
    /// <param name="name">The icon name (<see cref="SampleImages.Names"/>).</param>
    public IconExtension(string name) => Name = name;

    /// <summary>The icon name (<see cref="SampleImages.Names"/>).</summary>
    public string Name { get; set; }

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider) => SampleImages.Load(Name);
}
