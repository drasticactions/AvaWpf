using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaWpf.Themes;

/// <summary>The global styles of every family (TextBlock type classes).</summary>
internal partial class GlobalStyles : Styles
{
    public GlobalStyles()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
