using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Ribbon.Themes.Classic;

/// <summary>The Ribbon ControlThemes of the Classic family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
