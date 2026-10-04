using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.DataGrid.Themes.Classic;

/// <summary>The DataGrid ControlThemes of the Classic family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
