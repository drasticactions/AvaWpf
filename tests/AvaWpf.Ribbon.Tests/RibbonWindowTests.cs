using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaWpf.Ribbon.Primitives;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>RibbonWindow puts the Ribbon's Quick Access Toolbar and contextual tab group headers in the frame caption.</summary>
public class RibbonWindowTests
{
    public static TheoryData<ThemeFamily> AllFamilies() => new()
    {
        ThemeFamily.Aero2, ThemeFamily.AeroLite, ThemeFamily.Aero, ThemeFamily.Luna, ThemeFamily.Royale, ThemeFamily.Classic, ThemeFamily.Fluent,
    };

    /// <summary>A Ribbon with a QAT of two buttons and a "Picture Tools" contextual group over a Format tab.</summary>
    private static Ribbon ContextualRibbon()
    {
        var ribbon = RibbonSamples.ThreeGroupRibbon();
        ribbon.QuickAccessToolBar!.Items.Add(new RibbonButton { Label = "Save", SmallImageSource = RibbonSamples.Image(16) });
        ribbon.QuickAccessToolBar.Items.Add(new RibbonButton { Label = "Undo", SmallImageSource = RibbonSamples.Image(16) });
        ribbon.ContextualTabGroups.Add(new RibbonContextualTabGroup { Header = "Picture Tools", Background = Brushes.Orange });
        var format = new RibbonTab { Header = "Format", ContextualTabGroupHeader = "Picture Tools" };
        format.Items.Add(RibbonSamples.Group("Adjust", 2));
        ribbon.Items.Add(format);
        return ribbon;
    }

    private static RibbonWindow ShowWindow(Ribbon ribbon)
    {
        var window = new RibbonWindow
        {
            Title = "Document - WordPad",
            Width = 900,
            Height = 400,
            Content = new DockPanel { Children = { ribbon, new TextBox() } },
        };
        DockPanel.SetDock(ribbon, Dock.Top);
        window.Show();
        RibbonSamples.Settle(window);
        return window;
    }

