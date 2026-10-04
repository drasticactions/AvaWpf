// Ported from WPF $W/PresentationFramework/System/Windows/Automation/Peers/StatusBarItemAutomationPeer.cs (MIT, see NOTICE.md).
using Avalonia.Automation.Peers;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>Exposes a <see cref="StatusBarItem"/> to UI Automation as text named by its content.</summary>
public class StatusBarItemAutomationPeer : ContentControlAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="StatusBarItemAutomationPeer"/> class.</summary>
    /// <param name="owner">The status bar item.</param>
    public StatusBarItemAutomationPeer(StatusBarItem owner)
        : base(owner)
    {
    }

    /// <summary>The status bar item.</summary>
    public new StatusBarItem Owner => (StatusBarItem)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "StatusBarItem";

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => true;

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => true;
}
