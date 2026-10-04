namespace AvaWpf;

/// <summary>The kind of window a <see cref="WindowFrame"/> draws.</summary>
public enum WindowFrameKind
{
    /// <summary>A main window: full caption, minimize, maximize and close.</summary>
    Normal,

    /// <summary>A tool window: the small caption (<c>SystemParameters.SmallCaptionHeight</c>, the small caption font).</summary>
    Tool,

    /// <summary>A dialog: no minimize or maximize button.</summary>
    Dialog,
}
