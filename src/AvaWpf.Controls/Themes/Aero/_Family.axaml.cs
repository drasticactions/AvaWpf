using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Controls.Themes.Aero;

/// <summary>The AvaWpf.Controls ControlThemes of the Aero family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
