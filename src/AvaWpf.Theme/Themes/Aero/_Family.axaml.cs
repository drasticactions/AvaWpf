using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Themes.Aero;

/// <summary>The ControlThemes of the Aero family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
