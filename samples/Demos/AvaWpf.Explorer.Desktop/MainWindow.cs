using AvaWpf.Explorer.ViewModels;

namespace AvaWpf.Explorer.Desktop;

/// <summary>The Explorer window: a ThemeWindow titled with the current folder, as Explorer does ("My Documents").</summary>
public class MainWindow : ThemeWindow
{
    public MainWindow()
    {
        var view = new MainView();
        Title = view.ViewModel.Title;
        FrameIcon = Icons.Get("folder-open");
        Width = LaunchOptions.Current.Width;
        Height = LaunchOptions.Current.Height;
        Content = view;
        view.ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ExplorerViewModel.Title))
            {
                Title = view.ViewModel.Title;
            }
        };
    }
}
