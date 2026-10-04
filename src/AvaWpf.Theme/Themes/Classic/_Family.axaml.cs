using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Themes.Classic;

/// <summary>The ControlThemes of the Classic family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
