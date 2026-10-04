using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace AvaWpf.Chrome;

/// <summary>
/// The per-instance cache of a chrome's token brushes and pens from <see cref="ChromeResources"/>. The owner clears it
/// when its resources, theme variant or tree change.
/// </summary>
internal sealed class ChromeBrushCache
{
    private readonly StyledElement _owner;
    private readonly Dictionary<string, IBrush?> _brushes = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Name, double Thickness), IPen?> _pens = new();
    private IBrush? _userBrush;
    private double _userThickness;
    private IPen? _userPen;

    /// <summary>Creates the cache for <paramref name="owner"/>.</summary>
    public ChromeBrushCache(StyledElement owner) => _owner = owner;

    /// <summary>The chrome brush token <paramref name="name"/>, or null when the family does not define it.</summary>
    public IBrush? Brush(string name)
    {
        if (!_brushes.TryGetValue(name, out var brush))
        {
            brush = ChromeResources.Brush(_owner, name);
            _brushes[name] = brush;
        }

        return brush;
    }

    /// <summary>A pen of <paramref name="thickness"/> with the chrome brush token <paramref name="name"/>, or null.</summary>
    public IPen? Pen(string name, double thickness = 1.0)
    {
        if (!_pens.TryGetValue((name, thickness), out var pen))
        {
            pen = Brush(name) is { } b ? new Avalonia.Media.Pen(b, thickness) : null;
            _pens[(name, thickness)] = pen;
        }

        return pen;
    }

    /// <summary>
    /// A pen of <paramref name="thickness"/> with a brush the chrome's user set (BorderBrush), or null. The last pen is
    /// kept, as WPF keeps its common border pen.
    /// </summary>
    public IPen? UserPen(IBrush? brush, double thickness = 1.0)
    {
        if (brush is null)
        {
            return null;
        }

        if (!ReferenceEquals(brush, _userBrush) || thickness != _userThickness || _userPen is null)
        {
            _userBrush = brush;
            _userThickness = thickness;
            _userPen = new Avalonia.Media.Pen(brush, thickness);
        }

        return _userPen;
    }

    /// <summary>Drops every cached brush and pen.</summary>
    public void Clear()
    {
        _brushes.Clear();
        _pens.Clear();
        _userBrush = null;
        _userPen = null;
    }
}
