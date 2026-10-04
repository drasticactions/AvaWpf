// Ported from WPF $W/PresentationFramework/System/Windows/Controls/Primitives/StatusBar.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Automation.Peers;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Controls;

/// <summary>
/// The status bar at the bottom of a window; items are wrapped in <see cref="StatusBarItem"/>s on a
/// <see cref="DockPanel"/>.
/// </summary>
/// <remarks>A <see cref="Separator"/> item gets the <c>statusbar</c> style class (WPF's <c>StatusBar.SeparatorStyleKey</c>).</remarks>
public class StatusBar : SelectingItemsControl
{
    /// <summary>The style class given to <see cref="Separator"/> items.</summary>
    public const string SeparatorClass = "statusbar";

    private static readonly FuncTemplate<Panel?> s_defaultPanel = new(() => new DockPanel());

    static StatusBar()
    {
        ItemsPanelProperty.OverrideDefaultValue<StatusBar>(s_defaultPanel);
        FocusableProperty.OverrideDefaultValue<StatusBar>(false);
    }

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is StatusBarItem or Separator)
        {
            recycleKey = null;
            return false;
        }

        recycleKey = DefaultRecycleKey;
        return true;
    }

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new StatusBarItem();

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is Separator)
        {
            container.Classes.Add(SeparatorClass);
        }
    }

    /// <inheritdoc/>
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is Separator)
        {
            container.Classes.Remove(SeparatorClass);
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new StatusBarAutomationPeer(this);
}
