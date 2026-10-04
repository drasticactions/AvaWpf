using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Themes.Luna;

/// <summary>The ControlThemes of the Luna family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
