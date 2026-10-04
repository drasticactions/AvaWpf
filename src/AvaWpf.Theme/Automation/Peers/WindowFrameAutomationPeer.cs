using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace AvaWpf.Automation.Peers;

/// <summary>
/// Exposes a <see cref="WindowFrame"/> to UI Automation, with the title bar, the menu bar and the content as children.
/// </summary>
public class WindowFrameAutomationPeer : ContentControlAutomationPeer
{
    private WindowFrameTitleBarAutomationPeer? _titleBar;

    /// <summary>Initializes a new instance of the <see cref="WindowFrameAutomationPeer"/> class.</summary>
    /// <param name="owner">The window frame.</param>
    public WindowFrameAutomationPeer(WindowFrame owner)
        : base(owner)
    {
        owner.TemplateApplied += (_, _) =>
        {
            _titleBar = null;
            InvalidateChildren();
        };
    }

    /// <summary>The window frame.</summary>
    public new WindowFrame Owner => (WindowFrame)base.Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "WindowFrame";

    /// <inheritdoc/>
    protected override string? GetNameCore()
    {
        var name = Avalonia.Automation.AutomationProperties.GetName(Owner);
        return string.IsNullOrWhiteSpace(name) ? Owner.Title : name;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        var children = new List<AutomationPeer>();
        if (Owner.CaptionPart is { IsVisible: true } caption)
        {
            _titleBar ??= new WindowFrameTitleBarAutomationPeer(Owner, caption);
            children.Add(_titleBar);
        }

        if (Owner.MenuBar is Control { IsVisible: true } menuBar)
        {
            children.Add(GetOrCreate(menuBar));
        }

        var presenter = Owner.GetVisualDescendants().OfType<ContentPresenter>().FirstOrDefault(p => p.Name == "PART_ContentPresenter");
        if (presenter?.Child is { IsVisible: true } content)
        {
            children.Add(GetOrCreate(content));
        }

        return children;
    }
}
