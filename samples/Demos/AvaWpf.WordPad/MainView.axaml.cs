using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using AvaloniaRichEditor.Controls;
using AvaloniaRichEditor.Documents;
using AvaWpf.Ribbon;
using AvaWpf.Samples;

namespace AvaWpf.WordPad;

/// <summary>
/// A paragraph style of the Styles gallery: a heading level for the caret paragraph (0 = body text). The size, weight
/// and style are the gallery preview of that level.
/// </summary>
public sealed record ParagraphStyle(string Name, int Heading, double Size, FontWeight Weight, FontStyle Style)
{
    /// <summary>The size of the "AaBb" sample in the gallery.</summary>
    public double PreviewSize => Math.Min(Size, 18);

    public override string ToString() => Name;
}

/// <summary>A theme of the View tab's Theme gallery.</summary>
public sealed record ThemeChoice(string Name, ThemeFamily Family, string? Scheme, bool Dark)
{
    public override string ToString() => Name;
}

/// <summary>A document of the application menu's recent list.</summary>
public sealed record RecentDocument(string Name, string Text, int Index)
{
    /// <summary>The label, numbered as WordPad numbers its recent documents.</summary>
    public string Label => $"{Index}  {Name}";

    /// <summary>The KeyTip: the number.</summary>
    public string KeyTip => Index.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// The WordPad shell. It is its own data context: the Ribbon's check boxes, galleries and the status bar bind to its
/// properties, and the Ribbon commands are click handlers that act on the document, an AvaloniaRichEditor
/// <see cref="RichEditor"/>: the formatting commands apply to the selection, and the Ribbon's toggles, alignment and
/// font boxes follow the caret.
/// </summary>
public partial class MainView : UserControl, INotifyPropertyChanged
{
    // RichEditor's body size: a run left at it takes a heading paragraph's size, any other size is kept.
    private const double BaseFontSize = 10;
    private static readonly double[] s_zoomSteps = [0.5, 0.75, 1, 1.25, 1.5, 2, 3];

    private string _documentName = "Document";
    private bool _showRuler = true;
    private bool _showStatusBar = true;
    private string _statusText = "Ready";
    private string _positionText = "Ln 1, Col 1";
    private int _zoomIndex = 2;
    private bool _syncing;

