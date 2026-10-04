using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Ribbon.Themes.Luna;

/// <summary>The Ribbon ControlThemes of the Luna family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
