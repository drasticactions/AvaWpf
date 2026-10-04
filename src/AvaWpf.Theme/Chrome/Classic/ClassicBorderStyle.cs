// Ported from WPF $W/Themes/PresentationFramework.Classic/Microsoft/Windows/Themes/ClassicBorderDecorator.cs (MIT, see NOTICE.md).
namespace AvaWpf.Chrome.Classic;

/// <summary>The kind of border a <see cref="ClassicBorderDecorator"/> draws.</summary>
public enum ClassicBorderStyle
{
    /// <summary>No classic border.</summary>
    None,

    /// <summary>A normal button.</summary>
    Raised,

    /// <summary>A pressed button.</summary>
    RaisedPressed,

    /// <summary>A focused or defaulted button.</summary>
    RaisedFocused,

    /// <summary>A ListBox, TextBox, CheckBox and similar.</summary>
    Sunken,

    /// <summary>A GroupBox.</summary>
    Etched,

    /// <summary>A horizontal separator.</summary>
    HorizontalLine,

    /// <summary>A vertical separator.</summary>
    VerticalLine,

    /// <summary>A tab on the right of a TabControl.</summary>
    TabRight,

    /// <summary>A tab on the top of a TabControl.</summary>
    TabTop,

    /// <summary>A tab on the left of a TabControl.</summary>
    TabLeft,

    /// <summary>A tab on the bottom of a TabControl.</summary>
    TabBottom,

    /// <summary>A top-level MenuItem or ToolBar button in the hover state.</summary>
    ThinRaised,

    /// <summary>A top-level MenuItem or ToolBar button in the pressed state.</summary>
    ThinPressed,

    /// <summary>A ScrollBar button.</summary>
    AltRaised,

    /// <summary>A pressed ScrollBar button.</summary>
    AltPressed,

    /// <summary>A RadioButton.</summary>
    RadioButton,
}
