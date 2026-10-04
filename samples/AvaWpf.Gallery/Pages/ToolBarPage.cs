using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using AvaWpf.Controls;
using AvaWpf.Samples;

namespace AvaWpf.Gallery.Pages;

/// <summary>ToolBarTray and ToolBar: bands, overflow, the locked tray and a vertical tray.</summary>
public class ToolBarPage : UserControl
{
    public ToolBarPage()
    {
        var tray = new ToolBarTray
        {
            ToolBars =
            {
                Standard(0, 0),
                Formatting(0, 1),
                FindBar(1, 0),
            },
        };

        var locked = new CheckBox { Content = "_Locked (grippers hidden, no dragging)" };
        locked.IsCheckedChanged += (_, _) => tray.IsLocked = locked.IsChecked == true;

        var narrow = new ToolBarTray
        {
            Width = 260,
            HorizontalAlignment = HorizontalAlignment.Left,
            ToolBars = { Standard(0, 0) },
        };

        var vertical = new ToolBarTray
        {
            Orientation = Orientation.Vertical,
            Height = 230,
            HorizontalAlignment = HorizontalAlignment.Left,
            ToolBars = { Standard(0, 0), Formatting(1, 0) },
        };

        Content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Classes = { "Subtitle" }, Text = "Tray with two bands (drag a gripper to move a toolbar)" },
                tray,
                locked,
                new TextBlock { Classes = { "Subtitle" }, Text = "Overflow (the items that do not fit go behind the arrow)" },
                narrow,
                new TextBlock { Classes = { "Subtitle" }, Text = "Vertical tray" },
                vertical,
            },
        };
    }

    private static ToolBar Standard(int band, int index) => new()
    {
        Band = band,
        BandIndex = index,
        ItemsSource = new Control[]
        {
            IconButton("file", "New"),
            IconButton("folder-open", "Open"),
            IconButton("text", "Save"),
            new Separator(),
            IconButton("cut", "Cut"),
            IconButton("copy", "Copy"),
            IconButton("paste", "Paste"),
            new Separator(),
            IconButton("undo", "Undo"),
            IconButton("redo", "Redo"),
            Overflow(IconButton("find", "Find"), OverflowMode.Always),
        },
    };

    private static ToolBar Formatting(int band, int index)
    {
        var group = $"align{band}.{index}";
        return new ToolBar
        {
            Band = band,
            BandIndex = index,
            ItemsSource = new Control[]
            {
                new ComboBox { Width = 110, SelectedIndex = 0, ItemsSource = new[] { "Tahoma", "Verdana", "Courier New" } },
                new Separator(),
                IconToggle("bold", "Bold", true),
                IconToggle("italic", "Italic", false),
                IconToggle("underline", "Underline", false),
                new Separator(),
                IconRadio("align-left", "Align left", group, true),
                IconRadio("align-center", "Center", group, false),
                IconRadio("align-right", "Align right", group, false),
            },
        };
    }

    private static ToolBar FindBar(int band, int index) => new()
    {
        Band = band,
        BandIndex = index,
        Header = "Find:",
        ItemsSource = new Control[]
        {
            new TextBox { Width = 140, Text = "toolbar" },
            new Button { Content = "Next" },
            new CheckBox { Content = "Match case" },
        },
    };

    private static Control Overflow(Control item, OverflowMode mode)
    {
        ToolBar.SetOverflowMode(item, mode);
        return item;
    }

    private static Image Icon(string name) => new() { Source = SampleImages.Load(name), Width = 16, Height = 16 };

    private static Button IconButton(string icon, string tip)
    {
        var b = new Button { Content = Icon(icon) };
        ToolTip.SetTip(b, tip);
        return b;
    }

    private static ToggleButton IconToggle(string icon, string tip, bool isChecked)
    {
        var b = new ToggleButton { Content = Icon(icon), IsChecked = isChecked };
        ToolTip.SetTip(b, tip);
        return b;
    }

    private static RadioButton IconRadio(string icon, string tip, string group, bool isChecked)
    {
        var b = new RadioButton { Content = Icon(icon), IsChecked = isChecked, GroupName = group };
        ToolTip.SetTip(b, tip);
        return b;
    }
}