    private static T Part<T>(TemplatedControl owner, string name)
        where T : Control =>
        owner.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == owner);

    [AvaloniaFact]
    public void Ribbon_In_A_RibbonWindow_Moves_Its_Toolbar_And_Group_Headers_Into_The_Caption()
    {
        var ribbon = ContextualRibbon();
        var window = ShowWindow(ribbon);
        var frame = window.Frame!;

        Assert.True(ribbon.IsHostedInRibbonWindow);
        Assert.Contains(":hosted", ribbon.Classes);
        Assert.Same(ribbon, window.Ribbon);

        // The toolbar is in the caption content slot, between its caps.
        var host = Assert.IsType<RibbonCaptionQuickAccessToolBarHost>(window.CaptionContent);
        Assert.Same(ribbon.QuickAccessToolBar, host.Content);
        var captionSlot = Part<ContentPresenter>(frame, WindowFrame.CaptionContentPartName);
        Assert.Contains(captionSlot, ribbon.QuickAccessToolBar!.GetVisualAncestors());
        Assert.True(ribbon.QuickAccessToolBar!.IsEffectivelyVisible);
        Assert.Same(ribbon, ribbon.QuickAccessToolBar!.Ribbon);

        // The Ribbon's own title row is hidden.
        Assert.False(Part<Panel>(ribbon, "titlePanel").IsVisible);

        // The contextual group header is in the overlay slot, over the Format tab header.
        var row = Assert.IsType<RibbonContextualTabGroupItemsControl>(window.CaptionOverlay);
        Assert.True(row.IsInCaption);
        Assert.Same(ribbon, row.Ribbon);
        var group = ribbon.ContextualTabGroups[0];
        Assert.Contains(row, group.GetVisualAncestors());
        Assert.Contains(":incaption", group.Classes);
        var formatHeader = ribbon.TabHeaders.Single(h => Equals(h.Content, "Format"));
        var groupX = group.TranslatePoint(default, window)!.Value.X;
        var headerX = formatHeader.TranslatePoint(default, window)!.Value.X;
        Assert.InRange(groupX, headerX - 1, headerX + 1);
        Assert.InRange(group.Bounds.Width, formatHeader.Bounds.Width - 1, formatHeader.Bounds.Width + 1);
        Assert.True(group.Bounds.Height > 0);

        // The header is in the caption, above the tab row.
        Assert.True(group.TranslatePoint(default, window)!.Value.Y < formatHeader.TranslatePoint(default, window)!.Value.Y);
        window.Close();
    }

    [AvaloniaFact]
    public void Clicking_The_Caption_Group_Header_Selects_Its_Tab()
    {
        var ribbon = ContextualRibbon();
        var window = ShowWindow(ribbon);
        var group = ribbon.ContextualTabGroups[0];
        var center = group.TranslatePoint(new Point(group.Bounds.Width / 2, group.Bounds.Height / 2), window)!.Value;
        var drags = 0;
        window.Frame!.DragRequested += (_, _) => drags++;
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Assert.Equal(2, ribbon.SelectedIndex);
        Assert.Equal(0, drags);
        window.Close();
    }

    [AvaloniaFact]
    public void Toolbar_Below_The_Ribbon_Leaves_The_Caption()
    {
        var ribbon = ContextualRibbon();
        var window = ShowWindow(ribbon);
        ribbon.ShowQuickAccessToolBarOnTop = false;
        RibbonSamples.Settle(window);
        Assert.Null(window.CaptionContent);
        Assert.Contains(ribbon, ribbon.QuickAccessToolBar!.GetVisualAncestors());
        Assert.True(ribbon.QuickAccessToolBar!.IsEffectivelyVisible);

        ribbon.ShowQuickAccessToolBarOnTop = true;
        RibbonSamples.Settle(window);
        Assert.IsType<RibbonCaptionQuickAccessToolBarHost>(window.CaptionContent);
        Assert.DoesNotContain(ribbon, ribbon.QuickAccessToolBar!.GetVisualAncestors());
        window.Close();
    }

    [AvaloniaFact]
    public void Removing_The_Ribbon_Gives_The_Caption_Back()
    {
        var ribbon = ContextualRibbon();
        var window = ShowWindow(ribbon);
        var group = ribbon.ContextualTabGroups[0];
        ((DockPanel)window.Content!).Children.Remove(ribbon);
        var stack = new StackPanel { Children = { ribbon } };
        window.Content = stack;
        RibbonSamples.Settle(window);

        // Still hosted: the new content is in the same window.
        Assert.True(ribbon.IsHostedInRibbonWindow);

        window.Content = null;
        stack.Children.Remove(ribbon);
        RibbonSamples.Settle(window);
        Assert.False(ribbon.IsHostedInRibbonWindow);
        Assert.Null(window.CaptionContent);
        Assert.Null(window.CaptionOverlay);
        Assert.Null(window.Ribbon);

        // Shown in a plain window, the Ribbon draws its own title row with the toolbar and the group headers again.
        var plain = RibbonSamples.Show(ribbon);
        Assert.False(ribbon.IsHostedInRibbonWindow);
        Assert.True(Part<Panel>(ribbon, "titlePanel").IsVisible);
        Assert.Contains(ribbon, ribbon.QuickAccessToolBar!.GetVisualAncestors());
        Assert.Contains(ribbon, group.GetVisualAncestors());
        Assert.DoesNotContain(":incaption", group.Classes);
        plain.Close();
        window.Close();
    }

    [AvaloniaFact]
    public void Only_The_First_Ribbon_Takes_The_Caption()
    {
        var first = ContextualRibbon();
        var second = RibbonSamples.ThreeGroupRibbon();
        var window = new RibbonWindow { Width = 900, Height = 600, Content = new StackPanel { Children = { first, second } } };
        window.Show();
        RibbonSamples.Settle(window);
        Assert.True(first.IsHostedInRibbonWindow);
        Assert.False(second.IsHostedInRibbonWindow);
        Assert.Same(first, window.Ribbon);
        window.Close();
    }

    [AvaloniaFact]
    public void Plain_ThemeWindow_Does_Not_Host_The_Ribbon()
    {
        var ribbon = ContextualRibbon();
        var window = new ThemeWindow { Width = 900, Height = 400, Content = ribbon };
        window.Show();
        RibbonSamples.Settle(window);
        Assert.False(ribbon.IsHostedInRibbonWindow);
        Assert.Null(window.CaptionContent);
        window.Close();
    }

    [AvaloniaFact]
    public void A_WindowFrame_With_HostsRibbon_Gives_Its_Caption_To_The_Ribbon()
    {
        var ribbon = ContextualRibbon();
        var frame = new WindowFrame { Title = "WordPad", Content = ribbon };
        RibbonWindow.SetHostsRibbon(frame, true);
        var window = RibbonSamples.Show(frame, 900, 400);
        Assert.True(ribbon.IsHostedInRibbonWindow);
        Assert.IsType<RibbonCaptionQuickAccessToolBarHost>(frame.CaptionContent);
        Assert.IsType<RibbonContextualTabGroupItemsControl>(frame.CaptionOverlay);
        Assert.Contains(frame, ribbon.QuickAccessToolBar!.GetVisualAncestors());
        window.Close();
    }

    [AvaloniaFact]
    public void Collapsed_Ribbon_Empties_The_Caption()
    {
        var ribbon = ContextualRibbon();
        var window = ShowWindow(ribbon);
        ribbon.IsCollapsed = true;
        RibbonSamples.Settle(window);
        Assert.Null(window.CaptionContent);
        Assert.Null(window.CaptionOverlay);
        ribbon.IsCollapsed = false;
        RibbonSamples.Settle(window);
        Assert.NotNull(window.CaptionContent);
        Assert.NotNull(window.CaptionOverlay);
        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(AllFamilies))]
    public void Caption_Hosts_The_Toolbar_In_Every_Family(ThemeFamily family)
    {
        TestApplication.Instance.Theme.Theme = family;
        var ribbon = ContextualRibbon();
        var window = ShowWindow(ribbon);
        if (Environment.GetEnvironmentVariable("AVAWPF_SHOTS") is { } dir)
        {
            Directory.CreateDirectory(dir);
            using var shot = window.CaptureRenderedFrame();
            shot?.Save(Path.Combine(dir, $"ribbonwindow.{family}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        var frame = window.Frame!;
        var caption = Part<Control>(frame, WindowFrame.CaptionPartName);
        var qat = ribbon.QuickAccessToolBar!;
        Assert.True(qat.IsEffectivelyVisible);
        Assert.True(qat.Bounds.Height > 0 && qat.Bounds.Height <= caption.Bounds.Height, $"{family}: the toolbar does not fit the caption");
        Assert.False(qat.HasOverflowItems);
        var group = ribbon.ContextualTabGroups[0];
        Assert.True(group.IsEffectivelyVisible && group.Bounds.Width > 0, $"{family}: no group header in the caption");

        // The caption still shows the title, after the group header.
        var title = Part<TextBlock>(frame, "title");
        Assert.True(title.IsEffectivelyVisible);
        Assert.True(title.TranslatePoint(default, window)!.Value.X >= group.TranslatePoint(default, window)!.Value.X + group.Bounds.Width - 0.5);
        window.Close();
    }
}
