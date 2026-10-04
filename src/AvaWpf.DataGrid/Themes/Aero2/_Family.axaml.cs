using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.DataGrid.Themes.Aero2;

/// <summary>The DataGrid ControlThemes of the Aero2 family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
