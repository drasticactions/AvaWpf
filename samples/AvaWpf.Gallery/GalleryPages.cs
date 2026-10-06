using System;
using System.Collections.Generic;
using Avalonia.Controls;
using AvaWpf.Gallery.Pages;

namespace AvaWpf.Gallery;

/// <summary>One gallery page.</summary>
/// <param name="Name">The name shown in the tree and used by <c>--page</c>.</param>
/// <param name="Group">The tree group.</param>
/// <param name="Create">Creates the page.</param>
public sealed record GalleryPage(string Name, string Group, Func<Control> Create);

/// <summary>Every page of the gallery, in tree order.</summary>
public static class GalleryPages
{
    private static List<GalleryPage>? s_all;

    /// <summary>Creates the Reference page; only the desktop head sets it (the page reads local files).</summary>
    public static Func<Control>? ReferencePageFactory { get; set; }

    /// <summary>Pages registered by optional packages (DataGrid, Ribbon) before the shell starts.</summary>
    public static List<GalleryPage> ExtraPages { get; } = new();

    /// <summary>Every page, built on first use.</summary>
    public static IReadOnlyList<GalleryPage> All => s_all ??= Build();

    private static List<GalleryPage> Build()
    {
        var pages = new List<GalleryPage>
        {
            new("Buttons", "Controls", () => new ButtonsPage()),
            new("Toggles", "Controls", () => new TogglesPage()),
            new("Text", "Controls", () => new TextPage()),
            new("Lists", "Controls", () => new ListsPage()),
            new("Trees", "Controls", () => new TreesPage()),
            new("Tabs", "Controls", () => new TabsPage()),
            new("Menus", "Controls", () => new MenusPage()),
            new("Range", "Controls", () => new RangePage()),
            new("Scrolling", "Controls", () => new ScrollingPage()),
            new("Dates", "Controls", () => new DatesPage()),
            new("Containers", "Controls", () => new ContainersPage()),
            new("DataGrid", "Controls", () => new DataGridPage()),
            new("ToolBar", "Controls", () => new ToolBarPage()),
            new("StatusBar", "Controls", () => new StatusBarPage()),
            new("ListView", "Controls", () => new ListViewPage()),
            new("Ribbon", "Controls", () => new RibbonPage()),
            new("Avalonia only", "Controls", () => new AvaloniaOnlyPage()),
            new("Windows", "Theme", () => new WindowsPage()),
            new("Dialogs", "Theme", () => new DialogsPage()),
            new("Popups", "Theme", () => new PopupsPage()),
            new("Motion", "Theme", () => new MotionPage()),
            new("Typography", "Theme", () => new TypographyPage()),
            new("Tokens", "Theme", () => new TokensPage()),
            new("SystemColors", "Theme", () => new SystemColorsPage()),
            new("Compare", "Theme", () => new ComparePage()),
        };

        pages.AddRange(ExtraPages);
        if (ReferencePageFactory is { } reference)
        {
            pages.Add(new("Reference", "Theme", reference));
        }

        return pages;
    }
}
