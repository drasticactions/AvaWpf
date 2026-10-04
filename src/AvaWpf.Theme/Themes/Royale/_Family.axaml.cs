using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Themes.Royale;

/// <summary>The ControlThemes of the Royale family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
