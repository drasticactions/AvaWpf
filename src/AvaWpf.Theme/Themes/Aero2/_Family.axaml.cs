using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Themes.Aero2;

/// <summary>The ControlThemes of the Aero2 family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
