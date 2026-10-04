using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace AvaWpf.Automation.Peers;

/// <summary>
/// Exposes the caption bar of a <see cref="WindowFrame"/> as a <see cref="AutomationControlType.TitleBar"/> named by the
/// frame's title, with the visible caption buttons as children.
/// </summary>
public class WindowFrameTitleBarAutomationPeer : ControlAutomationPeer
{
    /// <summary>Initializes a new instance of the <see cref="WindowFrameTitleBarAutomationPeer"/> class.</summary>
    /// <param name="frame">The window frame.</param>
    /// <param name="caption">The caption bar part of its template.</param>
    public WindowFrameTitleBarAutomationPeer(WindowFrame frame, Control caption)
        : base(caption)
    {
        Frame = frame;
        frame.PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowFrame.IsMinimizeButtonVisibleProperty || e.Property == WindowFrame.IsMaximizeButtonVisibleProperty ||
                e.Property == WindowFrame.WindowStateProperty)
            {
                InvalidateChildren();
            }
        };
    }

    /// <summary>The window frame.</summary>
    public WindowFrame Frame { get; }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TitleBar;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "TitleBar";

    /// <inheritdoc/>
    protected override string? GetNameCore() => Frame.Title;

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => false;

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        var children = new List<AutomationPeer>(3);
        foreach (var button in Frame.VisibleCaptionButtons)
        {
            children.Add(GetOrCreate(button));
        }

        return children;
    }
}
