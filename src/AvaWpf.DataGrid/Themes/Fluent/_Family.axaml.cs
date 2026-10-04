using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.DataGrid.Themes.Fluent;

/// <summary>The DataGrid ControlThemes of the Fluent family.</summary>
internal partial class FamilyControls : ResourceDictionary
{
    public FamilyControls()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
