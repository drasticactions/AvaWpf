using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace AvaWpf.Theme.Tests;

/// <summary>
/// Avalonia applies a templated parent's ControlTheme setters to every template child of the theme's target type, so
/// such a child takes the parent's template and setters on top of its own theme. No template may hold one.
/// </summary>
public class TemplatedParentThemeTests
{
    public static IEnumerable<object[]> FamilyControls() =>
        from f in Families.Selected()
        from c in ThemedControls.All
        where !ThemedControls.ThemeOnly.Contains(c.Type)
        select new object[] { f, c.Type.Name };

    [AvaloniaTheory]
    [MemberData(nameof(FamilyControls))]
    public void No_Template_Child_Takes_Its_Parents_Theme(ThemeFamily family, string control)
    {
        var instance = ThemedControls.All.Single(c => c.Type.Name == control).Create();
        var window = new Window { Content = new ThemeScope { Theme = family, Child = instance }, Width = 400, Height = 300 };
        window.Show();
        window.UpdateLayout();

        var theme = instance.Theme
            ?? (instance.TryFindResource(instance.StyleKey, instance.ActualThemeVariant, out var found) ? found as ControlTheme : null);
        var targets = new List<System.Type>();
        for (var t = theme; t is not null; t = t.BasedOn as ControlTheme)
        {
            if (t.TargetType is { } target && (t.Setters.Count > 0 || t.Animations.Count > 0))
            {
                targets.Add(target);
            }
        }

        var leaks = instance.GetVisualDescendants().OfType<StyledElement>()
            .Where(c => c.TemplatedParent == instance && targets.Any(t => t.IsAssignableFrom(c.StyleKey)))
            .Select(c => $"{c.GetType().Name} {c.Name}");
        Assert.Empty(leaks);
        window.Close();
    }
}
