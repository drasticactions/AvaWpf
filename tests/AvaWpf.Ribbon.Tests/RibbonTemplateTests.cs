using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaWpf.Ribbon.Primitives;
using Xunit;

namespace AvaWpf.Ribbon.Tests;

/// <summary>Every Ribbon type builds its template in every family.</summary>
public class RibbonTemplateTests
{
    public static readonly (Type Type, Func<Control> Create)[] Controls =
    [
        (typeof(Ribbon), () => RibbonSamples.ThreeGroupRibbon()),
        (typeof(RibbonTab), () => new RibbonTab { Header = "Tab", IsSelected = true, Items = { RibbonSamples.Group("Group", 2) } }),
        (typeof(RibbonTabHeader), () => new RibbonTabHeader { Content = "Home" }),
        (typeof(RibbonTabHeaderItemsControl), () => new RibbonTabHeaderItemsControl { Items = { new RibbonTabHeader { Content = "Home" } } }),
        (typeof(RibbonGroup), () => RibbonSamples.Group("Group", 3)),
        (typeof(RibbonControlGroup), () => new RibbonControlGroup { Items = { new RibbonToggleButton { Label = "B" }, new RibbonToggleButton { Label = "I" } } }),
        (typeof(RibbonSeparator), () => new RibbonSeparator { Label = "Section" }),
        (typeof(RibbonButton), () => RibbonSamples.Button("Paste")),
        (typeof(RibbonToggleButton), () => new RibbonToggleButton { Label = "Bold", LargeImageSource = RibbonSamples.Image() }),
        (typeof(RibbonRadioButton), () => new RibbonRadioButton { Label = "Left", SmallImageSource = RibbonSamples.Image(16) }),
        (typeof(RibbonCheckBox), () => new RibbonCheckBox { Label = "Ruler", IsChecked = true }),
        (typeof(RibbonSplitButton), () => new RibbonSplitButton { Label = "Paste", LargeImageSource = RibbonSamples.Image(), Items = { new RibbonMenuItem { Header = "Paste Special" } } }),
        (typeof(RibbonMenuButton), () => new RibbonMenuButton { Label = "Menu", Items = { new RibbonMenuItem { Header = "Item" } } }),
        (typeof(RibbonMenuItem), () => new RibbonMenuItem { Header = "Item", ImageSource = RibbonSamples.Image(16) }),
        (typeof(RibbonSplitMenuItem), () => new RibbonSplitMenuItem { Header = "Split", Items = { new RibbonMenuItem { Header = "Sub" } } }),
        (typeof(RibbonComboBox), () => new RibbonComboBox { Label = "Font", Text = "Segoe UI", Items = { new RibbonGallery() } }),
        (typeof(RibbonTextBox), () => new RibbonTextBox { Label = "Find", Text = "text" }),
        (typeof(RibbonGallery), () => new RibbonGallery { CanUserFilter = true, Items = { new RibbonGalleryCategory { Header = "Recent", Items = { new RibbonGalleryItem { Content = "A" } } } } }),
        (typeof(RibbonGalleryCategory), () => new RibbonGalleryCategory { Header = "Recent", Items = { new RibbonGalleryItem { Content = "A" } } }),
        (typeof(RibbonGalleryItem), () => new RibbonGalleryItem { Content = "A" }),
        (typeof(InRibbonGallery), () => new InRibbonGallery { Items = { new RibbonGallery { Items = { new RibbonGalleryCategory { Items = { new RibbonGalleryItem { Content = "A" } } } } } } }),
        (typeof(RibbonFilterMenuButton), () => new RibbonFilterMenuButton { Label = "All" }),
        (typeof(RibbonApplicationMenu), () => new RibbonApplicationMenu { SmallImageSource = RibbonSamples.Image(16), Items = { new RibbonApplicationMenuItem { Header = "New" } } }),
        (typeof(RibbonApplicationMenuItem), () => new RibbonApplicationMenuItem { Header = "Open", ImageSource = RibbonSamples.Image() }),
        (typeof(RibbonApplicationSplitMenuItem), () => new RibbonApplicationSplitMenuItem { Header = "Save As", Items = { new RibbonApplicationMenuItem { Header = "PDF" } } }),
        (typeof(RibbonQuickAccessToolBar), () => new RibbonQuickAccessToolBar { Items = { new RibbonButton { Label = "Save", SmallImageSource = RibbonSamples.Image(16) } } }),
        (typeof(RibbonContextualTabGroup), () => new RibbonContextualTabGroup { Header = "Picture Tools" }),
        (typeof(RibbonContextualTabGroupItemsControl), () => new RibbonContextualTabGroupItemsControl()),
        (typeof(RibbonCaptionQuickAccessToolBarHost), () => new RibbonCaptionQuickAccessToolBarHost { Content = new RibbonQuickAccessToolBar() }),
        (typeof(RibbonToolTip), () => new RibbonToolTip { Title = "Paste", Description = "Paste the clipboard.", FooterTitle = "Help" }),
        (typeof(RibbonTwoLineText), () => new RibbonTwoLineText { Text = "Two lines", HasTwoLines = true }),
        (typeof(KeyTipControl), () => new KeyTipControl { Text = "H" }),
        (typeof(RibbonContextMenu), () => new RibbonContextMenu()),
    ];

