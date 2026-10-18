using System.Text.Json.Serialization;

namespace RedScreen;

/// <summary>
/// Én automasjon: «kl. 20:15 bytt til Sovemodus», med valgfrie dager og overgang.
/// </summary>
public class Automation : Observable
{
    private string _id = Guid.NewGuid().ToString("N");
    private bool _enabled = true;
    private TimeSpan _time = new(22, 0, 0);
    private string? _modeId;
    private int _fadeMinutes;
    private bool _monday = true, _tuesday = true, _wednesday = true, _thursday = true,
                 _friday = true, _saturday = true, _sunday = true;

    public string Id { get => _id; set => Set(ref _id, value); }
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }

    public TimeSpan Time
    {
        get => _time;
        set { if (Set(ref _time, value)) OnPropertyChanged(nameof(TimeText)); }
    }

    /// <summary>Modusen som skal brukes fra dette tidspunktet.</summary>
    public string? ModeId { get => _modeId; set => Set(ref _modeId, value); }

    /// <summary>Hvor mange minutter overgangen fra forrige modus skal ta. 0 = med en gang.</summary>
    public int FadeMinutes
    {
        get => _fadeMinutes;
        set { if (Set(ref _fadeMinutes, value)) OnPropertyChanged(nameof(FadeText)); }
    }

    [JsonIgnore]
    public string FadeText => FadeMinutes == 0 ? Loc.T("fade.none") : Loc.F("fade.minutes", FadeMinutes);

    /// <summary>Kalles når språket byttes.</summary>
    public void RefreshText() => OnPropertyChanged(nameof(FadeText));

    /// <summary>Settes av vinduet: klokkeslettet ligger i dagtid-vinduet, så automasjonen hoppes over.</summary>
    private bool _inDayOff;
    [JsonIgnore]
    public bool InDayOff { get => _inDayOff; set => Set(ref _inDayOff, value); }

    public bool Monday { get => _monday; set => Set(ref _monday, value); }
    public bool Tuesday { get => _tuesday; set => Set(ref _tuesday, value); }
    public bool Wednesday { get => _wednesday; set => Set(ref _wednesday, value); }
    public bool Thursday { get => _thursday; set => Set(ref _thursday, value); }
    public bool Friday { get => _friday; set => Set(ref _friday, value); }
    public bool Saturday { get => _saturday; set => Set(ref _saturday, value); }
    public bool Sunday { get => _sunday; set => Set(ref _sunday, value); }

    /// <summary>Klokkeslett som tekst for redigering, f.eks. «20:15».</summary>
    [JsonIgnore]
    public string TimeText
    {
        get => Time.ToString(@"hh\:mm");
        set
        {
            if (TryParseTime(value, out var t)) Time = t;
            // Vis alltid normalisert tekst (eller gammel verdi ved ugyldig input).
            OnPropertyChanged();
        }
    }

    public bool IsOnDay(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Monday,
        DayOfWeek.Tuesday => Tuesday,
        DayOfWeek.Wednesday => Wednesday,
        DayOfWeek.Thursday => Thursday,
        DayOfWeek.Friday => Friday,
        DayOfWeek.Saturday => Saturday,
        _ => Sunday,
    };

    /// <summary>Godtar «20:15», «20.15», «2015», «7» og liknende.</summary>
    public static bool TryParseTime(string? text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        string s = text.Trim().Replace('.', ':').Replace(',', ':');

        int hours, minutes = 0;
        if (s.Contains(':'))
        {
            var parts = s.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out hours) || !int.TryParse(parts[1], out minutes))
                return false;
        }
        else
        {
            if (!int.TryParse(s, out int n)) return false;
            if (s.Length <= 2) hours = n;
            else if (s.Length <= 4) { hours = n / 100; minutes = n % 100; }
            else return false;
        }

        if (hours is < 0 or > 23 || minutes is < 0 or > 59) return false;
        time = new TimeSpan(hours, minutes, 0);
        return true;
    }
}
