// Ported from WPF $W/PresentationFramework/System/Windows/Controls/Primitives/StatusBarItem.cs (MIT, see NOTICE.md).
using Avalonia.Controls;
using Avalonia.Automation.Peers;
using AvaWpf.Controls.Automation.Peers;

namespace AvaWpf.Controls;

/// <summary>An item of a <see cref="StatusBar"/>.</summary>
public class StatusBarItem : ContentControl
{
    static StatusBarItem()
    {
        FocusableProperty.OverrideDefaultValue<StatusBarItem>(false);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new StatusBarItemAutomationPeer(this);
}
