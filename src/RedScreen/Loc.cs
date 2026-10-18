using System.Globalization;
using System.Windows;

namespace RedScreen;

/// <summary>
/// Språkstyring. Tekstene legges inn i Application.Resources, så XAML kan bruke
/// {DynamicResource nøkkel} og oppdateres med en gang språket byttes.
/// </summary>
public static class Loc
{
    public const string Fallback = "en";

    /// <summary>Språkene appen støtter: kode, navn i menyen, kort etikett.</summary>
    public static IReadOnlyList<(string Code, string Name, string Short)> Languages { get; } = new[]
    {
        ("en", "English", "EN"),
        ("nb", "Norsk", "NO"),
    };

    public static string Current { get; private set; } = Fallback;

    public static string CurrentShort => Languages.First(l => l.Code == Current).Short;

    public static string Normalize(string? code) =>
        code != null && Translations.All.ContainsKey(code) ? code : Fallback;

    /// <summary>Første oppstart: appen starter alltid på engelsk. Brukeren kan bytte språk selv.</summary>
    public static string DefaultLanguage() => Fallback;

    public static void Apply(string? code)
    {
        Current = Normalize(code);
        var resources = Application.Current?.Resources;
        if (resources == null) return;
        foreach (var key in Translations.All[Fallback].Keys)
            resources[key] = T(key);
    }

    /// <summary>Tekst for en nøkkel på gjeldende språk.</summary>
    public static string T(string key) =>
        Translations.All[Current].TryGetValue(key, out var text) ? text
        : Translations.All[Fallback].TryGetValue(key, out var fallback) ? fallback
        : key;

    /// <summary>Tekst med verdier satt inn, f.eks. F("now.fading", "Night").</summary>
    public static string F(string key, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, T(key), args);
}
