using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace AvaWpf.Chrome;

/// <summary>
/// The per-instance cache of chrome token lookups (<see cref="ChromeResources"/>) and the pens built from them; cleared
/// when the owner's resources or theme variant change.
/// </summary>
internal sealed class ChromeTokens
{
    private readonly StyledElement _owner;
    private readonly Dictionary<string, IBrush?> _brushes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Color?> _colors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IBrush?> _resources = new(StringComparer.Ordinal);
    private readonly Dictionary<(IBrush, double), IPen> _pens = new();

    /// <summary>Initializes a cache for <paramref name="owner"/>.</summary>
    public ChromeTokens(StyledElement owner)
    {
        _owner = owner;
    }

    /// <summary>The chrome brush token <paramref name="name"/> (<see cref="ChromeResources.Brush"/>), or null.</summary>
    public IBrush? Brush(string name)
    {
        if (!_brushes.TryGetValue(name, out var brush))
        {
            brush = ChromeResources.Brush(_owner, name);
            _brushes[name] = brush;
        }

        return brush;
    }

    /// <summary>The chrome color token <paramref name="name"/> (<see cref="ChromeResources.Color"/>), or transparent.</summary>
    public Color Color(string name)
    {
        if (!_colors.TryGetValue(name, out var color))
        {
            color = ChromeResources.Color(_owner, name);
            _colors[name] = color;
        }

        return color ?? Colors.Transparent;
    }

    /// <summary>The opacity of the brush token <paramref name="name"/>, or <paramref name="fallback"/> when it is missing.</summary>
    public double Opacity(string name, double fallback) => Brush(name)?.Opacity ?? fallback;

    /// <summary>A brush resource by its full key (for example <c>SystemColors.GrayTextBrush</c>), or null.</summary>
    public IBrush? Resource(string key)
    {
        if (!_resources.TryGetValue(key, out var brush))
        {
            brush = ChromeResources.Resource(_owner, key) switch
            {
                IBrush b => b,
                Color c => new SolidColorBrush(c),
                _ => null,
            };
            _resources[key] = brush;
        }

        return brush;
    }

    /// <summary>A pen of <paramref name="thickness"/> with the brush token <paramref name="name"/>, or null.</summary>
    public IPen? Pen(string name, double thickness) => Pen(Brush(name), thickness);

    /// <summary>A cached pen of <paramref name="thickness"/> with <paramref name="brush"/>, or null for a null brush.</summary>
    public IPen? Pen(IBrush? brush, double thickness)
    {
        if (brush is null)
        {
            return null;
        }

        if (!_pens.TryGetValue((brush, thickness), out var pen))
        {
            // Bound the cache: a chrome whose BorderBrush keeps changing would otherwise collect pens.
            if (_pens.Count >= 32)
            {
                _pens.Clear();
            }

            pen = new Pen(brush, thickness);
            _pens[(brush, thickness)] = pen;
        }

        return pen;
    }

    /// <summary>Forgets every resolved token and pen.</summary>
    public void Clear()
    {
        _brushes.Clear();
        _colors.Clear();
        _resources.Clear();
        _pens.Clear();
    }
}
