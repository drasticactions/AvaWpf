using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Controls.Themes.Royale;

/// <summary>The AvaWpf.Controls ControlThemes of the Royale family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
