using Avalonia;
using Avalonia.Controls;
using AvaWpf.Animations;

namespace AvaWpf.Gallery.Pages;

/// <summary>The theme motion: chrome fades, indeterminate progress and the window open/close motion.</summary>
public class MotionPage : UserControl
{
    public MotionPage()
    {
        var panel = new StackPanel { Spacing = 10, Width = 420, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        panel.Children.Add(new TextBlock { Classes = { "Subtitle" }, Text = "Theme motion" });
        panel.Children.Add(new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap, Text = "Hover the buttons: Aero fades its hover over 0.3 s and pulses the default button. The indeterminate bars run the family's storyboard or WPF's glow." });
        panel.Children.Add(new WrapPanel { ItemSpacing = 8, Children = { new Button { Content = "Hover me" }, new Button { Content = "Default", IsDefault = true }, new CheckBox { Content = "Check" } } });
        panel.Children.Add(new ProgressBar { IsIndeterminate = true, Height = 18 });
        var preview = new Border { Height = 80, [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("WpfAccentBrush") };
        var motions = new ComboBox { ItemsSource = System.Enum.GetValues<WindowMotion>(), SelectedIndex = 2 };
        var open = new Button { Content = "Play open" };
        var close = new Button { Content = "Play close" };
        open.Click += async (_, _) => { WindowAnimations.Reset(preview); await WindowAnimations.PlayOpen(preview, (WindowMotion)motions.SelectedItem!); };
        close.Click += async (_, _) => { await WindowAnimations.PlayClose(preview, (WindowMotion)motions.SelectedItem!); };
        panel.Children.Add(new TextBlock { Classes = { "Subtitle" }, Text = "Window motion" });
        panel.Children.Add(new WrapPanel { ItemSpacing = 8, Children = { motions, open, close } });
        panel.Children.Add(preview);
        Content = panel;
    }
}
