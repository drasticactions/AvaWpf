// Ported from WPF $R/Microsoft/Windows/Controls/KeyTipControl.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls.Primitives;

namespace AvaWpf.Ribbon;

/// <summary>The small label that shows an element's KeyTip while KeyTip mode is on.</summary>
public class KeyTipControl : TemplatedControl
{
    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<KeyTipControl, string?>(nameof(Text));

    /// <summary>The KeyTip text.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
