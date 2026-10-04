using System.Collections.Generic;
using Avalonia.Media;

namespace AvaWpf.Explorer.ViewModels;

/// <summary>A label and value line of the Properties and About dialogs.</summary>
/// <param name="Label">The label ("Type:").</param>
/// <param name="Value">The value.</param>
public sealed record DialogRow(string Label, string Value);

/// <summary>The content of the in-window dialog (a WindowFrame of Kind Dialog over the shell).</summary>
/// <param name="Title">The caption.</param>
/// <param name="Icon">The 32 px icon beside the heading.</param>
/// <param name="Heading">The bold first line.</param>
/// <param name="Rows">The label and value lines.</param>
public sealed record DialogInfo(string Title, IImage Icon, string Heading, IReadOnlyList<DialogRow> Rows);
