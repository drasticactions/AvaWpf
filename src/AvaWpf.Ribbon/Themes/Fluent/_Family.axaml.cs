using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Ribbon.Themes.Fluent;

/// <summary>The Ribbon ControlThemes of the Fluent family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
