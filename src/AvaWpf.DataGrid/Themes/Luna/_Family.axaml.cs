using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.DataGrid.Themes.Luna;

/// <summary>The DataGrid ControlThemes of the Luna family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
