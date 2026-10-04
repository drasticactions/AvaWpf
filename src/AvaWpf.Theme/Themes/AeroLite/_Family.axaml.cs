using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Themes.AeroLite;

/// <summary>The ControlThemes of the AeroLite family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
