using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Ribbon.Themes.AeroLite;

/// <summary>The Ribbon ControlThemes of the AeroLite family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
