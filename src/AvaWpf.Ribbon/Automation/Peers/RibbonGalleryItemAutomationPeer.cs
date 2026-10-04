// Ported from WPF $R/Microsoft/Windows/Automation/Peers/RibbonGalleryItemAutomationPeer.cs and
// RibbonGalleryItemDataAutomationPeer.cs (MIT, see NOTICE.md).
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// Exposes a <see cref="RibbonGalleryItem"/> to UI Automation as a list item named by its content, with its KeyTip as
/// the access key. Selecting it selects it in the gallery, as a click does.
/// </summary>
public class RibbonGalleryItemAutomationPeer : ContentControlAutomationPeer, ISelectionItemProvider
{
    /// <summary>Initializes a new instance of the <see cref="RibbonGalleryItemAutomationPeer"/> class.</summary>
    /// <param name="owner">The gallery item.</param>
    public RibbonGalleryItemAutomationPeer(RibbonGalleryItem owner)
        : base(owner)
    {
    }

    /// <summary>The gallery item.</summary>
    public new RibbonGalleryItem Owner => (RibbonGalleryItem)base.Owner;

    /// <inheritdoc/>
    public bool IsSelected => Owner.IsSelected;

    /// <inheritdoc/>
    public ISelectionProvider? SelectionContainer =>
        Owner.Gallery is { } gallery ? GetOrCreate(gallery).GetProvider<ISelectionProvider>() : null;

    /// <inheritdoc/>
    public void Select()
    {
        EnsureEnabled();
        Owner.Gallery?.SelectFromUser(Owner);
    }

    /// <inheritdoc/>
    public void AddToSelection() => Select();

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        EnsureEnabled();
        if (Owner.IsSelected && Owner.Gallery is { } gallery)
        {
            gallery.SelectedItem = null;
        }
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonGalleryItem";

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => true;

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => true;

    /// <inheritdoc/>
    protected override string? GetAccessKeyCore() => RibbonAutomation.AccessKey(Owner, base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => RibbonAutomation.HelpText(Owner, base.GetHelpTextCore());
}
