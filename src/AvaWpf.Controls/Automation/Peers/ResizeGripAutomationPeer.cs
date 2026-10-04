using Avalonia.Automation.Peers;

namespace AvaWpf.Controls.Automation.Peers;

/// <summary>Exposes a <see cref="ResizeGrip"/> to UI Automation as a non-content thumb, so screen readers skip it.</summary>
public class ResizeGripAutomationPeer : ControlAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="ResizeGripAutomationPeer"/> class.</summary>
    /// <param name="owner">The resize grip.</param>
    public ResizeGripAutomationPeer(ResizeGrip owner)
        : base(owner)
    {
    }

    /// <summary>The resize grip.</summary>
    public new ResizeGrip Owner => (ResizeGrip)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Thumb;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "ResizeGrip";

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => false;
}
