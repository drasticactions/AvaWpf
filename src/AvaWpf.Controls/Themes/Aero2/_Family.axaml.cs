using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Controls.Themes.Aero2;

/// <summary>The AvaWpf.Controls ControlThemes of the Aero2 family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
