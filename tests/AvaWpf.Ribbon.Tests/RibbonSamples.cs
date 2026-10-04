using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaWpf.Ribbon.Primitives;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Sample Ribbons for the behavior tests.</summary>
internal static class RibbonSamples
{
    /// <summary>A 32 × 32 (or 16 × 16) vector image, so buttons take their large variant.</summary>
    public static IImage Image(double size = 32) => new DrawingImage(new GeometryDrawing
    {
        Geometry = new RectangleGeometry(new Avalonia.Rect(0, 0, size, size)),
        Brush = Brushes.SteelBlue,
    });

    public static RibbonButton Button(string label, string? keyTip = null) => new()
    {
        Label = label,
        LargeImageSource = Image(),
        SmallImageSource = Image(16),
        KeyTip = keyTip,
        QuickAccessToolBarId = label,
    };

    public static RibbonGroup Group(string name, int buttons)
    {
        var group = new RibbonGroup { Name = name, Header = name, LargeImageSource = Image() };
        for (var i = 0; i < buttons; i++)
        {
            group.Items.Add(Button($"{name} {i + 1}"));
        }

        return group;
    }

    /// <summary>A Ribbon with a Home tab of three groups (Clipboard 3, Font 6, Paragraph 4 large buttons) and an Insert tab.</summary>
    public static Ribbon ThreeGroupRibbon(string? reductionOrder = null)
    {
        var home = new RibbonTab { Header = "Home", KeyTip = "H" };
        if (reductionOrder is not null)
        {
            home.GroupSizeReductionOrder = StringCollection.Parse(reductionOrder);
        }

        home.Items.Add(Group("Clipboard", 3));
        home.Items.Add(Group("Font", 6));
        home.Items.Add(Group("Paragraph", 4));
        var insert = new RibbonTab { Header = "Insert", KeyTip = "N" };
        insert.Items.Add(Group("Tables", 2));
        var ribbon = new Ribbon { QuickAccessToolBar = new RibbonQuickAccessToolBar() };
        ribbon.Items.Add(home);
        ribbon.Items.Add(insert);
        return ribbon;
    }

    public static Window Show(Control content, double width = 1400, double height = 400)
    {
        // A Ribbon sits at the top of the window, at its own height (as in a DockPanel).
        var window = new Window { Content = content is Ribbon ? new StackPanel { Children = { content } } : content, Width = width, Height = height };
        window.Show();
        Settle(window);
        return window;
    }

    /// <summary>Runs layout and the dispatcher until both are idle.</summary>
    public static void Settle(Window window)
    {
        for (var i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    public static RibbonGroupsPanel GroupsPanel(RibbonTab tab) => tab.GetVisualDescendants().OfType<RibbonGroupsPanel>().First();
}