    private static readonly HashSet<Type> s_themeOnly = [typeof(RibbonContextMenu), typeof(RibbonToolTip)];

    public static IEnumerable<object[]> FamilyControls() =>
        from f in Enum.GetValues<ThemeFamily>()
        from c in Controls
        select new object[] { f, c.Type.Name };

    public static IEnumerable<object[]> VariantControls() =>
        from f in new[] { ThemeFamily.Aero2, ThemeFamily.Luna }
        from v in new[] { "Light", "Dark" }
        from c in Controls
        where !s_themeOnly.Contains(c.Type)
        select new object[] { f, v, c.Type.Name };

    private static (Type Type, Func<Control> Create) Control(string name) => Controls.Single(c => c.Type.Name == name);

    [AvaloniaTheory]
    [MemberData(nameof(FamilyControls))]
    public void Family_Has_ControlTheme(ThemeFamily family, string control)
    {
        var dictionary = AvaWpfRibbonTheme.LoadControls(family);
        Assert.True(dictionary.TryGetResource(Control(control).Type, null, out var theme), $"{family} has no Ribbon ControlTheme for {control}");
        Assert.IsType<ControlTheme>(theme);
    }

    [AvaloniaTheory]
    [MemberData(nameof(VariantControls))]
    public void Template_Builds(ThemeFamily family, string variant, string control)
    {
        var (type, create) = Control(control);
        var instance = create();
        var scope = new ThemeScope
        {
            Theme = family,
            RequestedThemeVariant = variant == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light,
            Child = instance,
        };
        var window = RibbonSamples.Show(scope, 900, 400);

        Assert.True(instance.TryFindResource(type, instance.ActualThemeVariant, out var theme) && theme is ControlTheme,
            $"{family} has no ControlTheme for {control}");
        var templated = (TemplatedControl)instance;
        Assert.True(templated.GetVisualChildren().Any(), $"{control} built an empty visual tree in {family}");
        Assert.NotNull(templated.Template);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(ThemeFamily.Aero2)]
    [InlineData(ThemeFamily.Luna)]
    [InlineData(ThemeFamily.Fluent)]
    public void Two_Line_Text_Does_Not_Clip_Descenders(ThemeFamily family)
    {
        // The labels use WPF's 13 px line height, shorter than the font's line; WPF draws the descenders past it.
        var text = new RibbonTwoLineText { Text = "Copy", LineHeight = 13 };
        var window = RibbonSamples.Show(new ThemeScope { Theme = family, Child = text }, 200, 100);

        Assert.False(text.ClipToBounds);
        Assert.All(text.GetVisualDescendants().OfType<TextBlock>(), t => Assert.False(t.ClipToBounds));
        window.Close();
    }

    [AvaloniaFact]
    public void Missing_AvaWpfTheme_Throws_A_Clear_Error()
    {
        var app = TestApplication.Instance;
        app.Styles.Remove(app.Theme);
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() => app.Styles.Add(new AvaWpfRibbonTheme()));
            Assert.Contains("AvaWpfTheme", ex.Message);
        }
        finally
        {
            app.Styles.Insert(0, app.Theme);
        }
    }
}
