using AvaWpf.Ribbon;

namespace AvaWpf.WordPad.Desktop;

/// <summary>
/// The WordPad window: a RibbonWindow, so the Quick Access Toolbar and the Picture Tools header sit in the caption of
/// the family's frame. The title follows the document name ("Document - WordPad").
/// </summary>
public class MainWindow : RibbonWindow
{
    public MainWindow()
    {
        var view = new MainView();
        Title = view.Title;
        FrameIcon = Icons.Get("wordpad");
        Width = LaunchOptions.Current.Width;
        Height = LaunchOptions.Current.Height;
        Content = view;
        view.TitleChanged += (_, _) => Title = view.Title;
        if (LaunchOptions.Current.Picture)
        {
            view.InsertSamplePicture();
        }
    }
}
