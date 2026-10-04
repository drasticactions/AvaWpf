using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

public class WindowFrameTests
{
    /// <summary>Renders a control to a PNG when <c>AVAWPF_SHOTS</c> names a folder (for looking at templates).</summary>
    internal static void Shoot(Control content, string name, double width = 360, double height = 220)
    {
        var dir = Environment.GetEnvironmentVariable("AVAWPF_SHOTS");
        var window = new Window { Content = content, Width = width, Height = height };
        window.Show();
        window.UpdateLayout();
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
            using var frame = window.CaptureRenderedFrame();
            frame?.Save(Path.Combine(dir, name + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2, "Light")]
    [InlineData(ThemeFamily.Aero2, "Dark")]
    public void Frame_Builds_Caption_And_Buttons(ThemeFamily family, string variant)
    {
        var frame = new WindowFrame { Title = "Untitled - Notepad", Content = new TextBlock { Text = "Client area", Margin = new Thickness(8) } };
        var scope = new ThemeScope { Theme = family, RequestedThemeVariant = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light, Child = frame, Margin = new Thickness(10) };
        Shoot(scope, $"frame.{family}.{variant}");
        frame.ApplyTemplate();

        var invoked = (CaptionButton?)null;
        frame.CaptionButtonInvoked += (_, e) => invoked = e.Button;
        frame.InvokeCaptionButton(CaptionButton.Close);
        Assert.Equal(CaptionButton.Close, invoked);
    }

    [AvaloniaFact]
    public void Dialog_Hides_Minimize_And_Maximize()
    {
        var frame = new WindowFrame { Kind = WindowFrameKind.Dialog };
        Assert.False(frame.IsMinimizeButtonVisible);
        Assert.False(frame.IsMaximizeButtonVisible);
        Assert.Contains(":dialog", frame.Classes);
    }

    public static TheoryData<ThemeFamily> AllFamilies() => new()
    {
        ThemeFamily.Aero2, ThemeFamily.AeroLite, ThemeFamily.Aero, ThemeFamily.Luna, ThemeFamily.Royale, ThemeFamily.Classic, ThemeFamily.Fluent,
    };

    private static (Window Window, WindowFrame Frame) ShowFrame(ThemeFamily family, object? captionContent, object? captionOverlay)
    {
        var frame = new WindowFrame
        {
            Title = "Document - WordPad",
            CaptionContent = captionContent,
            CaptionOverlay = captionOverlay,
            Content = new Border { Height = 120 },
        };
        var scope = new ThemeScope { Theme = family, Child = frame };
        var window = new Window { Content = scope, Width = 600, Height = 260 };
        window.Show();
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }

        return (window, frame);
    }

    private static Control Part(WindowFrame frame, string name) =>
        frame.GetVisualDescendants().OfType<Control>().First(c => c.Name == name && c.TemplatedParent == frame);

    private static Point CenterIn(Visual element, Visual root) =>
        element.TranslatePoint(new Point(element.Bounds.Width / 2, element.Bounds.Height / 2), root)!.Value;

