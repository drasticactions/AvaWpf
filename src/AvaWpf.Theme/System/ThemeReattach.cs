using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvaWpf;

/// <summary>
/// Re-templates live controls after a family change by detaching and attaching the content of each root, because
/// Avalonia resolves an implicit ControlTheme only when a control attaches to the logical tree.
/// </summary>
/// <remarks>
/// <c>Loaded</c>/<c>Unloaded</c> fire again and scroll offsets and open popups reset; focus is restored. The content is
/// detached before the resources change, so the change notifications reach only the bare roots.
/// </remarks>
internal static class ThemeReattach
{
    private static readonly AttachedProperty<bool> s_managedThemeProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("ManagedTheme", typeof(ThemeReattach));

    private static readonly List<WeakReference<TopLevel>> s_topLevels = new();
    private static bool s_tracking;

    /// <summary>Tracks the open top levels so apps without a desktop lifetime are re-attached too.</summary>
    public static void EnsureTracking()
    {
        if (s_tracking)
        {
            return;
        }

        s_tracking = true;
        Control.LoadedEvent.AddClassHandler<TopLevel>((top, _) => Track(top), RoutingStrategies.Direct);
        Window.WindowOpenedEvent.AddClassHandler<Window>((w, _) => Track(w), RoutingStrategies.Direct);
    }

    private static void Track(TopLevel top)
    {
        // Grayscale antialiased text, as WPF's classic themes draw it.
        TextOptions.SetTextRenderingMode(top, TextRenderingMode.Antialias);
        s_topLevels.RemoveAll(w => !w.TryGetTarget(out var t) || t == top);
        s_topLevels.Add(new WeakReference<TopLevel>(top));
    }

    /// <summary>
    /// Re-attaches every root of the app, running <paramref name="swap"/> while the content is detached so its changes
    /// never walk the old trees.
    /// </summary>
    public static void ReattachApplication(Application app, Action swap)
    {
        var roots = new List<Control>();
        switch (app.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                roots.AddRange(desktop.Windows);
                break;
            case ISingleViewApplicationLifetime { MainView: { } view }:
                roots.Add(TopLevel.GetTopLevel(view) is { } top ? top : view);
                break;
        }

        foreach (var weak in s_topLevels.ToArray())
        {
            if (weak.TryGetTarget(out var top) && !roots.Contains(top) && (top is Window { IsVisible: true } || top.IsLoaded))
            {
                roots.Add(top);
            }
        }

        var detached = new List<Detached>(roots.Count);
        try
        {
            foreach (var root in roots)
            {
                detached.Add(Detach(root, retarget: true));
            }

            swap();
        }
        finally
        {
            foreach (var d in detached)
            {
                Attach(d);
            }
        }
    }

    /// <summary>Re-attaches the child of a <see cref="ThemeScope"/>, running <paramref name="swap"/> while it is detached.</summary>
    public static void ReattachChild(Decorator scope, Action swap)
    {
        var d = Detach(scope, retarget: false);
        try
        {
            swap();
        }
        finally
        {
            Attach(d);
        }
    }

    private static Detached Detach(Control root, bool retarget)
    {
        var focus = CaptureFocus(root);
        var snapshot = TemplateFrameCleanup.Capture(root);
        object? content = null;
        switch (root)
        {
            case ContentControl cc when cc.Content is { } c:
                content = c;
                cc.Content = null;
                break;
            case Decorator d when d.Child is { } child:
                content = child;
                d.Child = null;
                break;
        }

        return new Detached(root, content, focus, snapshot, retarget);
    }

    private static void Attach(Detached d)
    {
        if (d.Retarget)
        {
            RetargetTheme(d.Root);
        }

        switch (d.Root)
        {
            case ContentControl cc when d.Content is not null:
                cc.Content = d.Content;
                break;
            case Decorator dec when d.Content is Control child:
                dec.Child = child;
                break;
        }

        d.Root.UpdateLayout();
        TemplateFrameCleanup.ClearDiscarded(d.Snapshot);
        RestoreFocus(d.Focus);
    }

    /// <summary>Sets the ControlTheme the root resolves now, unless the app set one itself.</summary>
    private static void RetargetTheme(StyledElement root)
    {
        if (root.Theme is not null && !root.GetValue(s_managedThemeProperty))
        {
            return;
        }

        // The style key, not the CLR type: an app window derived from ThemeWindow is themed by the ThemeWindow theme.
        if (root.TryFindResource(root.StyleKey, root.ActualThemeVariant, out var found) && found is ControlTheme theme)
        {
            root.SetValue(s_managedThemeProperty, true);
            root.Theme = theme;
        }
    }

    private static IInputElement? CaptureFocus(Visual root) =>
        TopLevel.GetTopLevel(root)?.FocusManager?.GetFocusedElement() is Visual focused && (focused == root || root.IsVisualAncestorOf(focused))
            ? (IInputElement)focused
            : null;

    private static void RestoreFocus(IInputElement? focused)
    {
        for (var e = focused as ILogical; e is not null; e = e.LogicalParent)
        {
            if (e is InputElement { Focusable: true } input && input.IsAttachedToVisualTree())
            {
                input.Focus();
                return;
            }
        }
    }

    /// <summary>A root whose content is detached for a re-attach.</summary>
    private sealed record Detached(Control Root, object? Content, IInputElement? Focus, TemplateFrameCleanup.Snapshot Snapshot, bool Retarget);
}
