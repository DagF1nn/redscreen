using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RedScreen;

/// <summary>
/// Brukerens innstillinger. Lagres som JSON i %AppData%\RedScreen\settings.json.
/// </summary>
public class Settings : Observable
{
    /// <summary>Språkkode («en», «nb»). Null = ikke valgt ennå, følg Windows.</summary>
    public string? Language { get; set; }

    // Det som vises på skjermen akkurat nå.
    public bool FilterOn { get; set; }
    public int Kelvin { get; set; } = 3400;
    public int BrightnessPercent { get; set; } = 100;

    /// <summary>Modusen som sist ble valgt (null = egendefinerte verdier).</summary>
    public string? ActiveModeId { get; set; }

    /// <summary>Klokkeslettet til automasjonen som satte det skjermen viser nå (null = satt manuelt).</summary>
    public TimeSpan? SetByAutomationAt { get; set; }
    /// <summary>
    /// Sist appen sjekket automasjonene. Et klokkeslett som passeres etter dette, slår inn
    /// (også ved oppstart, hvis det ble passert mens appen var av). Eldre klokkeslett gjør ingenting.
    /// </summary>
    public DateTime? LastAutomationCheck { get; set; }

    public ObservableCollection<Mode> Modes { get; set; } = new();

    public ObservableCollection<Automation> Automations { get; set; } = new();

    /// <summary>Tidsrom på dagtid der filteret er av.</summary>
    public DayOffWindow DayOff { get; set; } = new();

    /// <summary>
    /// Fra eldre versjoner med én hovedbryter for alle automasjoner. Leses bare inn,
    /// og gjøres om til av/på per automasjon i <see cref="MigrateLegacy"/>.
    /// </summary>
    [JsonPropertyName("AutomationsEnabled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyAutomationsEnabled { get; set; }

    /// <summary>Gjør om gamle innstillinger slik at appen oppfører seg som før.</summary>
    public void MigrateLegacy()
    {
        // Hovedbryteren var av: slå av hver automasjon, så ingen plutselig begynner å kjøre.
        if (LegacyAutomationsEnabled == false)
            foreach (var a in Automations) a.Enabled = false;
        LegacyAutomationsEnabled = null;

        // Forrige versjon hadde innebygde moduser med oversatt navn. Gi dem et fast navn
        // (engelsk), så de vises riktig og kan omdøpes som andre moduser.
        foreach (var m in Modes.Where(m => string.IsNullOrWhiteSpace(m.Name) && m.NameKey != null))
            m.Name = LegacyNames.TryGetValue(m.NameKey!, out var n) ? n : m.Id;
        foreach (var m in Modes) m.NameKey = null;
    }

    private static readonly Dictionary<string, string> LegacyNames = new()
    {
        ["mode.day"] = "Day", ["mode.evening"] = "Evening", ["mode.night"] = "Night",
        ["mode.red"] = "Red", ["mode.fullRed"] = "Full red",
    };

    public Mode? FindMode(string? id) => id == null ? null : Modes.FirstOrDefault(m => m.Id == id);

    /// <summary>Sorterer automasjonene etter klokkeslett.</summary>
    public void SortAutomations()
    {
        var sorted = Automations.OrderBy(a => a.Time).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            int current = Automations.IndexOf(sorted[i]);
            if (current != i) Automations.Move(current, i);
        }
    }

    /// <summary>Kan overstyres med «--settings &lt;sti&gt;» (testing), ellers %AppData%\RedScreen\settings.json.</summary>
    public static string FilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RedScreen", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), JsonOptions) ?? new Settings();
                loaded.MigrateLegacy();
                return loaded;
            }
        }
        catch (Exception)
        {
            // Ødelagt fil: start med standardverdier i stedet for å krasje.
        }
        return new Settings();
    }

    /// <summary>Når true lagres ingenting (brukes av --snapshot).</summary>
    [JsonIgnore]
    public bool ReadOnly { get; set; }

    public void Save()
    {
        if (ReadOnly) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception)
        {
            // Lagring feilet (f.eks. låst fil). Prøver igjen ved neste endring.
        }
    }
}