    [AvaloniaTheory]
    [MemberData(nameof(AllFamilies))]
    public void Caption_Slots_Sit_Between_The_Icon_And_The_Caption_Buttons(ThemeFamily family)
    {
        var content = new Button { Content = "Q", Width = 40, Height = 16 };
        var overlay = new Border { Width = 60, Height = 10, Background = Avalonia.Media.Brushes.Orange };
        var (window, frame) = ShowFrame(family, content, overlay);
        if (Environment.GetEnvironmentVariable("AVAWPF_SHOTS") is { } dir)
        {
            Directory.CreateDirectory(dir);
            using var shot = window.CaptureRenderedFrame();
            shot?.Save(Path.Combine(dir, $"caption-slots.{family}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        }

        var caption = Part(frame, WindowFrame.CaptionPartName);
        var contentPresenter = Part(frame, WindowFrame.CaptionContentPartName);
        var overlayPresenter = Part(frame, WindowFrame.CaptionOverlayPartName);
        Assert.True(contentPresenter.IsVisible);
        Assert.True(overlayPresenter.IsVisible);
        Assert.Same(content, contentPresenter.GetVisualDescendants().OfType<Button>().Single());
        Assert.Contains(caption, contentPresenter.GetVisualAncestors());
        Assert.Contains(caption, overlayPresenter.GetVisualAncestors());

        // Left to right: caption content, overlay, title, caption buttons.
        var contentX = content.TranslatePoint(default, frame)!.Value.X;
        var overlayX = overlay.TranslatePoint(default, frame)!.Value.X;
        var title = Part(frame, "title");
        var close = Part(frame, WindowFrame.ClosePartName);
        Assert.True(contentX + content.Bounds.Width <= overlayX + 0.5, $"{family}: overlay overlaps the caption content");
        Assert.True(overlayX + overlay.Bounds.Width <= title.TranslatePoint(default, frame)!.Value.X + 0.5, $"{family}: title starts under the overlay");
        Assert.True(overlayX + overlay.Bounds.Width <= close.TranslatePoint(default, frame)!.Value.X, $"{family}: overlay reaches the caption buttons");

        // The slots take the caption text color of the frame.
        Assert.Equal(
            (((TextBlock)title).Foreground as Avalonia.Media.ISolidColorBrush)?.Color,
            (((Avalonia.Controls.Presenters.ContentPresenter)contentPresenter).Foreground as Avalonia.Media.ISolidColorBrush)?.Color);
        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(AllFamilies))]
    public void Empty_Caption_Slots_Are_Hidden(ThemeFamily family)
    {
        var (window, frame) = ShowFrame(family, null, null);
        Assert.False(Part(frame, WindowFrame.CaptionContentPartName).IsVisible);
        Assert.False(Part(frame, WindowFrame.CaptionOverlayPartName).IsVisible);
        window.Close();
    }

    [AvaloniaTheory]
    [MemberData(nameof(AllFamilies))]
    public void Caption_Drags_Around_The_Slot_Controls_But_Not_Through_Them(ThemeFamily family)
    {
        var clicked = 0;
        var content = new Button { Content = "Q", Width = 40, Height = 16 };
        content.Click += (_, _) => clicked++;

        // A panel without a background: its empty area must still drag the window.
        var overlay = new StackPanel { Orientation = Orientation.Horizontal, Width = 80, VerticalAlignment = VerticalAlignment.Stretch };
        var (window, frame) = ShowFrame(family, content, overlay);
        var drags = 0;
        var maximized = 0;
        frame.DragRequested += (_, _) => drags++;
        frame.CaptionButtonInvoked += (_, e) => maximized += e.Button == CaptionButton.Maximize ? 1 : 0;

        // A click on the control in the slot clicks it and does not drag.
        var p = CenterIn(content, window);
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Assert.Equal(1, clicked);
        Assert.Equal(0, drags);

        // A double click on it does not maximize either.
        window.MouseDown(p, MouseButton.Left);
        window.MouseUp(p, MouseButton.Left);
        Assert.Equal(0, maximized);

        // The empty overlay area drags.
        var q = CenterIn(overlay, window);
        window.MouseDown(q, MouseButton.Left);
        window.MouseUp(q, MouseButton.Left);
        Assert.Equal(1, drags);
        window.Close();
    }

    [AvaloniaFact]
    public void ThemeWindow_Requests_Mica_Only_Under_Fluent()
    {
        var theme = TestApplication.Instance.Theme;
        var window = new ThemeWindow { Title = "T" };
        window.Show();
        Assert.NotEqual(WindowTransparencyLevel.Mica, window.TransparencyLevelHint[0]);
        theme.Theme = ThemeFamily.Fluent;
        Assert.Equal(WindowTransparencyLevel.Mica, window.TransparencyLevelHint[0]);
        theme.Theme = ThemeFamily.Aero2;
        Assert.Equal(WindowTransparencyLevel.Transparent, window.TransparencyLevelHint[0]);
        window.Close();
    }

    [AvaloniaFact]
    public void ThemeWindow_Passes_The_Caption_Slots_To_Its_Frame()
    {
        var content = new TextBlock { Text = "QAT" };
        var overlay = new TextBlock { Text = "Tools" };
        var window = new ThemeWindow { Width = 500, Height = 300, CaptionContent = content, CaptionOverlay = overlay };
        window.Show();
        window.UpdateLayout();
        Assert.NotNull(window.Frame);
        Assert.Same(content, window.Frame!.CaptionContent);
        Assert.Same(overlay, window.Frame.CaptionOverlay);
        Assert.True(content.IsEffectivelyVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void Backdrop_Sets_The_Pseudo_Class()
    {
        var frame = new WindowFrame { IsBackdropVisible = true };
        Assert.Contains(":backdrop", frame.Classes);
    }

    /// <summary>An app window derived from ThemeWindow, as apps (and the gallery) write them.</summary>
    private sealed class AppWindow : ThemeWindow
    {
    }

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Luna)]
    [InlineData(ThemeFamily.Classic)]
    [InlineData(ThemeFamily.Fluent)]
    public void Family_Switch_Retemplates_The_Frame_Of_A_Derived_ThemeWindow(ThemeFamily target)
    {
        var theme = TestApplication.Instance.Theme;
        theme.Theme = ThemeFamily.Aero2;
        var window = new AppWindow { Title = "App", Width = 400, Height = 300, Content = new Button { Content = "OK" } };
        window.Show();
        window.UpdateLayout();
        var before = window.Frame;
        Assert.NotNull(before);

        theme.Theme = target;
        window.UpdateLayout();

        Assert.NotNull(window.Frame);
        Assert.NotSame(before, window.Frame);
        Assert.True(window.Frame!.TryFindResource(typeof(WindowFrame), window.ActualThemeVariant, out var expected));
        Assert.Contains(window.Frame.GetVisualChildren(), _ => true);
        theme.Theme = ThemeFamily.Aero2;
        window.Close();
        _ = expected;
    }
}
