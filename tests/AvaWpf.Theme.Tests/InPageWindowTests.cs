using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>WindowHost and in-page windows: the dialogs of a single-view app (the browser).</summary>
public sealed class InPageWindowTests : IDisposable
{
    public InPageWindowTests() => WindowHost.UseInPageWindows = true;

    public void Dispose() => WindowHost.UseInPageWindows = null;

    private sealed class Page
    {
        public Page()
        {
            Button = new Button { Content = "Page button", Width = 120, Height = 30, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
            Button.Click += (_, _) => Clicks++;
            Window = new Window { Width = 900, Height = 700, Content = new ThemeScope { Theme = ThemeFamily.Aero, Child = Button } };
            Window.Show();
            Window.UpdateLayout();
        }

        public Window Window { get; }

        public Button Button { get; }

        public int Clicks { get; set; }

        public void Pump()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }

        public InPageWindowLayer Layer => InPageWindowLayer.GetOrCreate(Button)!;
    }

    private sealed class Dialog : UserControl
    {
        public Dialog(string title, object? okResult = null)
        {
            WindowSettings.SetTitle(this, title);
            WindowSettings.SetSizeToContent(this, SizeToContent.WidthAndHeight);
            Ok = new Button { Content = "OK", IsDefault = true };
            Cancel = new Button { Content = "Cancel", IsCancel = true };
            Box = new TextBox { Width = 160 };
            Ok.Click += (_, _) => WindowHost.CloseDialog(this, okResult ?? "ok");
            Cancel.Click += (_, _) => WindowHost.CloseDialog(this, "cancel");
            Content = new StackPanel { Margin = new Thickness(12), Spacing = 8, Children = { Box, Ok, Cancel } };
        }

        public Button Ok { get; }

        public Button Cancel { get; }

        public TextBox Box { get; }
    }

    [AvaloniaFact]
    public void Enter_Clicks_The_Default_Button_And_Completes_The_Dialog()
    {
        var page = new Page();
        var dialog = new Dialog("Options");
        Task<string?> result = WindowHost.ShowDialogAsync<string>(dialog, page.Button);
        page.Pump();

        Assert.True(dialog.Box.IsFocused);
        page.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        page.Pump();

        Assert.True(result.IsCompleted);
        Assert.Equal("ok", result.Result);
        Assert.Empty(page.Layer.Windows);
    }

    [AvaloniaFact]
    public void Escape_Clicks_The_Cancel_Button()
    {
        var page = new Page();
        var dialog = new Dialog("Options");
        Task<string?> result = WindowHost.ShowDialogAsync<string>(dialog, page.Button);
        page.Pump();

        page.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        page.Pump();

        Assert.Equal("cancel", result.Result);
    }

