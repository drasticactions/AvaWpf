using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace AvaWpf;

/// <summary>
/// All resources of one family and scheme in one provider (add-ons, ControlThemes, tokens, aliases, accent, system
/// values), resolving <see cref="ResourceAlias"/> entries against itself. The inner dictionaries share its owner, so
/// <c>DynamicResource</c> values inside them resolve from the scope.
/// </summary>
internal sealed class ThemeResources : ResourceProvider
{
    private readonly List<IResourceProvider> _addons = new();
    private readonly IReadOnlyDictionary<string, ResourceAlias> _aliases;

    public ThemeResources(ThemeFamily family, string scheme, Color? accent)
    {
        Family = family;
        Scheme = scheme;
        System = new SystemResources(family, scheme);
        Accent = new AccentColors(family, accent);
        Tokens = FamilyCatalog.LoadTokens(family, scheme);
        Controls = FamilyCatalog.LoadControls(family);
        _aliases = FamilyCatalog.AppAliases(family);
    }

    public ThemeFamily Family { get; }

    public string Scheme { get; }

    public SystemResources System { get; }

    public AccentColors Accent { get; }

    public ResourceDictionary Tokens { get; }

    public IResourceProvider Controls { get; }

    public override bool HasResources => true;

    /// <summary>Adds the ControlThemes of an add-on package for this family.</summary>
    public void AddAddon(IResourceProvider provider)
    {
        _addons.Add(provider);
        if (Owner is { } owner)
        {
            provider.AddOwner(owner);
            RaiseResourcesChanged();
        }
    }

    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value) =>
        TryGetResourceCore(key, theme, out value, 0);

    private bool TryGetResourceCore(object key, ThemeVariant? theme, out object? value, int depth)
    {
        for (var i = _addons.Count - 1; i >= 0; i--)
        {
            if (_addons[i].TryGetResource(key, theme, out value))
            {
                return Unalias(theme, ref value, depth);
            }
        }

        if (Controls.TryGetResource(key, theme, out value) || Tokens.TryGetResource(key, theme, out value))
        {
            return Unalias(theme, ref value, depth);
        }

        if (key is Chrome.ChromeResources.FamilyKey or "AvaWpf.Family")
        {
            value = Family;
            return true;
        }

        if (key is Chrome.ChromeResources.SchemeKey)
        {
            value = Scheme;
            return true;
        }

        if (key is ThemeWindow.WindowMotionKey)
        {
            // Each family uses the window animations of its Windows version.
            value = Family switch
            {
                ThemeFamily.Aero => Animations.WindowMotion.Windows7,
                ThemeFamily.AeroLite or ThemeFamily.Aero2 => Animations.WindowMotion.Windows10,
                ThemeFamily.Fluent => Animations.WindowMotion.Windows11,
                _ => Animations.WindowMotion.None,
            };
            return true;
        }

        if (key is "Luna.ThemeColor" && Family == ThemeFamily.Luna)
        {
            value = Scheme switch
            {
                ColorSchemes.Metallic => Chrome.Luna.ThemeColor.Metallic,
                ColorSchemes.Homestead => Chrome.Luna.ThemeColor.Homestead,
                _ => Chrome.Luna.ThemeColor.NormalColor,
            };
            return true;
        }

        if (key is string s && _aliases.TryGetValue(s, out var alias))
        {
            value = alias;
            return Unalias(theme, ref value, depth);
        }

        return Accent.TryGetResource(key, theme, out value) || System.TryGetResource(key, theme, out value);
    }

    private bool Unalias(ThemeVariant? theme, ref object? value, int depth)
    {
        if (value is not ResourceAlias alias)
        {
            return true;
        }

        if (depth > 8)
        {
            throw new InvalidOperationException($"Resource alias chain too deep at '{alias.Target}'.");
        }

        return TryGetResourceCore(alias.TargetFor(theme), theme, out value, depth + 1);
    }

    protected override void OnAddOwner(IResourceHost owner)
    {
        foreach (var p in Inner())
        {
            p.AddOwner(owner);
        }

        base.OnAddOwner(owner);
    }

    protected override void OnRemoveOwner(IResourceHost owner)
    {
        foreach (var p in Inner())
        {
            p.RemoveOwner(owner);
        }

        base.OnRemoveOwner(owner);
    }

    private IEnumerable<IResourceProvider> Inner()
    {
        yield return System;
        yield return Accent;
        yield return Tokens;
        yield return Controls;
        foreach (var a in _addons)
        {
            yield return a;
        }
    }
}