    public MainView()
    {
        InitializeComponent();
        DataContext = this;

        // The Ribbon is the toolbar; the view keeps its zoom host and scrolling. Without a target the view's own
        // toolbar hides itself (it resets IsVisible from its target).
        EditorView.Toolbar.Target = null;

        // RichEditorView floors the editor's height at the viewport and then adds a 12 px top and bottom margin, so a
        // short document still scrolls by 24 px and jumps on the first click. The editor's own inset keeps the text
        // off the edges.
        Editor.Margin = new Thickness(Editor.Margin.Left, 0, Editor.Margin.Right, 0);
        Editor.DefaultFontFamily = new FontFamily(DefaultFontName);
        Editor.DefaultFontSize = BaseFontSize;
        Editor.FontFamilyChoices = FontFamilies;
        _ = LoadFontsAsync();

        // Each font name is drawn in its own font, as in WordPad. The rows have one height, and each name is centered
        // in its row whatever its font's line spacing.
        FontFamilyGallery.GalleryItemTemplate = new FuncDataTemplate<string>(
            (name, _) => name is null ? null : new FontNameRow
            {
                Height = 20,
                Child = new TextBlock { Text = name, FontFamily = new FontFamily(name) },
            });
        LoadText(SampleDocuments.Letter);
        Editor.SelectionChanged += (_, _) =>
        {
            SyncRibbon();
            UpdatePosition();
        };
        Editor.StatusChanged += (_, _) => UpdatePosition();

        // RichEditor does not report which picture is selected, so the Picture Tools tab shows for the picture just
        // inserted and goes on the next press or key in the document.
        Editor.AddHandler(PointerPressedEvent, (_, _) => ShowPictureTools(false), handledEventsToo: true);
        Editor.AddHandler(KeyDownEvent, (_, _) => ShowPictureTools(false), handledEventsToo: true);
        StatusText = "Ready";
        KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.S, KeyModifiers.Control), Command = new Command(() => Save("Saved")) });
        KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.F, KeyModifiers.Control), Command = new Command(() => ShowFind(replace: false)) });
        KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.H, KeyModifiers.Control), Command = new Command(() => ShowFind(replace: true)) });
    }

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public new event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when <see cref="Title"/> changes.</summary>
    public event EventHandler? TitleChanged;

    /// <summary>The window title: the document name and the application name, as WordPad shows it.</summary>
    public string Title => $"{_documentName} - WordPad";

    /// <summary>The document font; WithAvaWpfFonts stands in for it where it is not installed.</summary>
    private const string DefaultFontName = "Segoe UI";

    /// <summary>The fonts installed on the machine and the document font, in name order; only the document font until
    /// <see cref="LoadFontsAsync"/> finishes.</summary>
    public IReadOnlyList<string> FontFamilies { get; private set; } = [DefaultFontName];

    /// <summary>Lists the installed fonts and loads each one off the UI thread, so the font gallery opens without
    /// loading font files.</summary>
    private async Task LoadFontsAsync()
    {
        var fontManager = FontManager.Current;
        List<string> names;
        try
        {
            names = await Task.Run(() =>
            {
                var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { DefaultFontName };
                foreach (var family in fontManager.SystemFonts)
                {
                    set.Add(family.Name);
                }

                foreach (var name in set)
                {
                    fontManager.TryGetGlyphTypeface(new Typeface(name), out _);
                }

                return set.ToList();
            });
        }
        catch (Exception)
        {
            // Without the system list the gallery keeps the document font.
            return;
        }

        FontFamilies = names;
        Editor.FontFamilyChoices = names;
        Raise(nameof(FontFamilies));
    }

    public IReadOnlyList<string> FontSizes { get; } =
        ["8", "9", "10", "11", "12", "14", "16", "18", "20", "24", "28", "36", "48", "72"];

    public IReadOnlyList<ParagraphStyle> ParagraphStyles { get; } =
    [
        new("Normal", 0, 10, FontWeight.Normal, FontStyle.Normal),
        new("Heading 1", 1, 20, FontWeight.Bold, FontStyle.Normal),
        new("Heading 2", 2, 16, FontWeight.Bold, FontStyle.Normal),
        new("Heading 3", 3, 14, FontWeight.Bold, FontStyle.Normal),
    ];

    public IReadOnlyList<ThemeChoice> LightThemes { get; } = Themes(dark: false);

    public IReadOnlyList<ThemeChoice> DarkThemes { get; } = Themes(dark: true);

    public IReadOnlyList<RecentDocument> RecentDocuments { get; } =
    [
        new("Letter to Grandma.rtf", SampleDocuments.Letter, 1),
        new("Pangram.txt", SampleDocuments.Pangram + "\n", 2),
        new("Shopping list.txt", "Milk\nBread\nApples\nCoffee\n", 3),
    ];

    public bool ShowRuler
    {
        get => _showRuler;
        set => Set(ref _showRuler, value);
    }

    public bool ShowStatusBar
    {
        get => _showStatusBar;
        set => Set(ref _showStatusBar, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => Set(ref _statusText, value);
    }

    public string PositionText
    {
        get => _positionText;
        private set => Set(ref _positionText, value);
    }

    public string ZoomText => $"{s_zoomSteps[_zoomIndex] * 100:0}%";

    private RichEditor Editor => EditorView.Editor;

    /// <summary>Inserts the sample picture at the end of the document: the Picture Tools tab shows.</summary>
    public void InsertSamplePicture()
    {
        SendShortcut(Key.End);
        InsertPicture(Pictures.Sample());
    }

    private static List<ThemeChoice> Themes(bool dark)
    {
        var suffix = dark ? " Dark" : string.Empty;
        return
        [
            new("Aero2" + suffix, ThemeFamily.Aero2, null, dark),
            new("AeroLite" + suffix, ThemeFamily.AeroLite, null, dark),
            new("Aero" + suffix, ThemeFamily.Aero, null, dark),
            new("Luna Blue" + suffix, ThemeFamily.Luna, "NormalColor", dark),
            new("Luna Silver" + suffix, ThemeFamily.Luna, "Metallic", dark),
            new("Luna Olive" + suffix, ThemeFamily.Luna, "Homestead", dark),
            new("Royale" + suffix, ThemeFamily.Royale, null, dark),
            new("Classic" + suffix, ThemeFamily.Classic, null, dark),
            new("Fluent" + suffix, ThemeFamily.Fluent, null, dark),
        ];
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void SetDocumentName(string name)
    {
        _documentName = name;
        Raise(nameof(Title));
        TitleChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdatePosition()
    {
        var (_, _, line, column) = Editor.GetStatus();
        PositionText = $"Ln {line}, Col {column}";
    }

    /// <summary>
    /// Loads plain text, one paragraph per line, in the document font. The runs carry the font but not a size: they
    /// stay at RichEditor's body size, the only size a heading style enlarges.
    /// </summary>
    private void LoadText(string text)
    {
        var html = new System.Text.StringBuilder();
        var style = $"font-family:'{DefaultFontName}'";
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            html.Append("<p><span style=\"").Append(style).Append("\">").Append(System.Net.WebUtility.HtmlEncode(line)).Append("</span></p>");
        }

        Editor.LoadHtml(html.ToString());
        Editor.MarkSaved();
        SyncRibbon();
        UpdatePosition();
    }

    /// <summary>Shows the caret's formatting on the Ribbon: the toggles, the alignment, the font and size boxes.</summary>
    private void SyncRibbon()
    {
        var format = Editor.GetCaretFormat();
        var style = ParagraphStyles.FirstOrDefault(p => p.Heading == format.Heading);

        // RichEditor 1.2 draws a heading's runs bold, and a run left at the body size at the heading's size.
        bool heading = format.Heading > 0;
        double size = heading && style != null && format.FontSize == BaseFontSize ? style.Size : format.FontSize;
        _syncing = true;
        try
        {
            BoldToggle.IsChecked = format.Bold || heading;
            ItalicToggle.IsChecked = format.Italic;
            UnderlineToggle.IsChecked = format.Underline;
            AlignCenter.IsChecked = format.Align == TextAlignment.Center;
            AlignRight.IsChecked = format.Align == TextAlignment.Right;
            AlignLeft.IsChecked = format.Align is not (TextAlignment.Center or TextAlignment.Right);
            BulletsToggle.IsChecked = format.List == ListKind.Bullet;
            FontFamilyBox.Text = format.FontFamily ?? DefaultFontName;
            FontSizeBox.Text = size.ToString("0.#", CultureInfo.InvariantCulture);
            StylesGallery.SelectedItem = style ?? ParagraphStyles[0];
        }
        finally
        {
            _syncing = false;
        }
    }

    // Clipboard

    private async void OnPaste(object? sender, RoutedEventArgs e)
    {
        Editor.Focus();
        await Editor.PasteFromClipboardAsync();
    }

    private void OnCut(object? sender, RoutedEventArgs e) => SendShortcut(Key.X);

    private void OnCopy(object? sender, RoutedEventArgs e) => SendShortcut(Key.C);

    /// <summary>
    /// Runs one of the editor's clipboard or selection shortcuts (Ctrl+X, Ctrl+C, Ctrl+A): RichEditor 1.2 handles Cut,
    /// Copy and Select all only from the keyboard.
    /// </summary>
    private void SendShortcut(Key key)
    {
        Editor.Focus();
        Editor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = KeyModifiers.Control, Source = Editor });
    }

    // Font

    private void OnFontFamilyChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_syncing && FontFamilyGallery.SelectedItem is string name)
        {
            Editor.SetFontFamily(name);
            Editor.Focus();
        }
    }

    private void OnFontSizeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_syncing && FontSizeGallery.SelectedItem is string size && double.TryParse(size, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            Editor.SetFontSize(value);
            Editor.Focus();
        }
    }

    // The toggles act when the user flips them, not when SyncRibbon shows the caret's format.
    private void OnFormatChanged(object? sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        if (sender == BoldToggle)
        {
            Editor.ToggleBold();
        }
        else if (sender == ItalicToggle)
        {
            Editor.ToggleItalic();
        }
        else if (sender == UnderlineToggle)
        {
            Editor.ToggleUnderline();
        }

        Editor.Focus();
    }

    // Paragraph

    private void OnAlignChanged(object? sender, RoutedEventArgs e)
    {
        if (_syncing || sender is not RadioButton { IsChecked: true } button)
        {
            return;
        }

        // The event fires before the group unchecks the previous button, so read the sender, not the group.
        Editor.SetTextAlignment(button == AlignCenter ? TextAlignment.Center
            : button == AlignRight ? TextAlignment.Right
            : TextAlignment.Left);
        Editor.Focus();
    }

    private void OnBulletsChanged(object? sender, RoutedEventArgs e)
    {
        if (!_syncing)
        {
            Editor.ToggleBullet();
            Editor.Focus();
        }
    }

    // Styles

    private void OnStyleChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_syncing && StylesGallery.SelectedItem is ParagraphStyle style)
        {
            Editor.SetHeading(style.Heading);
            SyncRibbon();
            Editor.Focus();
            StatusText = $"Style: {style.Name}";
        }
    }

    // Insert and Picture Tools

    private async void OnInsertPicture(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this)?.StorageProvider is not { CanOpen: true } storage)
        {
            return;
        }

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Insert picture",
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
        });
        if (files.Count == 0)
        {
            return;
        }

        await using var stream = await files[0].OpenReadAsync();
        using var bytes = new MemoryStream();
        await stream.CopyToAsync(bytes);
        InsertPicture(bytes.ToArray());
    }

    /// <summary>Inserts a picture after the caret's paragraph and shows the Picture Tools tab, as WordPad does.</summary>
    private void InsertPicture(byte[] picture)
    {
        var count = Editor.GetImageCount();
        Editor.InsertImageBytes(picture);
        if (Editor.GetImageCount() > count)
        {
            ShowPictureTools(true);
        }
    }

    private void ShowPictureTools(bool show)
    {
        if (FormatTab.IsVisible == show)
        {
            return;
        }

        if (show)
        {
            PictureTools.IsVisible = FormatTab.IsVisible = true;
            Ribbon.SelectedItem = FormatTab;
            return;
        }

        if (Ribbon.SelectedItem == FormatTab)
        {
            Ribbon.SelectedItem = HomeTab;
        }

        PictureTools.IsVisible = FormatTab.IsVisible = false;
    }

    // Editing

    private void OnFind(object? sender, RoutedEventArgs e) => ShowFind(replace: false);

    private void OnReplace(object? sender, RoutedEventArgs e) => ShowFind(replace: true);

    private void OnSelectAll(object? sender, RoutedEventArgs e) => SendShortcut(Key.A);

    private void ShowFind(bool replace)
    {
        FindBar.IsVisible = true;
        ReplaceLabel.IsVisible = replace;
        ReplaceText.IsVisible = replace;
        ReplaceAllButton.IsVisible = replace;
        FindText.Focus();
    }

    private void OnFindNext(object? sender, RoutedEventArgs e)
    {
        var what = FindText.Text;
        if (string.IsNullOrEmpty(what))
        {
            return;
        }

        StatusText = Editor.FindNext(what, matchCase: false) ? "Ready" : $"Cannot find \"{what}\"";
    }

    private void OnReplaceAll(object? sender, RoutedEventArgs e)
    {
        var what = FindText.Text;
        if (!string.IsNullOrEmpty(what))
        {
            var count = Editor.ReplaceAll(what, ReplaceText.Text ?? string.Empty, matchCase: false);
            StatusText = $"Replaced {count} occurrence(s)";
        }
    }

    private void OnCloseFind(object? sender, RoutedEventArgs e) => FindBar.IsVisible = false;

    // View

    private void OnZoomIn(object? sender, RoutedEventArgs e) => Zoom(_zoomIndex + 1);

    private void OnZoomOut(object? sender, RoutedEventArgs e) => Zoom(_zoomIndex - 1);

    private void OnZoomReset(object? sender, RoutedEventArgs e) => Zoom(2);

    private void Zoom(int index)
    {
        _zoomIndex = Math.Clamp(index, 0, s_zoomSteps.Length - 1);
        EditorView.FitToWidth = false;

        // The view floors the editor's height at the viewport in unzoomed pixels, but only when the viewport changes:
        // rescale the floor with the zoom, or a short document scrolls when zoomed in.
        double oldZoom = EditorView.ZoomFactor > 0 ? EditorView.ZoomFactor : 1;
        EditorView.ZoomFactor = s_zoomSteps[_zoomIndex];
        Editor.MinHeight *= oldZoom / EditorView.ZoomFactor;
        Raise(nameof(ZoomText));
    }

    private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not RibbonGallery { SelectedItem: ThemeChoice choice })
        {
            return;
        }

        var theme = App.Theme;
        if (theme.Theme != choice.Family)
        {
            // The current scheme may not exist in the new family.
            theme.ColorScheme = null;
        }

        theme.Theme = choice.Family;
        theme.ColorScheme = choice.Scheme;
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = choice.Dark ? ThemeVariant.Dark : ThemeVariant.Light;
        }

        StatusText = $"Theme: {choice.Name}";
    }

    // Quick Access Toolbar and application menu

    private void OnUndo(object? sender, RoutedEventArgs e) => Editor.Undo();

    private void OnRedo(object? sender, RoutedEventArgs e) => Editor.Redo();

    private void OnNew(object? sender, RoutedEventArgs e)
    {
        LoadText(string.Empty);
        SetDocumentName("Document");
        StatusText = "New document";
    }

    private void OnOpen(object? sender, RoutedEventArgs e) => Open(RecentDocuments[0]);

    private void OnOpenRecent(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: RecentDocument document })
        {
            Open(document);
        }
    }

    private void Open(RecentDocument document)
    {
        LoadText(document.Text);
        SetDocumentName(document.Name);
        StatusText = $"Opened {document.Name}";
        Ribbon.ApplicationMenu?.SetCurrentValue(RibbonMenuButton.IsDropDownOpenProperty, false);
    }

    private void OnSave(object? sender, RoutedEventArgs e) => Save("Saved");

    private void OnSaveAs(object? sender, RoutedEventArgs e)
    {
        if (_documentName == "Document")
        {
            SetDocumentName("Document.rtf");
        }

        Save("Saved as " + _documentName);
    }

    private void Save(string status)
    {
        if (_documentName == "Document")
        {
            SetDocumentName("Document.rtf");
        }

        StatusText = status;
    }

    private void OnPrint(object? sender, RoutedEventArgs e) => StatusText = "Sent to the printer";

    private void OnAbout(object? sender, RoutedEventArgs e) => AboutBox.IsVisible = true;

    private void OnCloseAbout(object? sender, RoutedEventArgs e) => AboutBox.IsVisible = false;

    private void OnExit(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Close();
        }
    }

    /// <summary>A command for the keyboard shortcuts.</summary>
    private sealed class Command(Action execute) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }
}
