// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonWindow.cs (MIT, see NOTICE.md).
using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaWpf.Ribbon.Primitives;

namespace AvaWpf.Ribbon;

/// <summary>
/// A <see cref="ThemeWindow"/> that moves a <see cref="Ribbon"/>'s Quick Access Toolbar and contextual tab group
/// headers into the caption of its <see cref="WindowFrame"/>.
/// </summary>
/// <remarks>
/// The first Ribbon in the content takes the caption, through <see cref="ThemeWindow.CaptionContent"/> (the toolbar)
/// and <see cref="ThemeWindow.CaptionOverlay"/> (the group headers). Without a window (Browser), set
/// <see cref="HostsRibbonProperty"/> on a <see cref="WindowFrame"/>.
/// </remarks>
public class RibbonWindow : ThemeWindow
{
    /// <summary>
    /// Defines the HostsRibbon attached property: gives a <see cref="WindowFrame"/> shown as a control the caption
    /// behavior of a <see cref="RibbonWindow"/>.
    /// </summary>
    public static readonly AttachedProperty<bool> HostsRibbonProperty =
        AvaloniaProperty.RegisterAttached<RibbonWindow, WindowFrame, bool>("HostsRibbon");

    private static readonly ConditionalWeakTable<WindowFrame, RibbonCaption> s_frameCaptions = new();

    private readonly RibbonCaption _caption;

    /// <summary>Initializes a new instance of the <see cref="RibbonWindow"/> class.</summary>
    public RibbonWindow()
    {
        _caption = new RibbonCaption((content, overlay) =>
        {
            CaptionContent = content;
            CaptionOverlay = overlay;
        });
    }

    /// <summary>The Ribbon that has the caption, if any.</summary>
    public Ribbon? Ribbon => _caption.Owner;

    /// <summary>Gets whether a frame gives its caption to a Ribbon inside it.</summary>
    /// <param name="frame">The frame.</param>
    /// <returns>The value.</returns>
    public static bool GetHostsRibbon(WindowFrame frame) => frame.GetValue(HostsRibbonProperty);

    /// <summary>Sets whether a frame gives its caption to a Ribbon inside it.</summary>
    /// <param name="frame">The frame.</param>
    /// <param name="value">The value.</param>
    public static void SetHostsRibbon(WindowFrame frame, bool value) => frame.SetValue(HostsRibbonProperty, value);

    /// <summary>The caption a Ribbon inside <paramref name="frame"/> may take, or null.</summary>
    internal static RibbonCaption? CaptionFor(WindowFrame frame)
    {
        if (frame.TemplatedParent is RibbonWindow window)
        {
            return window._caption;
        }

        if (!GetHostsRibbon(frame))
        {
            return null;
        }

        return s_frameCaptions.GetValue(frame, f => new RibbonCaption((content, overlay) =>
        {
            f.CaptionContent = content;
            f.CaptionOverlay = overlay;
        }));
    }
}

/// <summary>The caption pieces a frame lends to one Ribbon: the Quick Access Toolbar host and the group header row.</summary>
internal sealed class RibbonCaption
{
    private readonly Action<object?, object?> _show;
    private IDisposable[] _fontBindings = [];

    public RibbonCaption(Action<object?, object?> show)
    {
        _show = show;
        ContextualTabGroups = new RibbonContextualTabGroupItemsControl();
        ContextualTabGroups.SetIsInCaption(true);
    }

    /// <summary>The Ribbon that has the caption.</summary>
    public Ribbon? Owner { get; private set; }

    /// <summary>The host of the Quick Access Toolbar, shown after the caption icon.</summary>
    public RibbonCaptionQuickAccessToolBarHost QuickAccessToolBarHost { get; } = new();

    /// <summary>The contextual tab group headers, laid over the title area.</summary>
    public RibbonContextualTabGroupItemsControl ContextualTabGroups { get; }

    /// <summary>Gives the caption to <paramref name="ribbon"/>; false when another Ribbon has it.</summary>
    public bool TryAttach(Ribbon ribbon)
    {
        if (Owner is not null && Owner != ribbon)
        {
            return false;
        }

        Owner = ribbon;
        QuickAccessToolBarHost.SetValue(RibbonControlService.RibbonProperty, ribbon);
        ContextualTabGroups.SetValue(RibbonControlService.RibbonProperty, ribbon);
        ContextualTabGroups.OwnerRibbon = ribbon;

        // The header row is outside the Ribbon, so it takes the Ribbon's font and text color itself, not the caption's.
        _fontBindings =
        [
            ContextualTabGroups.Bind(TemplatedControl.FontFamilyProperty, ribbon.GetObservable(TemplatedControl.FontFamilyProperty)),
            ContextualTabGroups.Bind(TemplatedControl.FontSizeProperty, ribbon.GetObservable(TemplatedControl.FontSizeProperty)),
            ContextualTabGroups.Bind(TemplatedControl.ForegroundProperty, ribbon.GetObservable(TemplatedControl.ForegroundProperty)),
        ];
        return true;
    }

    /// <summary>Takes the caption back from <paramref name="ribbon"/>.</summary>
    public void Detach(Ribbon ribbon)
    {
        if (Owner != ribbon)
        {
            return;
        }

        Update(showToolBar: false, showGroups: false);
        QuickAccessToolBarHost.Content = null;
        ContextualTabGroups.ItemsSource = null;
        QuickAccessToolBarHost.ClearValue(RibbonControlService.RibbonProperty);
        ContextualTabGroups.ClearValue(RibbonControlService.RibbonProperty);
        ContextualTabGroups.OwnerRibbon = null;
        foreach (var binding in _fontBindings)
        {
            binding.Dispose();
        }

        _fontBindings = [];
        Owner = null;
    }

    /// <summary>Shows or hides the two pieces in the frame's caption slots.</summary>
    public void Update(bool showToolBar, bool showGroups) =>
        _show(showToolBar ? QuickAccessToolBarHost : null, showGroups ? ContextualTabGroups : null);
}
