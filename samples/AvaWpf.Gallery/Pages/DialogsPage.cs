using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaWpf.Gallery.Pages;

/// <summary>
/// Dialogs through WindowHost: desktop ThemeWindows here, in-page windows in the browser gallery (and with
/// "Force in-page windows" on the desktop).
/// </summary>
public class DialogsPage : UserControl
{
    private readonly TextBlock _result = new() { Text = "Result: (none)" };
    private IWindowHandle? _tool;

    public DialogsPage()
    {
        var force = new CheckBox { Content = "Force in-page windows", IsChecked = WindowHost.UseInPageWindows == true };
        force.IsCheckedChanged += (_, _) => WindowHost.UseInPageWindows = force.IsChecked == true ? true : null;

        var dialog = new Button { Content = "Show a _dialog..." };
        dialog.Click += async (_, _) =>
        {
            var result = await WindowHost.ShowDialogAsync<string>(Options(), this);
            _result.Text = "Result: " + (result ?? "(closed)");
        };

        var tool = new Button { Content = "Show a _tool window" };
        tool.Click += (_, _) =>
        {
            if (_tool is { IsOpen: true })
            {
                _tool.Activate();
                return;
            }

            var levels = new StackPanel { Margin = new Thickness(10), Spacing = 6, Children = { new TextBlock { Text = "A modeless tool window." }, new ProgressBar { Value = 60, Width = 160 } } };
            WindowSettings.SetTitle(levels, "Audio Levels");
            WindowSettings.SetFrameKind(levels, WindowFrameKind.Tool);
            WindowSettings.SetSizeToContent(levels, SizeToContent.WidthAndHeight);
            _tool = WindowHost.Show(levels, this);
        };

        Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children = { force, dialog, tool, _result },
        };
    }

    private static Control Options()
    {
        var content = new UserControl();
        WindowSettings.SetTitle(content, "Options");
        WindowSettings.SetSizeToContent(content, SizeToContent.WidthAndHeight);
        var name = new TextBox { Width = 200, Text = "Untitled" };
        var ok = new Button { Content = "OK", IsDefault = true, MinWidth = 75 };
        var cancel = new Button { Content = "Cancel", IsCancel = true, MinWidth = 75 };
        var more = new Button { Content = "_Message over this dialog..." };
        ok.Click += (_, _) => WindowHost.CloseDialog(content, name.Text);
        cancel.Click += (_, _) => WindowHost.CloseDialog(content, "Cancel");
        more.Click += async (_, _) =>
        {
            var message = new StackPanel { Margin = new Thickness(12), Spacing = 10 };
            var close = new Button { Content = "OK", IsDefault = true, IsCancel = true, MinWidth = 75, HorizontalAlignment = HorizontalAlignment.Right };
            message.Children.Add(new TextBlock { Text = "Enter and Esc close this one, not the dialog beneath." });
            message.Children.Add(close);
            WindowSettings.SetTitle(message, "Message");
            WindowSettings.SetSizeToContent(message, SizeToContent.WidthAndHeight);
            close.Click += (_, _) => WindowHost.CloseDialog(message);
            await WindowHost.ShowDialogAsync(message, content);
        };
        content.Content = new StackPanel
        {
            Margin = new Thickness(12),
            Spacing = 8,
            Children =
            {
                new Label { Content = "_Name:", Target = name },
                name,
                more,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { ok, cancel } },
            },
        };
        return content;
    }
}
