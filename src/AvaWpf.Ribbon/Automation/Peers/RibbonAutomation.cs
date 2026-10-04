using Avalonia.Automation;
using Avalonia.Controls;

namespace AvaWpf.Ribbon.Automation.Peers;

/// <summary>
/// What every Ribbon peer reports the way WPF's Ribbon peers do: the name from <c>Label</c>, the access key from the
/// KeyTip and the help text from the rich tool tip.
/// </summary>
internal static class RibbonAutomation
{
    /// <summary>The automation name: an explicit <see cref="AutomationProperties.NameProperty"/>, else the label, else <paramref name="fallback"/>.</summary>
    public static string? Name(Control owner, string? fallback)
    {
        var name = AutomationProperties.GetName(owner);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var label = RibbonControlService.GetLabel(owner);
        return string.IsNullOrWhiteSpace(label) ? fallback : label;
    }

    /// <summary>The access key: the KeyTip (pressed after Alt), else <paramref name="fallback"/>.</summary>
    public static string? AccessKey(Control owner, string? fallback) =>
        KeyTipService.GetKeyTip(owner) is { Length: > 0 } keyTip ? keyTip : fallback;

    /// <summary>The help text: an explicit one, else the tool tip description, else the tool tip title.</summary>
    public static string? HelpText(Control owner, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        var description = RibbonControlService.GetToolTipDescription(owner);
        return string.IsNullOrWhiteSpace(description) ? RibbonControlService.GetToolTipTitle(owner) : description;
    }
}
