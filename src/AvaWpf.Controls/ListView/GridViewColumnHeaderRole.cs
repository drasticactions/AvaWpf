// Ported from WPF $W/PresentationFramework/System/Windows/Controls/GridViewColumnHeader.cs (MIT, see NOTICE.md).
namespace AvaWpf.Controls;

/// <summary>The role of a <see cref="GridViewColumnHeader"/> in the header row.</summary>
public enum GridViewColumnHeaderRole
{
    /// <summary>The header of a column.</summary>
    Normal,

    /// <summary>The translucent copy of a header that follows the pointer while a column is dragged.</summary>
    Floating,

    /// <summary>The header that fills the row after the last column.</summary>
    Padding,
}
