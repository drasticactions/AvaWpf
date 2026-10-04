using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaWpf.Gallery.Pages;

public partial class TextPage : UserControl
{
    public TextPage()
    {
        InitializeComponent();
        this.FindControl<AutoCompleteBox>("Auto")!.ItemsSource = new[] { "Aero", "Aero2", "AeroLite", "Classic", "Fluent", "Luna", "Royale" };
    }
}
