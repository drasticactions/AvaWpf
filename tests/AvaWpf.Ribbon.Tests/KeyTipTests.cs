using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>KeyTips: Alt or F10 shows them, nested sequences, prefixes, Esc steps back.</summary>
public class KeyTipTests
{
    private sealed class Sample
    {
        public Sample()
        {
            // KeyTip mode is global; start every test outside it.
            KeyTipService.DismissKeyTips();
            Paste = RibbonSamples.Button("Paste", "V");
            Paste.Command = new CountingCommand(() => PasteClicks++);
            FontA = RibbonSamples.Button("Font A", "FA");
            FontB = RibbonSamples.Button("Font B", "FB");
            FontB.Click += (_, _) => FontBClicks++;
            Menu = new RibbonMenuButton { Label = "Menu", KeyTip = "M", LargeImageSource = RibbonSamples.Image() };
            MenuItem = new RibbonMenuItem { Header = "Item", KeyTip = "I" };
            MenuItem.Click += (_, _) => MenuItemClicks++;
            Menu.Items.Add(MenuItem);

            var clipboard = new RibbonGroup { Name = "Clipboard", Header = "Clipboard" };
            clipboard.Items.Add(Paste);
            clipboard.Items.Add(FontA);
            clipboard.Items.Add(FontB);
            clipboard.Items.Add(Menu);
            Home = new RibbonTab { Header = "Home", KeyTip = "H" };
            Home.Items.Add(clipboard);
            Insert = new RibbonTab { Header = "Insert", KeyTip = "N" };
            Insert.Items.Add(RibbonSamples.Group("Tables", 2));
            Ribbon = new Ribbon { QuickAccessToolBar = new RibbonQuickAccessToolBar() };
            Ribbon.Items.Add(Home);
            Ribbon.Items.Add(Insert);
            Window = RibbonSamples.Show(Ribbon);
        }

        public Ribbon Ribbon { get; }

        public RibbonTab Home { get; }

        public RibbonTab Insert { get; }

        public RibbonButton Paste { get; }

        public RibbonButton FontA { get; }

        public RibbonButton FontB { get; }

        public RibbonMenuButton Menu { get; }

        public RibbonMenuItem MenuItem { get; }

        public Window Window { get; }

        public int PasteClicks { get; set; }

        public int FontBClicks { get; set; }

        public int MenuItemClicks { get; set; }

        public string[] ActiveKeyTips => KeyTipService.ActiveElements.Select(KeyTipService.GetKeyTip).OrderBy(k => k).ToArray()!;

        public void PressAlt()
        {
            Window.KeyPress(Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
            Window.KeyRelease(Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
            Settle();
        }

        public void Type(string letters)
        {
            foreach (var c in letters)
            {
                var physical = c switch
                {
                    >= 'A' and <= 'Z' => PhysicalKey.A + (c - 'A'),
                    >= '0' and <= '9' => PhysicalKey.Digit0 + (c - '0'),
                    _ => PhysicalKey.None,
                };
                Window.KeyPressQwerty(physical, RawInputModifiers.None);
                Window.KeyReleaseQwerty(physical, RawInputModifiers.None);
                Settle();
            }
        }

        public void Escape()
        {
            Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
            Settle();
        }

        public void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            RibbonSamples.Settle(Window);
        }
    }

    private sealed class CountingCommand(System.Action execute) : System.Windows.Input.ICommand
    {
        public event System.EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }

    [AvaloniaFact]
    public void Alt_Shows_The_Tab_KeyTips()
    {
        var s = new Sample();
        Assert.Equal(KeyTipState.None, KeyTipService.State);
        s.PressAlt();
        Assert.Equal(KeyTipState.Enabled, KeyTipService.State);
        Assert.Equal(["H", "N"], s.ActiveKeyTips);
        Assert.All(KeyTipService.ActiveElements, e => Assert.IsType<RibbonTabHeader>(e));

        // Pressing Alt again leaves KeyTip mode.
        s.PressAlt();
        Assert.Equal(KeyTipState.None, KeyTipService.State);
    }

