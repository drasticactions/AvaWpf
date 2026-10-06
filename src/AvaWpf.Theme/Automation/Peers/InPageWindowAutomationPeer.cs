using Avalonia.Automation.Peers;

namespace AvaWpf.Automation.Peers;

/// <summary>
/// Exposes an <see cref="InPageWindow"/> to UI Automation as a window named by its title, as a
/// <see cref="ThemeWindowAutomationPeer"/> exposes a desktop window.
/// </summary>
public class InPageWindowAutomationPeer : ContentControlAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="InPageWindowAutomationPeer"/> class.</summary>
    /// <param name="owner">The window.</param>
    public InPageWindowAutomationPeer(InPageWindow owner)
        : base(owner)
    {
    }

    /// <summary>The window.</summary>
    public new InPageWindow Owner => (InPageWindow)base.Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "InPageWindow";

    /// <inheritdoc/>
    protected override string? GetNameCore() => Owner.Title is { Length: > 0 } title ? title : base.GetNameCore();

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => true;

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => true;
}
