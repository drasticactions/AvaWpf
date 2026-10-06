using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaWpf;

/// <summary>
/// The window settings a dialog's content declares, as attached properties on the content (usually a
/// <see cref="UserControl"/>). <see cref="WindowHost"/> applies them to whichever window shows the content: a
/// <see cref="ThemeWindow"/> on the desktop or an <see cref="InPageWindow"/> in a single-view app (the browser).
/// </summary>
/// <remarks>
/// <see cref="WidthProperty"/> and <see cref="HeightProperty"/> (and the minimums) are the window's size, frame included,
/// as on <see cref="Window"/>; leave them unset to size to the content.
/// </remarks>
public static class WindowSettings
{
    /// <summary>Defines the Title attached property.</summary>
    public static readonly AttachedProperty<string?> TitleProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("Title", typeof(WindowSettings));

    /// <summary>Defines the Icon attached property: the caption icon.</summary>
    public static readonly AttachedProperty<IImage?> IconProperty =
        AvaloniaProperty.RegisterAttached<Control, IImage?>("Icon", typeof(WindowSettings));

    /// <summary>Defines the FrameKind attached property.</summary>
    public static readonly AttachedProperty<WindowFrameKind> FrameKindProperty =
        AvaloniaProperty.RegisterAttached<Control, WindowFrameKind>("FrameKind", typeof(WindowSettings), WindowFrameKind.Dialog);

    /// <summary>Defines the ShowFrameTitle attached property.</summary>
    public static readonly AttachedProperty<bool> ShowFrameTitleProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("ShowFrameTitle", typeof(WindowSettings), true);

    /// <summary>Defines the ShowFrameIcon attached property.</summary>
    public static readonly AttachedProperty<bool> ShowFrameIconProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("ShowFrameIcon", typeof(WindowSettings), true);

    /// <summary>Defines the SizeToContent attached property.</summary>
    public static readonly AttachedProperty<SizeToContent> SizeToContentProperty =
        AvaloniaProperty.RegisterAttached<Control, SizeToContent>("SizeToContent", typeof(WindowSettings));

    /// <summary>Defines the Width attached property: the window width, NaN to size to the content.</summary>
    public static readonly AttachedProperty<double> WidthProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("Width", typeof(WindowSettings), double.NaN);

    /// <summary>Defines the Height attached property: the window height, NaN to size to the content.</summary>
    public static readonly AttachedProperty<double> HeightProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("Height", typeof(WindowSettings), double.NaN);

    /// <summary>Defines the MinWidth attached property.</summary>
    public static readonly AttachedProperty<double> MinWidthProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("MinWidth", typeof(WindowSettings));

    /// <summary>Defines the MinHeight attached property.</summary>
    public static readonly AttachedProperty<double> MinHeightProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("MinHeight", typeof(WindowSettings));

    /// <summary>Defines the MaxWidth attached property.</summary>
    public static readonly AttachedProperty<double> MaxWidthProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("MaxWidth", typeof(WindowSettings), double.PositiveInfinity);

    /// <summary>Defines the MaxHeight attached property.</summary>
    public static readonly AttachedProperty<double> MaxHeightProperty =
        AvaloniaProperty.RegisterAttached<Control, double>("MaxHeight", typeof(WindowSettings), double.PositiveInfinity);

    /// <summary>Defines the CanResize attached property.</summary>
    public static readonly AttachedProperty<bool> CanResizeProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("CanResize", typeof(WindowSettings));

    /// <summary>Defines the WindowStartupLocation attached property.</summary>
    public static readonly AttachedProperty<WindowStartupLocation> WindowStartupLocationProperty =
        AvaloniaProperty.RegisterAttached<Control, WindowStartupLocation>("WindowStartupLocation", typeof(WindowSettings), WindowStartupLocation.CenterOwner);

    /// <summary>Defines the ShowInTaskbar attached property (ignored by in-page windows).</summary>
    public static readonly AttachedProperty<bool> ShowInTaskbarProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("ShowInTaskbar", typeof(WindowSettings));

    /// <summary>Gets the window title.</summary>
    public static string? GetTitle(Control control) => control.GetValue(TitleProperty);

    /// <summary>Sets the window title.</summary>
    public static void SetTitle(Control control, string? value) => control.SetValue(TitleProperty, value);

    /// <summary>Gets the caption icon.</summary>
    public static IImage? GetIcon(Control control) => control.GetValue(IconProperty);

