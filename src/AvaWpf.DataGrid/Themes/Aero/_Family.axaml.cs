using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.DataGrid.Themes.Aero;

/// <summary>The DataGrid ControlThemes of the Aero family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
