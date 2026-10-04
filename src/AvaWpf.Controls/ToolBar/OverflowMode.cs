// Ported from WPF $W/PresentationFramework/System/Windows/Controls/ToolBar.cs (MIT, see NOTICE.md).
namespace AvaWpf.Controls;

/// <summary>Where a <see cref="ToolBar"/> item is placed: in the main bar, in the overflow popup, or as space allows.</summary>
public enum OverflowMode
{
    /// <summary>The item moves to the overflow popup when the main bar has no room for it (default).</summary>
    AsNeeded,

    /// <summary>The item is always in the overflow popup.</summary>
    Always,

    /// <summary>The item is never in the overflow popup.</summary>
    Never,
}
