using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Dialogs;
using Avalonia.Media;

namespace AvaWpf.Theme.Tests;

/// <summary>Every control Avalonia 12.1 Fluent ships a ControlTheme for, with a factory for a sample instance.</summary>
internal static class ThemedControls
{
    /// <summary>Types whose template is only checked for a theme, not built (top levels and hosts).</summary>
    public static readonly HashSet<Type> ThemeOnly = new()
    {
        typeof(Window), typeof(PopupRoot), typeof(OverlayPopupHost), typeof(EmbeddableControlRoot), typeof(AdornerLayer),
        typeof(WindowDrawnDecorations), typeof(ManagedFileChooser), typeof(ManagedFileChooserOverwritePrompt),
        typeof(DatePickerPresenter), typeof(TimePickerPresenter), typeof(TextSelectionHandle),
        typeof(WindowNotificationManager),
    };

    public static readonly (Type Type, Func<Control> Create)[] All =
    [
        (typeof(AdornerLayer), () => new Border()),
        (typeof(AutoCompleteBox), () => new AutoCompleteBox { ItemsSource = new[] { "Alpha", "Beta" } }),
        (typeof(Button), () => new Button { Content = "Button" }),
        (typeof(ButtonSpinner), () => new ButtonSpinner { Content = "1" }),
        (typeof(Calendar), () => new Calendar()),
        (typeof(CalendarButton), () => new CalendarButton { Content = "Jan" }),
        (typeof(CalendarDatePicker), () => new CalendarDatePicker()),
        (typeof(CalendarDayButton), () => new CalendarDayButton { Content = "1" }),
        (typeof(CalendarItem), () => new CalendarItem()),
        (typeof(Carousel), () => new Carousel { ItemsSource = new[] { "One", "Two" } }),
        (typeof(CarouselPage), () => new CarouselPage()),
        (typeof(CheckBox), () => new CheckBox { Content = "Check" }),
        (typeof(ComboBox), () => new ComboBox { ItemsSource = new[] { "One", "Two" }, SelectedIndex = 0 }),
        (typeof(ComboBoxItem), () => new ComboBoxItem { Content = "Item" }),
        (typeof(CommandBar), () => new CommandBar()),
        (typeof(CommandBarButton), () => new CommandBarButton { Label = "Cut" }),
        (typeof(CommandBarSeparator), () => new CommandBarSeparator()),
        (typeof(CommandBarToggleButton), () => new CommandBarToggleButton { Label = "Bold" }),
        (typeof(ContentPage), () => new ContentPage { Content = "Page" }),
        (typeof(ContextMenu), () => new ContextMenu { ItemsSource = new[] { new MenuItem { Header = "Copy" } } }),
        (typeof(DataValidationErrors), () => new DataValidationErrors { Content = new TextBox() }),
        (typeof(DatePicker), () => new DatePicker()),
        (typeof(DatePickerPresenter), () => new DatePickerPresenter()),
        (typeof(DrawerPage), () => new DrawerPage()),
        (typeof(DropDownButton), () => new DropDownButton { Content = "Drop" }),
        (typeof(EmbeddableControlRoot), () => new Border()),
        (typeof(Expander), () => new Expander { Header = "Header", Content = "Content", IsExpanded = true }),
        (typeof(FlyoutPresenter), () => new FlyoutPresenter { Content = "Flyout" }),
        (typeof(GridSplitter), () => new GridSplitter()),
        (typeof(GroupBox), () => new GroupBox { Header = "Group", Content = "Content" }),
        (typeof(HeaderedContentControl), () => new HeaderedContentControl { Header = "Header", Content = "Content" }),
        (typeof(HyperlinkButton), () => new HyperlinkButton { Content = "Link" }),
        (typeof(ItemsControl), () => new ItemsControl { ItemsSource = new[] { "A", "B" } }),
        (typeof(Label), () => new Label { Content = "_Label" }),
        (typeof(ListBox), () => new ListBox { ItemsSource = new[] { "One", "Two" } }),
        (typeof(ListBoxItem), () => new ListBoxItem { Content = "Item" }),
        (typeof(ManagedFileChooser), () => new Border()),
        (typeof(ManagedFileChooserOverwritePrompt), () => new Border()),
        (typeof(Menu), () => new Menu { ItemsSource = new[] { new MenuItem { Header = "_File" } } }),
        (typeof(MenuFlyoutPresenter), () => new MenuFlyoutPresenter { ItemsSource = new[] { new MenuItem { Header = "Item" } } }),
        (typeof(MenuItem), () => new MenuItem { Header = "Item" }),
        (typeof(NavigationPage), () => new NavigationPage()),
        (typeof(NotificationCard), () => new NotificationCard { Content = "Note" }),
        (typeof(NumericUpDown), () => new NumericUpDown { Value = 1 }),
        (typeof(OverlayPopupHost), () => new Border()),
        (typeof(PathIcon), () => new PathIcon { Data = Geometry.Parse("M 0 0 L 10 10") }),
        (typeof(PipsPager), () => new PipsPager { NumberOfPages = 5 }),
        (typeof(PopupRoot), () => new Border()),
        (typeof(ProgressBar), () => new ProgressBar { Value = 40 }),
        (typeof(RadioButton), () => new RadioButton { Content = "Radio" }),
        (typeof(RefreshContainer), () => new RefreshContainer { Content = "Refresh" }),
        (typeof(RefreshVisualizer), () => new RefreshVisualizer()),
        (typeof(RepeatButton), () => new RepeatButton { Content = "Repeat" }),
        (typeof(ScrollBar), () => new ScrollBar { Maximum = 100, ViewportSize = 10 }),
        (typeof(ScrollViewer), () => new ScrollViewer { Content = new Border { Width = 2000, Height = 2000 } }),
        (typeof(SelectableTextBlock), () => new SelectableTextBlock { Text = "Select" }),
        (typeof(Separator), () => new Separator()),
        (typeof(Slider), () => new Slider { Value = 30 }),
        (typeof(SplitButton), () => new SplitButton { Content = "Split" }),
        (typeof(SplitView), () => new SplitView { Pane = "Pane", Content = "Content" }),
        (typeof(TabbedPage), () => new TabbedPage()),
        (typeof(TabControl), () => new TabControl { ItemsSource = new[] { new TabItem { Header = "One", Content = "1" } } }),
        (typeof(TabItem), () => new TabItem { Header = "Tab", Content = "Content" }),
        (typeof(TableView), () => new TableView()),
        (typeof(TableViewCell), () => new TableViewCell()),
        (typeof(TableViewColumnHeader), () => new TableViewColumnHeader { Content = "Name" }),
        (typeof(TableViewRow), () => new TableViewRow()),
        (typeof(TabStrip), () => new TabStrip { ItemsSource = new[] { "One", "Two" } }),
        (typeof(TabStripItem), () => new TabStripItem { Content = "Item" }),
        (typeof(TextBox), () => new TextBox { Text = "Text" }),
        (typeof(TextSelectionHandle), () => new Border()),
        (typeof(ThemeVariantScope), () => new ThemeVariantScope { Child = new TextBlock { Text = "Scope" } }),
        (typeof(TimePicker), () => new TimePicker()),
        (typeof(TimePickerPresenter), () => new TimePickerPresenter()),
        (typeof(ToggleButton), () => new ToggleButton { Content = "Toggle" }),
        (typeof(ToggleSwitch), () => new ToggleSwitch()),
        (typeof(ToolTip), () => new ToolTip { Content = "Tip" }),
        (typeof(TransitioningContentControl), () => new TransitioningContentControl { Content = "Content" }),
        (typeof(TreeView), () => new TreeView { ItemsSource = new[] { new TreeViewItem { Header = "Node" } } }),
        (typeof(TreeViewItem), () => new TreeViewItem { Header = "Node" }),
        (typeof(UserControl), () => new UserControl { Content = "User" }),
        (typeof(Window), () => new Border()),
        (typeof(WindowDrawnDecorations), () => new Border()),
        (typeof(WindowNotificationManager), () => new Border()),
    ];
}
