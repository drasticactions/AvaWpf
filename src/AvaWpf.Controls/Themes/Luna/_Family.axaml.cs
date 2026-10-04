using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Controls.Themes.Luna;

/// <summary>The AvaWpf.Controls ControlThemes of the Luna family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
