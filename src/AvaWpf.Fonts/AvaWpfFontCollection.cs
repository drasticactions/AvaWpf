using System;
using System.Buffers.Binary;
using System.IO;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;

namespace AvaWpf;

/// <summary>
/// The embedded font collection (<c>fonts:AvaWpf</c>): Selawik for Segoe UI, Wine's Tahoma and MS Sans Serif, and the
/// Fluent icon subset. Register it with <see cref="AvaWpfFontsAppBuilderExtensions.WithAvaWpfFonts"/>.
/// </summary>
public sealed class AvaWpfFontCollection : FontCollectionBase
{
    /// <summary>The collection key, <c>fonts:AvaWpf</c>.</summary>
    public static readonly Uri CollectionKey = new("fonts:AvaWpf", UriKind.Absolute);

    /// <summary>The family name of the Fluent icon subset.</summary>
    public const string FluentGlyphsFamilyName = "AvaWpf Fluent Glyphs";

    private static readonly Uri s_source = new("avares://AvaWpf.Fonts/Assets/Fonts", UriKind.Absolute);
    private static readonly Uri s_semilight = new("avares://AvaWpf.Fonts/Assets/FontsExtra/selawksl.ttf", UriKind.Absolute);

    /// <summary>Family-name aliases to the bundled substitutes, used only when the named font is not installed.</summary>
    public static IReadOnlyDictionary<string, FontFamily> FamilyMappings { get; } = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase)
    {
        ["Segoe UI"] = new FontFamily("fonts:AvaWpf#Selawik"),
        ["Segoe UI Variable"] = new FontFamily("fonts:AvaWpf#Selawik"),
        ["Segoe UI Variable Text"] = new FontFamily("fonts:AvaWpf#Selawik"),
        ["Segoe UI Variable Display"] = new FontFamily("fonts:AvaWpf#Selawik"),
        ["Tahoma"] = new FontFamily("fonts:AvaWpf#Tahoma"),
        // Wine's MS Sans Serif has only bitmap strikes, which do not scale, so the outline Tahoma stands in.
        ["MS Sans Serif"] = new FontFamily("fonts:AvaWpf#Tahoma"),
        ["Microsoft Sans Serif"] = new FontFamily("fonts:AvaWpf#Tahoma"),
    };

    /// <summary>Initializes the collection and loads the embedded faces.</summary>
    public AvaWpfFontCollection()
    {
        foreach (var asset in AssetLoader.GetAssets(s_source, null))
        {
            if (asset.AbsolutePath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            {
                TryAdd(asset, null);
            }
        }

        // Selawik Semilight declares weight 300, the same as Light, so it is registered by hand under SemiLight (350).
        TryAdd(s_semilight, new FontCollectionKey(FontStyle.Normal, FontWeight.SemiLight, FontStretch.Normal));
    }

    /// <inheritdoc />
    public override Uri Key => CollectionKey;

    private void TryAdd(Uri asset, FontCollectionKey? key)
    {
        try
        {
            using var source = AssetLoader.Open(asset);
            using var stream = new MemoryStream();
            source.CopyTo(stream);
            var data = stream.GetBuffer();
            // Wine's Tahoma sets USE_TYPO_METRICS (1 em line); Microsoft's does not, so WPF uses its win metrics (1.207 em).
            var name = Path.GetFileName(asset.AbsolutePath);
            UseWindowsLineMetrics(data.AsSpan(0, (int)stream.Length), ignoreTypoMetricsFlag: name.StartsWith("tahoma", StringComparison.OrdinalIgnoreCase));
            stream.Position = 0;
            if (key is { } k)
            {
                // TryAddGlyphTypeface(Stream) adds the face under its own key, then under the requested one.
                if (TryAddGlyphTypeface(stream, out var face) || face is not null)
                {
                    TryAddGlyphTypeface(face, k);
                }
            }
            else
            {
                TryAddGlyphTypeface(stream, out _);
            }
        }
        catch (Exception)
        {
            // A face that does not load is skipped; requests for it get the nearest face of the family.
        }
    }

    /// <summary>
    /// Rewrites the in-memory <c>hhea</c> metrics, which Avalonia reads, to the <c>usWinAscent</c>/<c>usWinDescent</c>
    /// line metrics DirectWrite uses. Selawik's <c>hhea</c> is smaller, which makes lines too short. Fonts that set
    /// <c>USE_TYPO_METRICS</c> are left alone unless <paramref name="ignoreTypoMetricsFlag"/> clears the flag.
    /// </summary>
    internal static void UseWindowsLineMetrics(Span<byte> font, bool ignoreTypoMetricsFlag = false)
    {
        if (!TryFindTable(font, "OS/2", out var os2, out var os2Length) || !TryFindTable(font, "hhea", out var hhea, out var hheaLength)
            || os2Length < 78 || hheaLength < 10)
        {
            return;
        }

        const ushort useTypoMetrics = 1 << 7;
        var fsSelection = BinaryPrimitives.ReadUInt16BigEndian(font[(os2 + 62)..]);
        if ((fsSelection & useTypoMetrics) != 0)
        {
            if (!ignoreTypoMetricsFlag)
            {
                return;
            }

            BinaryPrimitives.WriteUInt16BigEndian(font[(os2 + 62)..], (ushort)(fsSelection & ~useTypoMetrics));
        }

        var winAscent = BinaryPrimitives.ReadUInt16BigEndian(font[(os2 + 74)..]);
        var winDescent = BinaryPrimitives.ReadUInt16BigEndian(font[(os2 + 76)..]);
        if (winAscent == 0 && winDescent == 0)
        {
            return;
        }

        // DirectWrite: the line gap is what the hhea line height has beyond usWinAscent + usWinDescent.
        var ascender = BinaryPrimitives.ReadInt16BigEndian(font[(hhea + 4)..]);
        var descender = BinaryPrimitives.ReadInt16BigEndian(font[(hhea + 6)..]);
        var lineGap = BinaryPrimitives.ReadInt16BigEndian(font[(hhea + 8)..]);
        var gap = Math.Max(0, ascender - descender + lineGap - (winAscent + winDescent));
        BinaryPrimitives.WriteInt16BigEndian(font[(hhea + 4)..], (short)Math.Min(winAscent, (ushort)short.MaxValue));
        BinaryPrimitives.WriteInt16BigEndian(font[(hhea + 6)..], (short)-Math.Min(winDescent, (ushort)short.MaxValue));
        BinaryPrimitives.WriteInt16BigEndian(font[(hhea + 8)..], (short)Math.Min(gap, short.MaxValue));
    }

    private static bool TryFindTable(ReadOnlySpan<byte> font, string tag, out int offset, out int length)
    {
        offset = length = 0;
        if (font.Length < 12)
        {
            return false;
        }

        var count = BinaryPrimitives.ReadUInt16BigEndian(font[4..]);
        for (var i = 0; i < count; i++)
        {
            var record = 12 + (i * 16);
            if (record + 16 > font.Length)
            {
                return false;
            }

            if (font[record] == tag[0] && font[record + 1] == tag[1] && font[record + 2] == tag[2] && font[record + 3] == tag[3])
            {
                offset = (int)BinaryPrimitives.ReadUInt32BigEndian(font[(record + 8)..]);
                length = (int)BinaryPrimitives.ReadUInt32BigEndian(font[(record + 12)..]);
                return offset >= 0 && length >= 0 && offset + length <= font.Length;
            }
        }

        return false;
    }
}
