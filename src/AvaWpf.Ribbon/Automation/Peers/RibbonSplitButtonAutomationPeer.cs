// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonSplitButtonAutomationPeer.cs (MIT, see NOTICE.md).
using System;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonSplitButton"/> to UI Automation as a split button: invoke runs the header action, the
/// drop-down is the expand/collapse pattern, and a checkable split button also has the toggle pattern (as WPF's).
/// </summary>
public class RibbonSplitButtonAutomationPeer : RibbonMenuButtonAutomationPeer, IInvokeProvider, IToggleProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonSplitButtonAutomationPeer"/> class.</summary>
    /// <param name="owner">The split button.</param>
    public RibbonSplitButtonAutomationPeer(RibbonSplitButton owner)
        : base(owner)
    {
    }

    /// <summary>The split button.</summary>
    public new RibbonSplitButton Owner => (RibbonSplitButton)base.Owner;

    /// <inheritdoc/>
    public ToggleState ToggleState => Owner.IsChecked ? ToggleState.On : ToggleState.Off;

    /// <inheritdoc/>
    public void Invoke()
    {
        EnsureEnabled();
        Owner.PerformClick();
    }

    /// <inheritdoc/>
    public void Toggle()
    {
        EnsureEnabled();
        if (Owner.IsCheckable)
        {
            Owner.PerformClick();
        }
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.SplitButton;

    /// <inheritdoc/>
    protected override object? GetProviderCore(Type providerType) =>
        providerType == typeof(IToggleProvider) && !Owner.IsCheckable ? null : base.GetProviderCore(providerType);
}
