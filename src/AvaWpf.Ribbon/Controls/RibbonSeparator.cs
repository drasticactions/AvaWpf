// Ported from WPF $R/Microsoft/Windows/Controls/Ribbon/RibbonSeparator.cs (MIT, see NOTICE.md).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace AvaWpf.Ribbon;

/// <summary>
/// A separator in a Ribbon menu or group; with a <see cref="Label"/> it is a menu section header.
/// </summary>
[PseudoClasses(":haslabel")]
public class RibbonSeparator : Separator
{
    /// <summary>Defines the <see cref="Label"/> property.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<RibbonSeparator, string?>(nameof(Label));

    /// <summary>The section header text.</summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LabelProperty)
        {
            PseudoClasses.Set(":haslabel", !string.IsNullOrEmpty(change.GetNewValue<string?>()));
        }
    }
}
