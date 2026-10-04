using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaWpf.Ribbon;
using AvaWpf.Samples;

namespace AvaWpf.Gallery.Pages;

/// <summary>
/// The Ribbon: tabs, groups that collapse as the Ribbon narrows, split and menu buttons, an in-ribbon gallery, the
/// application menu, the Quick Access Toolbar and a contextual tab group; once on its own, once narrow, and once in a
/// frame caption as a RibbonWindow shows it.
/// </summary>
public class RibbonPage : UserControl
{
    public RibbonPage()
    {
        var wide = Create(out var tools);
        wide.Width = 980;
        wide.HorizontalAlignment = HorizontalAlignment.Left;

        var width = new Slider { Minimum = 320, Maximum = 980, Value = 980, Width = 260, HorizontalAlignment = HorizontalAlignment.Left };
        width.ValueChanged += (_, e) => wide.Width = e.NewValue;

        var contextual = new CheckBox { Content = "Show the _Picture Tools contextual tab group", IsChecked = true };
        contextual.IsCheckedChanged += (_, _) => tools.IsVisible = contextual.IsChecked == true;

        var narrow = Create(out _);
        narrow.Width = 470;
        narrow.HorizontalAlignment = HorizontalAlignment.Left;

        var frame = new WindowFrame
        {
            Title = "Document - WordPad",
            Icon = Icon("wordpad"),
            CanResize = false,
            Width = 980,
            Height = 250,
            HorizontalAlignment = HorizontalAlignment.Left,
            Content = new DockPanel
            {
                Children =
                {
                    DockTop(Create(out _)),
                    new TextBox { Text = SampleDocuments.Letter, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, BorderThickness = new Thickness(0) },
                },
            },
        };
        RibbonWindow.SetHostsRibbon(frame, true);

        Content = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock { Classes = { "Subtitle" }, Text = "Ribbon (Alt shows the KeyTips; right-click a control to add it to the Quick Access Toolbar)" },
                wide,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children = { new TextBlock { Text = "Ribbon width:", VerticalAlignment = VerticalAlignment.Center }, width },
                },
                contextual,
                new TextBlock { Classes = { "Subtitle" }, Text = "Narrow: the groups step down to small controls, then collapse to drop-down buttons" },
                narrow,
                new TextBlock { Classes = { "Subtitle" }, Text = "In a frame caption (RibbonWindow): the toolbar and the contextual header move into the caption" },
                frame,
            },
        };
    }

    private static Control DockTop(Control control)
    {
        DockPanel.SetDock(control, Avalonia.Controls.Dock.Top);
        return control;
    }

    private static IImage Icon(string name, int size = 16) => SampleImages.Load(name, size);

    private static RibbonButton Button(string label, string icon, string keyTip, bool large = false) => new()
    {
        Label = label,
        SmallImageSource = Icon(icon),
        LargeImageSource = large ? Icon(icon, 32) : null,
        KeyTip = keyTip,
        QuickAccessToolBarId = label,
        ToolTipTitle = label,
    };

    private static RibbonToggleButton Toggle(string icon, string keyTip, string tip) => new()
    {
        SmallImageSource = Icon(icon),
        KeyTip = keyTip,
        QuickAccessToolBarId = tip,
        ToolTipTitle = tip,
    };

    private static RibbonGalleryCategory Category(string header, params string[] items)
    {
        var category = new RibbonGalleryCategory { Header = header };
        foreach (var item in items)
        {
            category.Items.Add(new RibbonGalleryItem { Content = item, KeyTip = item[..1] });
        }

        return category;
    }

    /// <summary>A WordPad-like Ribbon. <paramref name="tools"/> is its Picture Tools contextual tab group.</summary>
    private static AvaWpf.Ribbon.Ribbon Create(out RibbonContextualTabGroup tools)
    {
        var ribbon = new AvaWpf.Ribbon.Ribbon
        {
            Title = "Document - WordPad",
            QuickAccessToolBar = new RibbonQuickAccessToolBar
            {
                Items =
                {
                    Button("Save", "save", "1"),
                    Button("Undo", "undo", "2"),
                    Button("Redo", "redo", "3"),
                },
            },
            ApplicationMenu = new RibbonApplicationMenu
            {
                SmallImageSource = Icon("wordpad"),
                KeyTip = "F",
                Items =
                {
                    new RibbonApplicationMenuItem { Header = "_New", ImageSource = Icon("new", 32), KeyTip = "N" },
                    new RibbonApplicationMenuItem { Header = "_Open", ImageSource = Icon("folder-open", 32), KeyTip = "O" },
                    new RibbonApplicationMenuItem
                    {
                        Header = "Save _as",
                        ImageSource = Icon("save-as", 32),
                        KeyTip = "A",
                        Items =
                        {
                            new RibbonApplicationMenuItem { Header = "Rich Text document", KeyTip = "R" },
                            new RibbonApplicationMenuItem { Header = "Plain text document", KeyTip = "T" },
                        },
                    },
                    new RibbonApplicationMenuItem { Header = "_Print", ImageSource = Icon("print", 32), KeyTip = "P" },
                    new RibbonApplicationMenuItem { Header = "E_xit", ImageSource = Icon("exit", 32), KeyTip = "X" },
                },
                AuxiliaryPaneContent = new StackPanel
                {
                    Margin = new Thickness(6, 4),
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = "Recent documents", FontWeight = FontWeight.Bold },
                        new TextBlock { Text = "1  Letter to Grandma.rtf" },
                        new TextBlock { Text = "2  Pangram.txt" },
                    },
                },
            },
        };

        tools = new RibbonContextualTabGroup { Header = "Picture Tools", Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xB0, 0x30)) };
        ribbon.ContextualTabGroups.Add(tools);

        // Home
        var home = new RibbonTab { Header = "Home", KeyTip = "H" };
        var paste = new RibbonSplitButton
        {
            Label = "Paste",
            LargeImageSource = Icon("paste", 32),
            SmallImageSource = Icon("paste"),
            KeyTip = "V",
            ToolTipTitle = "Paste",
            Items =
            {
                new RibbonMenuItem { Header = "_Paste", ImageSource = Icon("paste"), KeyTip = "P" },
                new RibbonMenuItem { Header = "Paste _special", KeyTip = "S" },
            },
        };
        home.Items.Add(new RibbonGroup
        {
            Header = "Clipboard",
            KeyTip = "ZC",
            LargeImageSource = Icon("paste", 32),
            Items = { paste, Button("Cut", "cut", "X"), Button("Copy", "copy", "C") },
        });

        var font = new RibbonComboBox { KeyTip = "FF", SelectionBoxWidth = 110, Text = "Segoe UI", Items = { new RibbonGallery { MaxColumnCount = 1, Items = { Category("Fonts", "Segoe UI", "Tahoma", "Verdana", "Times New Roman") } } } };
        var size = new RibbonComboBox { KeyTip = "FS", SelectionBoxWidth = 30, Text = "11", Items = { new RibbonGallery { MaxColumnCount = 1, Items = { Category("Sizes", "8", "10", "11", "12", "14", "18") } } } };
        home.Items.Add(new RibbonGroup
        {
            Header = "Font",
            KeyTip = "ZF",
            LargeImageSource = Icon("bold", 32),
            Items =
            {
                font,
                size,
                new RibbonControlGroup { Items = { Toggle("bold", "1", "Bold"), Toggle("italic", "2", "Italic"), Toggle("underline", "3", "Underline") } },
            },
        });

        home.Items.Add(new RibbonGroup
        {
            Header = "Paragraph",
            KeyTip = "ZP",
            LargeImageSource = Icon("align-left", 32),
            Items =
            {
                new RibbonControlGroup
                {
                    Items =
                    {
                        new RibbonRadioButton { GroupName = "align", IsChecked = true, SmallImageSource = Icon("align-left"), KeyTip = "AL", ToolTipTitle = "Align left" },
                        new RibbonRadioButton { GroupName = "align", SmallImageSource = Icon("align-center"), KeyTip = "AE", ToolTipTitle = "Center" },
                        new RibbonRadioButton { GroupName = "align", SmallImageSource = Icon("align-right"), KeyTip = "AR", ToolTipTitle = "Align right" },
                    },
                },
                new RibbonToggleButton { Label = "Bullets", SmallImageSource = Icon("bullets"), KeyTip = "U" },
            },
        });

        home.Items.Add(new RibbonGroup
        {
            Header = "Styles",
            KeyTip = "ZS",
            LargeImageSource = Icon("text", 32),
            Items =
            {
                new InRibbonGallery
                {
                    Label = "Styles",
                    KeyTip = "L",
                    SmallImageSource = Icon("text"),
                    MinColumnCount = 2,
                    MaxColumnCount = 4,
                    Items =
                    {
                        new RibbonGallery
                        {
                            Items = { Category("Paragraph styles", "Normal", "Heading 1", "Heading 2", "Title", "Quote", "Strong", "Caption", "List") },
                        },
                    },
                },
            },
        });

        home.Items.Add(new RibbonGroup
        {
            Header = "Editing",
            KeyTip = "ZE",
            LargeImageSource = Icon("find", 32),
            Items =
            {
                Button("Find", "find", "FD"),
                Button("Replace", "replace", "R"),
                Button("Select all", "select-all", "A"),
            },
        });

        // Insert
        var insert = new RibbonTab { Header = "Insert", KeyTip = "N" };
        insert.Items.Add(new RibbonGroup
        {
            Header = "Insert",
            KeyTip = "ZI",
            Items =
            {
                Button("Picture", "picture", "P", large: true),
                new RibbonMenuButton
                {
                    Label = "Date and time",
                    LargeImageSource = Icon("text", 32),
                    SmallImageSource = Icon("text"),
                    KeyTip = "D",
                    Items =
                    {
                        new RibbonMenuItem { Header = "10/4/2026", KeyTip = "1" },
                        new RibbonMenuItem { Header = "Sunday, October 4, 2026", KeyTip = "2" },
                    },
                },
            },
        });

        // View
        var view = new RibbonTab { Header = "View", KeyTip = "V" };
        view.Items.Add(new RibbonGroup
        {
            Header = "Zoom",
            KeyTip = "ZZ",
            Items = { Button("Zoom in", "zoom-in", "I", large: true), Button("Zoom out", "zoom-out", "O", large: true) },
        });
        view.Items.Add(new RibbonGroup
        {
            Header = "Show or hide",
            KeyTip = "ZH",
            Items =
            {
                new RibbonCheckBox { Label = "Ruler", IsChecked = true, KeyTip = "R" },
                new RibbonCheckBox { Label = "Status bar", IsChecked = true, KeyTip = "S" },
            },
        });

        // Picture Tools: Format
        var format = new RibbonTab { Header = "Format", KeyTip = "JP", ContextualTabGroupHeader = "Picture Tools" };
        format.Items.Add(new RibbonGroup
        {
            Header = "Arrange",
            KeyTip = "ZA",
            Items = { Button("Rotate", "rotate", "R", large: true), Button("Crop", "crop", "C", large: true) },
        });

        ribbon.Items.Add(home);
        ribbon.Items.Add(insert);
        ribbon.Items.Add(view);
        ribbon.Items.Add(format);
        return ribbon;
    }
}
