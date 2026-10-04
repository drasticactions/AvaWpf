namespace AvaWpf.Gallery.Desktop;

/// <summary>The gallery window: a ThemeWindow, so the frame follows the theme switchers.</summary>
public class MainWindow : ThemeWindow
{
    public MainWindow()
    {
        Title = "AvaWpf Gallery";
        Width = LaunchOptions.Current.Width;
        Height = LaunchOptions.Current.Height;
        Content = new MainView();
    }
}
