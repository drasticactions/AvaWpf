// Ported from WPF $W/PresentationFramework/System/Windows/Controls/GridViewColumn.cs (MIT, see NOTICE.md).
namespace AvaWpf.Controls;

/// <summary>How far an auto-sized <see cref="GridViewColumn"/> has been measured.</summary>
internal enum ColumnMeasureState
{
    /// <summary>Not measured yet: the header and the rows will size it.</summary>
    Init = 0,

    /// <summary>The header has been measured; the rows still widen it.</summary>
    Headered = 1,

    /// <summary>The visible rows have been measured; the width is fixed until the column is auto-sized again.</summary>
    Data = 2,

    /// <summary>The column has an explicit <see cref="GridViewColumn.Width"/>.</summary>
    SpecificWidth = 3,
}
