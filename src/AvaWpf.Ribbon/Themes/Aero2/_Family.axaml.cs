using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Ribbon.Themes.Aero2;

/// <summary>The Ribbon ControlThemes of the Aero2 family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