    [AvaloniaFact]
    public void Tab_Moves_Through_The_Dialog_And_Wraps_Inside_It()
    {
        var page = new Page();
        var dialog = new Dialog("Options");
        _ = WindowHost.ShowDialogAsync<string>(dialog, page.Button);
        page.Pump();
        Assert.True(dialog.Box.IsFocused);

        page.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        page.Pump();
        Assert.True(dialog.Ok.IsFocused);

        page.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        page.Pump();
        Assert.True(dialog.Cancel.IsFocused);

        page.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        page.Pump();
        Assert.True(dialog.Box.IsFocused);

        page.Window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Shift);
        page.Pump();
        Assert.True(dialog.Cancel.IsFocused);
    }

    [AvaloniaFact]
    public void A_Popup_In_The_Dialog_Takes_Its_Own_Enter_And_Escape()
    {
        var page = new Page();
        var dialog = new Dialog("Options");
        var field = new TextBox { Width = 100 };
        int enters = 0;
        Popup? popup = null;
        field.KeyDown += (_, e) =>
        {
            // As a drop-down does: Enter picks, Esc closes.
            if (e.Key == Key.Enter)
            {
                enters++;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popup!.IsOpen = false;
                e.Handled = true;
            }
        };

        // Overlay popups are the browser's: in the top level, outside the window's visual tree.
        popup = new Popup { ShouldUseOverlayLayer = true, PlacementTarget = dialog.Box, Child = new Border { Child = field } };
        ((StackPanel)dialog.Content!).Children.Add(popup);
        Task<string?> result = WindowHost.ShowDialogAsync<string>(dialog, page.Button);
        page.Pump();
        popup.IsOpen = true;
        page.Pump();
        field.Focus();
        page.Pump();

        page.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        page.Pump();
        Assert.Equal(1, enters);
        Assert.False(result.IsCompleted);

        page.Window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        page.Pump();
        Assert.False(popup.IsOpen);
        Assert.False(result.IsCompleted);
    }

    [AvaloniaFact]
    public void Enter_On_A_Focused_Button_Clicks_That_Button()
    {
        var page = new Page();
        var dialog = new Dialog("Options");
        Task<string?> result = WindowHost.ShowDialogAsync<string>(dialog, page.Button);
        page.Pump();
        dialog.Cancel.Focus(NavigationMethod.Tab);
        page.Pump();

        page.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        page.Pump();

        Assert.Equal("cancel", result.Result);
    }

    [AvaloniaFact]
    public void A_Modal_Window_Blocks_Clicks_On_The_Page_And_Comes_Up_Centered()
    {
        var page = new Page();
        var dialog = new Dialog("Options");
        _ = WindowHost.ShowDialogAsync(dialog, page.Button);
        page.Pump();

        var window = page.Layer.Windows.Single();
        Assert.True(window.IsModal);
        Assert.True(window.IsActive);
        var expected = new Point(Math.Round((page.Window.Bounds.Width - window.Bounds.Width) / 2), Math.Round((page.Window.Bounds.Height - window.Bounds.Height) / 2));
        Assert.Equal(expected, window.Bounds.Position);

        page.Window.MouseDown(new Point(20, 15), MouseButton.Left);
        page.Window.MouseUp(new Point(20, 15), MouseButton.Left);
        page.Pump();
        Assert.Equal(0, page.Clicks);
        Assert.Single(page.Layer.Windows);
    }

    [AvaloniaFact]
    public void Without_A_Modal_The_Page_Takes_Clicks()
    {
        var page = new Page();
        page.Window.MouseDown(new Point(20, 15), MouseButton.Left);
        page.Window.MouseUp(new Point(20, 15), MouseButton.Left);
        page.Pump();
        Assert.Equal(1, page.Clicks);

        _ = page.Layer;
        page.Window.MouseDown(new Point(20, 15), MouseButton.Left);
        page.Window.MouseUp(new Point(20, 15), MouseButton.Left);
        page.Pump();
        Assert.Equal(2, page.Clicks);
    }

    [AvaloniaFact]
    public void A_Nested_Modal_Takes_Enter_And_Gives_The_Keyboard_Back()
    {
        var page = new Page();
        var options = new Dialog("Options", "options-ok");
        Task<string?> outer = WindowHost.ShowDialogAsync<string>(options, page.Button);
        page.Pump();
        options.Box.Focus();
        var message = new Dialog("Message", "message-ok");
        Task<string?> inner = WindowHost.ShowDialogAsync<string>(message, options);
        page.Pump();

        Assert.False(page.Layer.Windows[0].IsActive);
        Assert.True(page.Layer.Windows[1].IsActive);
        page.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        page.Pump();

        Assert.Equal("message-ok", inner.Result);
        Assert.False(outer.IsCompleted);
        Assert.True(options.Box.IsFocused);
        Assert.True(page.Layer.Windows.Single().IsActive);
    }

    [AvaloniaFact]
    public void Closing_The_Last_Window_Gives_The_Focus_Back()
    {
        var page = new Page();
        page.Button.Focus();
        var dialog = new Dialog("Options");
        _ = WindowHost.ShowDialogAsync(dialog, page.Button);
        page.Pump();
        Assert.False(page.Button.IsFocused);

        WindowHost.CloseDialog(dialog);
        page.Pump();
        Assert.True(page.Button.IsFocused);
    }

    [AvaloniaFact]
    public void A_Closing_Handler_Can_Keep_The_Window_Open()
    {
        var page = new Page();
        var dialog = new Dialog("Publish");
        var keepOpen = true;
        var closed = 0;
        WindowHost.AddClosingHandler(dialog, (_, e) => e.Cancel = keepOpen);
        WindowHost.AddClosedHandler(dialog, (_, _) => closed++);
        Task result = WindowHost.ShowDialogAsync(dialog, page.Button);
        page.Pump();

        WindowHost.CloseDialog(dialog);
        page.Pump();
        Assert.False(result.IsCompleted);

        keepOpen = false;
        WindowHost.CloseDialog(dialog);
        page.Pump();
        Assert.True(result.IsCompleted);
        Assert.Equal(1, closed);
    }

    [AvaloniaFact]
    public void Opened_Is_Raised_On_The_Content()
    {
        var page = new Page();
        var dialog = new Dialog("Options");
        var opened = 0;
        WindowHost.AddOpenedHandler(dialog, (_, _) => opened++);
        _ = WindowHost.ShowDialogAsync(dialog, page.Button);
        page.Pump();
        Assert.Equal(1, opened);
    }

    [AvaloniaFact]
    public void The_Caption_Drags_The_Window_Within_The_Page()
    {
        var page = new Page();
        var dialog = new Dialog("Options");
        _ = WindowHost.ShowDialogAsync(dialog, page.Button);
        page.Pump();
        var window = page.Layer.Windows.Single();
        var caption = window.GetVisualDescendants().OfType<Control>().First(c => c.Name == WindowFrame.CaptionPartName);
        var start = caption.TranslatePoint(new Point(caption.Bounds.Width / 2, caption.Bounds.Height / 2), page.Window)!.Value;
        var before = window.Position;

        page.Window.MouseDown(start, MouseButton.Left);
        page.Window.MouseMove(start + new Point(-60, 40));
        page.Window.MouseUp(start + new Point(-60, 40), MouseButton.Left);
        page.Pump();

        Assert.Equal(before + new Point(-60, 40), window.Position);

        page.Window.MouseDown(start + new Point(-60, 40), MouseButton.Left);
        page.Window.MouseMove(new Point(-2000, -2000));
        page.Window.MouseUp(new Point(-2000, -2000), MouseButton.Left);
        page.Pump();
        Assert.Equal(0, window.Position.Y);
        Assert.True(window.Position.X + window.Bounds.Width > 0);
    }

    [AvaloniaFact]
    public void Settings_Size_And_Name_The_Window()
    {
        var page = new Page();
        var content = new Border();
        WindowSettings.SetTitle(content, "Audio Levels");
        WindowSettings.SetWidth(content, 300);
        WindowSettings.SetHeight(content, 200);
        WindowSettings.SetFrameKind(content, WindowFrameKind.Tool);
        IWindowHandle handle = WindowHost.Show(content, page.Button);
        page.Pump();

        var window = page.Layer.Windows.Single();
        Assert.False(window.IsModal);
        Assert.Equal(new Size(300, 200), window.Bounds.Size);
        Assert.Equal("Audio Levels", Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(window).GetName());
        Assert.Equal(Avalonia.Automation.Peers.AutomationControlType.Window, Avalonia.Automation.Peers.ControlAutomationPeer.CreatePeerForElement(window).GetAutomationControlType());

        handle.Close();
        page.Pump();
        Assert.False(handle.IsOpen);
        Assert.Empty(page.Layer.Windows);
    }

    [AvaloniaFact]
    public void Alt_Key_Reaches_The_Dialog_Not_The_Page()
    {
        var page = new Page();
        var pageClicks = 0;
        var pageAccess = new Button { Content = "_Reset" };
        pageAccess.Click += (_, _) => pageClicks++;
        var scope = (ThemeScope)page.Window.Content!;
        scope.Child = null;
        scope.Child = new StackPanel { Children = { page.Button, pageAccess } };
        page.Pump();

        var dialogClicks = 0;
        var dialog = new Dialog("Volume");
        var reset = new Button { Content = "_Reset" };
        reset.Click += (_, _) => dialogClicks++;
        ((StackPanel)dialog.Content!).Children.Add(reset);
        _ = WindowHost.ShowDialogAsync(dialog, page.Button);
        page.Pump();

        page.Window.KeyPressQwerty(PhysicalKey.R, RawInputModifiers.Alt);
        page.Pump();
        Assert.Equal(1, dialogClicks);
        Assert.Equal(0, pageClicks);
    }

    [AvaloniaFact]
    public void Desktop_Hosting_Uses_A_ThemeWindow()
    {
        WindowHost.UseInPageWindows = false;
        var owner = new Window { Width = 600, Height = 400 };
        owner.Show();
        var dialog = new Dialog("Options");
        WindowSettings.SetSizeToContent(dialog, SizeToContent.Height);
        WindowSettings.SetWidth(dialog, 320);
        Task<string?> result = WindowHost.ShowDialogAsync<string>(dialog, owner);
        Dispatcher.UIThread.RunJobs();

        var host = Assert.IsType<ThemeWindow>(WindowHost.HostOf(dialog));
        Assert.Equal("Options", host.Title);
        Assert.Equal(320, host.Width);
        WindowHost.CloseDialog(dialog, "done");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("done", result.Result);
    }
}
