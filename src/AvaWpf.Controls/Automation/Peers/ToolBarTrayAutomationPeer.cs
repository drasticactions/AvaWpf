using System.Collections.Generic;
using Avalonia.Automation.Peers;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>
/// Exposes a <see cref="ToolBarTray"/> to UI Automation as a pane that holds its tool bars (WPF gives the tray no peer).
/// </summary>
public class ToolBarTrayAutomationPeer : ControlAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="ToolBarTrayAutomationPeer"/> class.</summary>
    /// <param name="owner">The tool bar tray.</param>
    public ToolBarTrayAutomationPeer(ToolBarTray owner)
        : base(owner)
    {
        owner.ToolBars.CollectionChanged += (_, _) => InvalidateChildren();
    }

    /// <summary>The tool bar tray.</summary>
    public new ToolBarTray Owner => (ToolBarTray)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "ToolBarTray";

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => false;

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        var result = new List<AutomationPeer>(Owner.ToolBars.Count);
        foreach (var toolBar in Owner.ToolBars)
        {
            if (toolBar.IsVisible)
            {
                result.Add(GetOrCreate(toolBar));
            }
        }

        return result;
    }
}
