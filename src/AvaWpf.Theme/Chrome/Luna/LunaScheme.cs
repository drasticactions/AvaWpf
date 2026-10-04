namespace AvaWpf.Chrome.Luna;

/// <summary>The scheme segment of a Luna chrome token, e.g. <c>ButtonChrome.Metallic.HoverFill</c>.</summary>
internal static class LunaScheme
{
    /// <summary>The scheme segments, in <see cref="ThemeColor"/> order.</summary>
    public static readonly string[] All = ["NormalColor", "Homestead", "Metallic"];

    /// <summary>The token segment of <paramref name="color"/>.</summary>
    public static string Name(ThemeColor color) => color switch
    {
        ThemeColor.Homestead => "Homestead",
        ThemeColor.Metallic => "Metallic",
        _ => "NormalColor",
    };
}
