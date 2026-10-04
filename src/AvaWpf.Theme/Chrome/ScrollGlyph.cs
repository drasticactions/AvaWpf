namespace AvaWpf.Chrome;

/// <summary>The glyph a ScrollChrome draws, as WPF's <c>Microsoft.Windows.Themes.ScrollGlyph</c>.</summary>
public enum ScrollGlyph
{
    /// <summary>No glyph.</summary>
    None,

    /// <summary>A left arrow.</summary>
    LeftArrow,

    /// <summary>A right arrow.</summary>
    RightArrow,

    /// <summary>An up arrow.</summary>
    UpArrow,

    /// <summary>A down arrow.</summary>
    DownArrow,

    /// <summary>The gripper of a vertical thumb.</summary>
    VerticalGripper,

    /// <summary>The gripper of a horizontal thumb.</summary>
    HorizontalGripper,
}
