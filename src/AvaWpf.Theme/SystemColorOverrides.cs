using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;

namespace AvaWpf;

/// <summary>
/// The SystemColors roles an app replaces in every family and variant, by WPF role name (<c>Highlight</c>,
/// <c>Control</c>, <c>WindowText</c>…). A change takes effect at once.
/// </summary>
public sealed class SystemColorOverrides : IDictionary<string, Color>, IReadOnlyDictionary<string, Color>
{
    private readonly Dictionary<string, Color> _inner = new(StringComparer.Ordinal);

    /// <summary>Raised after any change.</summary>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public Color this[string key]
    {
        get => _inner[key];
        set
        {
            _inner[key] = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc/>
    public ICollection<string> Keys => _inner.Keys;

    /// <inheritdoc/>
    public ICollection<Color> Values => _inner.Values;

    /// <inheritdoc/>
    public int Count => _inner.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    IEnumerable<string> IReadOnlyDictionary<string, Color>.Keys => _inner.Keys;

    IEnumerable<Color> IReadOnlyDictionary<string, Color>.Values => _inner.Values;

    /// <inheritdoc/>
    public void Add(string key, Color value)
    {
        _inner.Add(key, value);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Add(KeyValuePair<string, Color> item) => Add(item.Key, item.Value);

    /// <inheritdoc/>
    public void Clear()
    {
        _inner.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public bool Contains(KeyValuePair<string, Color> item) => ((ICollection<KeyValuePair<string, Color>>)_inner).Contains(item);

    /// <inheritdoc/>
    public bool ContainsKey(string key) => _inner.ContainsKey(key);

    /// <inheritdoc/>
    public void CopyTo(KeyValuePair<string, Color>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, Color>>)_inner).CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, Color>> GetEnumerator() => _inner.GetEnumerator();

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        var removed = _inner.Remove(key);
        if (removed)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return removed;
    }

    /// <inheritdoc/>
    public bool Remove(KeyValuePair<string, Color> item) => Contains(item) && Remove(item.Key);

    /// <inheritdoc/>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out Color value) => _inner.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
