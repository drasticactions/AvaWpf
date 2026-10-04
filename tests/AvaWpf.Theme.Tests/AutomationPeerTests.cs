using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaWpf.Automation.Peers;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>Automation peers of the window frame and ThemeWindow.</summary>
public class AutomationPeerTests
{
    private static AutomationPeer Peer(Control control) => ControlAutomationPeer.CreatePeerForElement(control);

    private static IEnumerable<AutomationPeer> Descendants(AutomationPeer peer)
    {
        foreach (var child in peer.GetChildren())
        {
            yield return child;
            foreach (var d in Descendants(child))
            {
                yield return d;
            }
        }
    }

    private static Window Show(Control content)
    {
        var window = new Window { Content = content, Width = 500, Height = 300 };
        window.Show();
        window.UpdateLayout();
        return window;
    }

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2)]
    [InlineData(ThemeFamily.Aero)]
    [InlineData(ThemeFamily.Luna)]
    [InlineData(ThemeFamily.Classic)]
    [InlineData(ThemeFamily.Fluent)]
    public void WindowFrame_Has_A_TitleBar_With_Named_Caption_Buttons(ThemeFamily family)
    {
        var content = new TextBlock { Text = "Client area" };
        var frame = new WindowFrame { Title = "Untitled - Notepad", Content = content };
        var window = Show(new ThemeScope { Theme = family, Child = frame });

        var peer = Peer(frame);
        var children = peer.GetChildren();
        var titleBar = children[0];

        Assert.IsType<WindowFrameAutomationPeer>(peer);
        Assert.Equal("Untitled - Notepad", peer.GetName());
        Assert.False(peer.IsControlElement());
        Assert.Equal(AutomationControlType.TitleBar, titleBar.GetAutomationControlType());
        Assert.Equal("Untitled - Notepad", titleBar.GetName());
        Assert.Equal(["Minimize", "Maximize", "Close"], titleBar.GetChildren().Select(c => c.GetName()));
        Assert.All(titleBar.GetChildren(), b => Assert.Equal(AutomationControlType.Button, b.GetAutomationControlType()));
        Assert.Same(Peer(content), children[^1]);

        frame.WindowState = WindowState.Maximized;
        Assert.Equal(["Minimize", "Restore", "Close"], titleBar.GetChildren().Select(c => c.GetName()));
        window.Close();
    }

    [AvaloniaFact]
    public void Caption_Buttons_Invoke_The_Frame()
    {
        var frame = new WindowFrame { Title = "Doc" };
        var window = Show(frame);
        var invoked = new List<CaptionButton>();
        frame.CaptionButtonInvoked += (_, e) => invoked.Add(e.Button);

        foreach (var button in Peer(frame).GetChildren()[0].GetChildren())
        {
            button.GetProvider<IInvokeProvider>()!.Invoke();
        }

        Assert.Equal([CaptionButton.Minimize, CaptionButton.Maximize, CaptionButton.Close], invoked);
        window.Close();
    }

    [AvaloniaFact]
    public void Dialog_TitleBar_Has_Only_Close()
    {
        var frame = new WindowFrame { Title = "Options", Kind = WindowFrameKind.Dialog };
        var window = Show(frame);

        var titleBar = Peer(frame).GetChildren()[0];

        Assert.Equal(["Close"], titleBar.GetChildren().Select(c => c.GetName()));
        window.Close();
    }

    [AvaloniaFact]
    public void ThemeWindow_Is_A_Window_Containing_The_Frame_TitleBar()
    {
        var content = new Button { Content = "OK" };
        var window = new ThemeWindow { Title = "Explorer", Content = content, Width = 500, Height = 300 };
        window.Show();
        window.UpdateLayout();

        var peer = Peer(window);
        var all = Descendants(peer).ToList();

        Assert.IsType<ThemeWindowAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.Window, peer.GetAutomationControlType());
        Assert.Equal("Explorer", peer.GetName());
        Assert.Equal("ThemeWindow", peer.GetClassName());
        Assert.Contains(all, p => p.GetAutomationControlType() == AutomationControlType.TitleBar && p.GetName() == "Explorer");
        Assert.Contains(Peer(content), all);
        window.Close();
    }
}
