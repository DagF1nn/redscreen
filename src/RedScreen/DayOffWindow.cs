using System.Text.Json.Serialization;

namespace RedScreen;

/// <summary>
/// Tidsrom på dagtid der filteret skal være av. Når vinduet starter, slås filteret av,
/// også ved oppstart hvis starten ble passert mens PC-en var av. Automasjoner inne i
/// vinduet hoppes over. Brukeren kan fortsatt slå på filteret for hånd i vinduet.
/// </summary>
public class DayOffWindow : Observable
{
    private bool _enabled = true;
    private TimeSpan _start = new(7, 0, 0);
    private TimeSpan _end = new(18, 0, 0);

    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }

    public TimeSpan Start
    {
        get => _start;
        set { if (Set(ref _start, value)) OnPropertyChanged(nameof(StartText)); }
    }

    public TimeSpan End
    {
        get => _end;
        set { if (Set(ref _end, value)) OnPropertyChanged(nameof(EndText)); }
    }

    [JsonIgnore]
    public string StartText
    {
        get => Start.ToString(@"hh\:mm");
        set { if (Automation.TryParseTime(value, out var t)) Start = t; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public string EndText
    {
        get => End.ToString(@"hh\:mm");
        set { if (Automation.TryParseTime(value, out var t)) End = t; OnPropertyChanged(); }
    }

    /// <summary>Om et klokkeslett ligger inne i vinduet. Støtter vindu over midnatt.</summary>
    public bool Contains(TimeSpan t)
    {
        if (!Enabled || Start == End) return false;
        return Start < End ? t >= Start && t < End : t >= Start || t < End;
    }

    /// <summary>Slutten på vinduet som inneholder tidspunktet <paramref name="at"/>.</summary>
    public DateTime EndAfter(DateTime at)
    {
        var end = at.Date + End;
        return end > at ? end : end.AddDays(1);
    }
}