    [AvaloniaFact]
    public void F10_Shows_The_KeyTips()
    {
        var s = new Sample();
        s.Window.KeyPress(Key.F10, RawInputModifiers.None, PhysicalKey.F10, null);
        s.Window.KeyRelease(Key.F10, RawInputModifiers.None, PhysicalKey.F10, null);
        s.Settle();
        Assert.Equal(KeyTipState.Enabled, KeyTipService.State);
        Assert.Equal(["H", "N"], s.ActiveKeyTips);
        KeyTipService.DismissKeyTips();
    }

    [AvaloniaFact]
    public void A_Tab_KeyTip_Selects_The_Tab_And_Shows_Its_KeyTips()
    {
        var s = new Sample();
        s.PressAlt();
        s.Type("H");
        Assert.Same(s.Home, KeyTipService.CurrentScope);
        Assert.Equal(["FA", "FB", "M", "V"], s.ActiveKeyTips);

        // Esc steps back to the tabs; a second Esc leaves KeyTip mode.
        s.Escape();
        Assert.Equal(KeyTipState.Enabled, KeyTipService.State);
        Assert.Equal(["H", "N"], s.ActiveKeyTips);
        s.Escape();
        Assert.Equal(KeyTipState.None, KeyTipService.State);
    }

    [AvaloniaFact]
    public void A_Nested_Sequence_Clicks_The_Button_And_Leaves_KeyTip_Mode()
    {
        var s = new Sample();
        s.PressAlt();
        s.Type("N");
        Assert.Equal(1, s.Ribbon.SelectedIndex);
        Assert.Same(s.Insert, KeyTipService.CurrentScope);
        s.Escape();
        s.Type("HV");
        Assert.Equal(0, s.Ribbon.SelectedIndex);
        Assert.Equal(1, s.PasteClicks);
        Assert.Equal(KeyTipState.None, KeyTipService.State);
    }

    [AvaloniaFact]
    public void A_Prefix_Narrows_The_KeyTips()
    {
        var s = new Sample();
        s.PressAlt();
        s.Type("HF");
        Assert.Equal(["FA", "FB"], s.ActiveKeyTips);
        s.Type("B");
        Assert.Equal(1, s.FontBClicks);
        Assert.Equal(KeyTipState.None, KeyTipService.State);
    }

    [AvaloniaFact]
    public void A_Menu_KeyTip_Opens_The_Menu_And_Esc_Closes_It()
    {
        var s = new Sample();
        s.PressAlt();
        s.Type("H");
        var before = string.Join(",", s.ActiveKeyTips) + " " + KeyTipService.State;
        s.Type("M");
        Assert.True(s.Menu.IsDropDownOpen, before + " / " + string.Join(",", s.ActiveKeyTips) + " " + KeyTipService.State);
        Assert.Same(s.Menu, KeyTipService.CurrentScope);
        Assert.Equal(["I"], s.ActiveKeyTips);

        s.Escape();
        Assert.False(s.Menu.IsDropDownOpen);
        Assert.Same(s.Home, KeyTipService.CurrentScope);

        s.Type("M");
        s.Type("I");
        Assert.Equal(1, s.MenuItemClicks);
        Assert.False(s.Menu.IsDropDownOpen);
        Assert.Equal(KeyTipState.None, KeyTipService.State);
    }

    [AvaloniaFact]
    public void A_Pointer_Press_Leaves_KeyTip_Mode()
    {
        var s = new Sample();
        s.PressAlt();
        Assert.Equal(KeyTipState.Enabled, KeyTipService.State);
        s.Window.MouseDown(new Avalonia.Point(5, 300), MouseButton.Left);
        s.Window.MouseUp(new Avalonia.Point(5, 300), MouseButton.Left);
        Assert.Equal(KeyTipState.None, KeyTipService.State);
    }

    [AvaloniaFact]
    public void Quick_Access_Toolbar_Items_Get_Numbered_KeyTips()
    {
        var s = new Sample();
        Assert.True(s.Ribbon.AddToQuickAccessToolBar(s.Paste));
        Assert.True(s.Ribbon.AddToQuickAccessToolBar(s.FontB));
        s.Settle();
        s.PressAlt();
        Assert.Equal(["1", "2", "H", "N"], s.ActiveKeyTips);
        s.Type("1");
        Assert.Equal(1, s.PasteClicks);
        Assert.Equal(KeyTipState.None, KeyTipService.State);
    }
}