    /// <summary>Sets the caption icon.</summary>
    public static void SetIcon(Control control, IImage? value) => control.SetValue(IconProperty, value);

    /// <summary>Gets the frame kind (default <see cref="WindowFrameKind.Dialog"/>).</summary>
    public static WindowFrameKind GetFrameKind(Control control) => control.GetValue(FrameKindProperty);

    /// <summary>Sets the frame kind.</summary>
    public static void SetFrameKind(Control control, WindowFrameKind value) => control.SetValue(FrameKindProperty, value);

    /// <summary>Gets whether the caption shows the title.</summary>
    public static bool GetShowFrameTitle(Control control) => control.GetValue(ShowFrameTitleProperty);

    /// <summary>Sets whether the caption shows the title.</summary>
    public static void SetShowFrameTitle(Control control, bool value) => control.SetValue(ShowFrameTitleProperty, value);

    /// <summary>Gets whether the caption shows the icon.</summary>
    public static bool GetShowFrameIcon(Control control) => control.GetValue(ShowFrameIconProperty);

    /// <summary>Sets whether the caption shows the icon.</summary>
    public static void SetShowFrameIcon(Control control, bool value) => control.SetValue(ShowFrameIconProperty, value);

    /// <summary>Gets how the window sizes to its content.</summary>
    public static SizeToContent GetSizeToContent(Control control) => control.GetValue(SizeToContentProperty);

    /// <summary>Sets how the window sizes to its content.</summary>
    public static void SetSizeToContent(Control control, SizeToContent value) => control.SetValue(SizeToContentProperty, value);

    /// <summary>Gets the window width.</summary>
    public static double GetWidth(Control control) => control.GetValue(WidthProperty);

    /// <summary>Sets the window width.</summary>
    public static void SetWidth(Control control, double value) => control.SetValue(WidthProperty, value);

    /// <summary>Gets the window height.</summary>
    public static double GetHeight(Control control) => control.GetValue(HeightProperty);

    /// <summary>Sets the window height.</summary>
    public static void SetHeight(Control control, double value) => control.SetValue(HeightProperty, value);

    /// <summary>Gets the window's minimum width.</summary>
    public static double GetMinWidth(Control control) => control.GetValue(MinWidthProperty);

    /// <summary>Sets the window's minimum width.</summary>
    public static void SetMinWidth(Control control, double value) => control.SetValue(MinWidthProperty, value);

    /// <summary>Gets the window's minimum height.</summary>
    public static double GetMinHeight(Control control) => control.GetValue(MinHeightProperty);

    /// <summary>Sets the window's minimum height.</summary>
    public static void SetMinHeight(Control control, double value) => control.SetValue(MinHeightProperty, value);

    /// <summary>Gets the window's maximum width.</summary>
    public static double GetMaxWidth(Control control) => control.GetValue(MaxWidthProperty);

    /// <summary>Sets the window's maximum width.</summary>
    public static void SetMaxWidth(Control control, double value) => control.SetValue(MaxWidthProperty, value);

    /// <summary>Gets the window's maximum height.</summary>
    public static double GetMaxHeight(Control control) => control.GetValue(MaxHeightProperty);

    /// <summary>Sets the window's maximum height.</summary>
    public static void SetMaxHeight(Control control, double value) => control.SetValue(MaxHeightProperty, value);

    /// <summary>Gets whether the user can resize the window.</summary>
    public static bool GetCanResize(Control control) => control.GetValue(CanResizeProperty);

    /// <summary>Sets whether the user can resize the window.</summary>
    public static void SetCanResize(Control control, bool value) => control.SetValue(CanResizeProperty, value);

    /// <summary>Gets where the window opens (default <see cref="WindowStartupLocation.CenterOwner"/>).</summary>
    public static WindowStartupLocation GetWindowStartupLocation(Control control) => control.GetValue(WindowStartupLocationProperty);

    /// <summary>Sets where the window opens.</summary>
    public static void SetWindowStartupLocation(Control control, WindowStartupLocation value) => control.SetValue(WindowStartupLocationProperty, value);

    /// <summary>Gets whether a desktop window shows in the taskbar.</summary>
    public static bool GetShowInTaskbar(Control control) => control.GetValue(ShowInTaskbarProperty);

    /// <summary>Sets whether a desktop window shows in the taskbar.</summary>
    public static void SetShowInTaskbar(Control control, bool value) => control.SetValue(ShowInTaskbarProperty, value);
}
