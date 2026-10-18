using System.Text.Json.Serialization;
using System.Windows.Media;

namespace RedScreen;

/// <summary>En lagret modus, f.eks. «Reading»: fargetemperatur, lysstyrke og om filteret er på.</summary>
public class Mode : Observable
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "";
    private string? _nameKey;
    private int _kelvin = 3400;
    private int _brightnessPercent = 100;
    private bool _filterOn = true;
    private bool _isActive;

    public string Id { get => _id; set => Set(ref _id, value); }

    /// <summary>Navn brukeren har gitt. Tomt for innebygde moduser som ikke er omdøpt.</summary>
    public string Name
    {
        get => _name;
        set { if (Set(ref _name, value)) OnPropertyChanged(nameof(DisplayName)); }
    }

    /// <summary>Oversettelsesnøkkel for innebygde moduser, så navnet følger språket.</summary>
    public string? NameKey
    {
        get => _nameKey;
        set { if (Set(ref _nameKey, value)) OnPropertyChanged(nameof(DisplayName)); }
    }

    public int Kelvin
    {
        get => _kelvin;
        set { if (Set(ref _kelvin, value)) RaiseLook(); }
    }

    public int BrightnessPercent
    {
        get => _brightnessPercent;
        set { if (Set(ref _brightnessPercent, value)) RaiseLook(); }
    }

    public bool FilterOn
    {
        get => _filterOn;
        set { if (Set(ref _filterOn, value)) RaiseLook(); }
    }

    /// <summary>Navnet som vises: eget navn, ellers oversatt standardnavn.</summary>
    [JsonIgnore]
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name) && NameKey != null ? Loc.T(NameKey) : Name;

    /// <summary>Om denne modusen er den som brukes nå (kun til visning).</summary>
    [JsonIgnore]
    public bool IsActive { get => _isActive; set => Set(ref _isActive, value); }

    [JsonIgnore]
    public Brush PreviewBrush => ColorPreview.Brush(FilterOn, Kelvin, BrightnessPercent);

    [JsonIgnore]
    public string Summary => FilterOn ? $"{Kelvin} K · {BrightnessPercent} %" : Loc.T("mode.summaryOff");

    /// <summary>Kalles når språket byttes.</summary>
    public void RefreshText()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Summary));
    }

    private void RaiseLook()
    {
        OnPropertyChanged(nameof(PreviewBrush));
        OnPropertyChanged(nameof(Summary));
    }
}
