using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AvaWpf.Animations;

namespace AvaWpf.Gallery;

public partial class MainView : UserControl
{
    private static readonly string[] s_variants = ["Light", "Dark", "HighContrast"];
    private bool _updating;

    public MainView()
    {
        InitializeComponent();
        BuildTree();
        BuildSwitchers();
        this.FindControl<MenuItem>("ExitItem")!.Click += (_, _) => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        this.FindControl<MenuItem>("LightItem")!.Click += (_, _) => SetVariant("Light");
        this.FindControl<MenuItem>("DarkItem")!.Click += (_, _) => SetVariant("Dark");
        this.FindControl<MenuItem>("ContrastItem")!.Click += (_, _) => SetVariant("HighContrast");
        this.FindControl<MenuItem>("AnimationsItem")!.Click += (s, _) => WpfAnimations.IsEnabled = ((MenuItem)s!).IsChecked;
        App.Theme.ThemeChanged += (_, _) => SyncSwitchers();
        ShowPage(InitialPage ?? GalleryPages.All[0].Name);
    }

    /// <summary>The page shown first (from <c>--page</c>).</summary>
    public static string? InitialPage { get; set; }

    private TreeView Tree => this.FindControl<TreeView>("PageTree")!;

    /// <summary>Shows a page by name.</summary>
    public void ShowPage(string name)
    {
        var page = GalleryPages.All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) ?? GalleryPages.All[0];
        this.FindControl<ContentControl>("PageHost")!.Content = page.Create();
        foreach (var group in Tree.Items.OfType<TreeViewItem>())
        {
            foreach (var item in group.Items.OfType<TreeViewItem>())
            {
                if (Equals(item.Tag, page.Name))
                {
                    _updating = true;
                    Tree.SelectedItem = item;
                    _updating = false;
                }
            }
        }

        UpdateStatus(page.Name);
    }

    private void BuildTree()
    {
        foreach (var group in GalleryPages.All.GroupBy(p => p.Group))
        {
            var node = new TreeViewItem { Header = group.Key, IsExpanded = true };
            foreach (var page in group)
            {
                node.Items.Add(new TreeViewItem { Header = page.Name, Tag = page.Name });
            }

            Tree.Items.Add(node);
        }

        Tree.SelectionChanged += (_, _) =>
        {
            if (!_updating && Tree.SelectedItem is TreeViewItem { Tag: string name })
            {
                ShowPage(name);
            }
        };
    }

    private void BuildSwitchers()
    {
        ThemeBox.ItemsSource = Enum.GetValues<ThemeFamily>();
        VariantBox.ItemsSource = s_variants;
        SyncSwitchers();
        ThemeBox.SelectionChanged += (_, _) =>
        {
            if (!_updating && ThemeBox.SelectedItem is ThemeFamily f)
            {
                App.Theme.ColorScheme = null;
                App.Theme.Theme = f;
                SyncSwitchers();
            }
        };
        SchemeBox.SelectionChanged += (_, _) =>
        {
            if (!_updating && SchemeBox.SelectedItem is string s)
            {
                App.Theme.ColorScheme = s;
                UpdateStatus(null);
            }
        };
        VariantBox.SelectionChanged += (_, _) =>
        {
            if (!_updating && VariantBox.SelectedItem is string v)
            {
                SetVariant(v);
            }
        };
    }

    private void SyncSwitchers()
    {
        _updating = true;
        ThemeBox.SelectedItem = App.Theme.Theme;
        SchemeBox.ItemsSource = ColorSchemes.For(App.Theme.Theme).ToList();
        SchemeBox.SelectedItem = ColorSchemes.Resolve(App.Theme.Theme, App.Theme.ColorScheme);
        var variant = Application.Current!.ActualThemeVariant;
        VariantBox.SelectedItem = WpfThemeVariants.IsHighContrast(variant) ? "HighContrast" : WpfThemeVariants.IsDark(variant) ? "Dark" : "Light";
        _updating = false;
        UpdateStatus(null);
    }

    private void SetVariant(string name)
    {
        Application.Current!.RequestedThemeVariant = name switch
        {
            "Dark" => ThemeVariant.Dark,
            "HighContrast" => WpfThemeVariants.HighContrast,
            _ => ThemeVariant.Light,
        };
        SyncSwitchers();
    }

    private string _page = string.Empty;

    private void UpdateStatus(string? page)
    {
        _page = page ?? _page;
        this.FindControl<TextBlock>("StatusText")!.Text =
            $"{_page}  |  {App.Theme.ActualTheme} {App.Theme.ActualColorScheme}  |  {Application.Current!.ActualThemeVariant}";
    }
}
