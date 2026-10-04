using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Controls.Themes.Fluent;

/// <summary>The AvaWpf.Controls ControlThemes of the Fluent family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
