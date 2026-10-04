// Ported from WPF $R/Microsoft/Windows/Controls/KeyTipService.cs (MIT, see NOTICE.md).
using System.Collections.Generic;
using Avalonia.Controls;

namespace AvaWpf.Ribbon;

/// <summary>
/// A KeyTip scope whose template holds elements with KeyTips that belong to the enclosing scope (the header half of a
/// split button), as WPF's <c>KeyTipService.CustomSiblingKeyTipElements</c>.
/// </summary>
internal interface IKeyTipSiblingProvider
{
    /// <summary>The elements whose KeyTips show beside the provider's own.</summary>
    IEnumerable<Control> KeyTipSiblings { get; }
}
