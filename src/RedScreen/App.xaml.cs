using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace RedScreen;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _tray;
    private TrayMenu? _trayMenu;
    private System.Drawing.Icon? _trayIcon;
    private string? _trayIconKey;
    private SettingsWindow? _window;
    private DispatcherTimer? _automationTimer;
    private DispatcherTimer? _saveTimer;
    private DispatcherTimer? _gammaWatchTimer;
    private System.Threading.Mutex? _singleInstance;

    /// <summary>Brukeren har endret noe for hånd mens en overgang pågår.</summary>
    private bool _manualOverride;

    /// <summary>Overgangen som pågår, og verdiene skjermen hadde da den startet.</summary>
    private string? _fadeKey;
    private ScreenValues _fadeFrom;

    public Settings Settings { get; private set; } = new();

    /// <summary>Siste evaluering av automasjonene, til visning i vinduet.</summary>
    public AutomationState? AutomationState { get; private set; }

    /// <summary>Utløses når det som vises på skjermen endres.</summary>
    public event Action? StateChanged;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        // Utvikling: «--snapshot <mappe>» tegner vinduet til PNG og avslutter,
        // uten å endre skjermfarger eller lagre innstillinger.
        int snapIdx = Array.IndexOf(e.Args, "--snapshot");
        if (snapIdx >= 0 && snapIdx + 1 < e.Args.Length)
        {
            GammaRamp.Disabled = true;
            int langIdx = Array.IndexOf(e.Args, "--lang");
            string lang = langIdx >= 0 && langIdx + 1 < e.Args.Length ? e.Args[langIdx + 1] : Loc.Fallback;
            Settings = new Settings { ReadOnly = true, Language = lang };
            Loc.Apply(lang);
            Snapshot.Run(this, e.Args[snapIdx + 1]);
            return;
        }

        // Testing: «--settings <sti>» bruker en annen innstillingsfil,
        // «--dry» endrer ikke skjermfargene.
        int settingsIdx = Array.IndexOf(e.Args, "--settings");
        if (settingsIdx >= 0 && settingsIdx + 1 < e.Args.Length)
            Settings.FilePath = e.Args[settingsIdx + 1];
        if (e.Args.Contains("--dry")) GammaRamp.Disabled = true;

        // Bare én RedScreen om gangen for den ekte profilen, ellers kjemper de om skjermen.
        // Startes en til (f.eks. fra Oppstart og for hånd), avslutter den nye seg selv.
        if (settingsIdx < 0)
        {
            _singleInstance = new System.Threading.Mutex(true, @"Local\RedScreen.SingleInstance", out bool first);
            if (!first)
            {
                Shutdown();
                return;
            }
        }

        Settings = Settings.Load();
        // Første oppstart: engelsk.
        Settings.Language ??= Loc.DefaultLanguage();
        Loc.Apply(Settings.Language);

        // Utvikling: «--tray-snapshot <mappe>» tegner menyen og ikonene og avslutter.
        int traySnapIdx = Array.IndexOf(e.Args, "--tray-snapshot");
        if (traySnapIdx >= 0 && traySnapIdx + 1 < e.Args.Length)
        {
            GammaRamp.Disabled = true;
            Settings.ReadOnly = true;
            _trayMenu = new TrayMenu(this);
            TraySnapshot.Run(this, e.Args[traySnapIdx + 1]);
            return;
        }
        // Lagre med en gang, så språkvalg og omgjøring fra eldre versjoner blir varig.
        Settings.Save();
        WatchSettings();

        // Sørg for at skjermen alltid nullstilles, også ved krasj.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => GammaRamp.Reset();
        DispatcherUnhandledException += (_, _) => GammaRamp.Reset();

        _trayMenu = new TrayMenu(this);
        _tray = new Forms.NotifyIcon
        {
            Text = "RedScreen",
            ContextMenuStrip = _trayMenu.Menu,
        };
        UpdateTrayIcon();
        _tray.Visible = true;
        // Venstreklikk åpner vinduet. Av/på og moduser ligger i høyreklikkmenyen.
        _tray.MouseClick += (_, args) =>
        {
            if (args.Button == Forms.MouseButtons.Left) ShowSettings();
        };

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _saveTimer.Tick += (_, _) => { _saveTimer.Stop(); Settings.Save(); };

        ApplyCurrent();

        // Sjekk automasjonene ofte nok til at overganger blir jevne.
        _automationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _automationTimer.Tick += (_, _) => CheckAutomations();
        _automationTimer.Start();
        CheckAutomations();

        // Oppvåkning og skjermendringer kan nullstille gamma-tabellen.
        Microsoft.Win32.SystemEvents.PowerModeChanged += OnPowerModeChanged;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        // Fullskjerm inn/ut nullstiller ofte tabellen. Sjekk den ofte, så blinket blir kort.
        _gammaWatchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _gammaWatchTimer.Tick += (_, _) => WatchGamma();
        _gammaWatchTimer.Start();

        // Utvikling: «--trace <fil>» logger hva skjermen settes til hvert sekund (for å teste overganger).
        int traceIdx = Array.IndexOf(e.Args, "--trace");
        if (traceIdx >= 0 && traceIdx + 1 < e.Args.Length)
        {
            string traceFile = e.Args[traceIdx + 1];
            _automationTimer.Interval = TimeSpan.FromSeconds(1);
            StateChanged += () => System.IO.File.AppendAllText(traceFile,
                $"{DateTime.Now:HH:mm:ss} filter={Settings.FilterOn} K={Settings.Kelvin} lys={Settings.BrightnessPercent} " +
                $"fade={(AutomationState?.Fade is { } f ? $"{f.Target.DisplayName}@{f.To:HH:mm} {f.Progress:P0}" : "-")} " +
                $"modus={AutomationState?.Mode.DisplayName ?? "-"} pause={IsManualOverride} " +
                $"kilde={Settings.SetByAutomationAt?.ToString(@"hh\:mm") ?? "manuell"}{Environment.NewLine}");
            GammaReapplied += reason => System.IO.File.AppendAllText(traceFile,
                $"{DateTime.Now:HH:mm:ss} gamma på nytt ({reason}) K={Settings.Kelvin} lys={Settings.BrightnessPercent}{Environment.NewLine}");        }

        // Utvikling: «--tray-selftest <mappe>» kjører tray-ikonet og menyen på ekte og avslutter.
        int selfTestIdx = Array.IndexOf(e.Args, "--tray-selftest");
        if (selfTestIdx >= 0 && selfTestIdx + 1 < e.Args.Length)
        {
            string folder = e.Args[selfTestIdx + 1];
            Dispatcher.BeginInvoke(() => TraySelfTest.Run(this, _tray!, _trayMenu!, folder),
                DispatcherPriority.ApplicationIdle);
        }

        // Første oppstart: vis vinduet så man ser at appen kjører.
        if (e.Args.Contains("--show")) ShowSettings();
    }

    // ===== Handlinger fra vinduet og tray-ikonet =====

    public void SetFilterOn(bool on)
    {
        Settings.FilterOn = on;
        Settings.SetByAutomationAt = null;
        _manualOverride = true;
        ApplyCurrent();
        SaveSoon();
    }

    /// <summary>Glidebryterne er flyttet: egendefinerte verdier.</summary>
    public void SetManual(int kelvin, int brightnessPercent)
    {
        Settings.Kelvin = kelvin;
        Settings.BrightnessPercent = brightnessPercent;
        Settings.FilterOn = true;
        Settings.ActiveModeId = null;
        Settings.SetByAutomationAt = null;
        _manualOverride = true;
        ApplyCurrent();
        SaveSoon();
    }

    public void ActivateMode(Mode mode)
    {
        _manualOverride = true;
        Settings.SetByAutomationAt = null;
        UseMode(mode);
    }

    /// <summary>Lagrer verdiene på skjermen som en ny modus.</summary>
    public Mode AddMode(string name)
    {
        var mode = new Mode
        {
            Name = name,
            Kelvin = Settings.Kelvin,
            BrightnessPercent = Settings.BrightnessPercent,
            FilterOn = Settings.FilterOn,
        };
        Settings.Modes.Add(mode);
        Settings.ActiveModeId = mode.Id;
        ApplyCurrent();
        return mode;
    }

    public void UpdateModeFromCurrent(Mode mode)
    {
        mode.Kelvin = Settings.Kelvin;
        mode.BrightnessPercent = Settings.BrightnessPercent;
        mode.FilterOn = Settings.FilterOn;
        Settings.ActiveModeId = mode.Id;
        ApplyCurrent();
    }

    public void DeleteMode(Mode mode)
    {
        Settings.Modes.Remove(mode);
        foreach (var a in Settings.Automations.Where(a => a.ModeId == mode.Id))
            a.ModeId = null;
        if (Settings.ActiveModeId == mode.Id) Settings.ActiveModeId = null;
        ApplyCurrent();
    }

    public Automation AddAutomation()
    {
        // Ny automasjon en time etter den siste, med siste modus som forslag.
        var last = Settings.Automations.OrderBy(a => a.Time).LastOrDefault();
        var time = last == null ? new TimeSpan(22, 0, 0) : TimeSpan.FromMinutes(
            Math.Min(last.Time.TotalMinutes + 60, 23 * 60 + 45));
        var automation = new Automation
        {
            Time = time,
            ModeId = Settings.Modes.LastOrDefault(m => m.FilterOn)?.Id ?? Settings.Modes.FirstOrDefault()?.Id,
        };
        Settings.Automations.Add(automation);
        return automation;
    }

    public void DeleteAutomation(Automation automation) => Settings.Automations.Remove(automation);

    /// <summary>Fortsett en overgang som ble satt på pause av en manuell endring.</summary>
    public void ResumeAutomations()
    {
        _manualOverride = false;
        _fadeKey = null;
        CheckAutomations();
    }

    /// <summary>Brukeren har endret noe for hånd mens en overgang pågår.</summary>
    public bool IsManualOverride => _manualOverride && AutomationState?.Fade != null;

    // ===== Språk =====

    public void SetLanguage(string code)
    {
        Settings.Language = Loc.Normalize(code);
        Loc.Apply(Settings.Language);
        foreach (var m in Settings.Modes) m.RefreshText();
        foreach (var a in Settings.Automations) a.RefreshText();
        UpdateTrayText();
        SaveSoon();
        StateChanged?.Invoke();
    }

    private void UpdateTrayText()
    {
        if (_tray != null)
        {
            string? activeId = Settings.Modes.FirstOrDefault(m => m.IsActive)?.Id;
            string name = Settings.FindMode(activeId)?.DisplayName
                ?? Loc.T(Settings.FilterOn ? "state.custom" : "state.off");
            string text = $"RedScreen: {name}";
            _tray.Text = text.Length > 63 ? text[..63] : text;
        }
        UpdateTrayIcon();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int index, uint dpi);

    /// <summary>
    /// Ikonstørrelse i systemfeltet i ekte piksler, så ikonet blir skarpt ved skalering (125–250 %).
    /// </summary>
    private static int TrayIconSize()
    {
        try
        {
            const int SM_CXSMICON = 49;
            int size = GetSystemMetricsForDpi(SM_CXSMICON, GetDpiForSystem());
            return size > 0 ? size : 16;
        }
        catch (EntryPointNotFoundException)
        {
            return Forms.SystemInformation.SmallIconSize.Width;
        }
    }

    /// <summary>Tray-ikonet viser fargen skjermen har nå. Tegnes bare på nytt når fargen endres.</summary>
    private void UpdateTrayIcon()
    {
        if (_tray == null) return;
        var s = Settings;
        // Rund av så overganger ikke tegner ikonet på nytt hvert tiende sekund for små endringer.
        string key = s.FilterOn ? $"{s.Kelvin / 100}:{s.BrightnessPercent / 5}" : "off";
        if (key == _trayIconKey) return;
        _trayIconKey = key;

        int size = TrayIconSize();
        using var bmp = IconFactory.DrawTray(size, s.FilterOn, s.Kelvin, s.BrightnessPercent);
        var old = _trayIcon;
        _trayIcon = IconFactory.ToIcon(bmp);
        _tray.Icon = _trayIcon;
        old?.Dispose();
    }

    // ===== Automasjoner =====

    /// <summary>
    /// En automasjon slår inn når klokka passerer tidspunktet, ikke før. Ble tidspunktet passert
    /// mens appen var av, slår den inn ved oppstart. Å lage eller endre en automasjon gjør ingenting
    /// før neste gang klokka når tidspunktet. Mellom automasjonene styrer brukeren selv.
    /// </summary>
    public void CheckAutomations()
    {
        var now = DateTime.Now;
        var lastCheck = Settings.LastAutomationCheck;
        Settings.LastAutomationCheck = now;
        // Lagre sjekktidspunktet av og til, så vi vet hva som ble passert mens appen var av.
        if (_lastCheckSaved == null || now - _lastCheckSaved > TimeSpan.FromMinutes(1))
        {
            _lastCheckSaved = now;
            SaveSoon();
        }

        var state = AutomationEngine.Evaluate(Settings, now);
        AutomationState = state;

        // 1) Har klokka passert en automasjon siden sist? Da slår den inn.
        var trigger = lastCheck is { } from ? AutomationEngine.LastTriggerBetween(Settings, from, now) : null;
        if (trigger is { } t)
        {
            _fadeKey = null;
            _manualOverride = false;
            if (t.IsDayOff)
            {
                // Dagtid: filteret av, ingen modus markert.
                Settings.FilterOn = false;
                Settings.ActiveModeId = null;
                Settings.SetByAutomationAt = null;
                ApplyCurrent();
                SaveSoon();
            }
            else
            {
                Settings.SetByAutomationAt = t.Rule!.Time;
                UseMode(t.Mode);
            }
            return;
        }

        // 2) Pågår en overgang mot neste automasjon? Gli fra det skjermen viser nå.
        if (state?.Fade is { } fade)
        {
            if (fade.Key != _fadeKey)
            {
                _fadeKey = fade.Key;
                _fadeFrom = new ScreenValues(Settings.FilterOn, Settings.Kelvin, Settings.BrightnessPercent);
                _manualOverride = false;
            }

            if (!_manualOverride)
            {
                var v = ScreenValues.Lerp(_fadeFrom, ScreenValues.Of(fade.Target), fade.Progress);
                Settings.FilterOn = v.FilterOn;
                Settings.Kelvin = v.Kelvin;
                Settings.BrightnessPercent = v.BrightnessPercent;
                Settings.ActiveModeId = null;
                Settings.SetByAutomationAt = fade.Rule.Time;
                // Under overgangen markeres modusen det glis mot.
                ApplyCurrent(markModeId: fade.Target.Id);
                return;
            }
        }
        else
        {
            _fadeKey = null;
            _manualOverride = false;
        }

        // 3) Stabil: ingenting å endre, men skriv fargen på nytt av og til i tilfelle tabellen er nullstilt.
        if (now - _lastGammaWrite >= TimeSpan.FromSeconds(30)) ReapplyGamma("periodic");

        StateChanged?.Invoke();
    }

    private DateTime? _lastCheckSaved;

    // ===== Skjerm =====

    private void UseMode(Mode mode)
    {
        Settings.FilterOn = mode.FilterOn;
        if (mode.FilterOn)
        {
            Settings.Kelvin = mode.Kelvin;
            Settings.BrightnessPercent = mode.BrightnessPercent;
        }
        Settings.ActiveModeId = mode.Id;
        ApplyCurrent();
        SaveSoon();
    }

    /// <summary>Setter skjermen ut fra gjeldende innstillinger.</summary>
    public void ApplyCurrent(string? markModeId = null)
    {
        if (!WriteGamma())
        {
            _tray?.ShowBalloonTip(3000, "RedScreen",
                Loc.T("tray.rejected"), Forms.ToolTipIcon.Warning);
        }

        string? activeId = markModeId ?? Settings.ActiveModeId;
        foreach (var m in Settings.Modes) m.IsActive = m.Id == activeId;

        UpdateTrayText();

        StateChanged?.Invoke();
    }

    private DateTime _lastGammaWrite;
    /// <summary>Skriver gjeldende farge til gamma-tabellen. False hvis en skjerm avviste den.</summary>
    private bool WriteGamma()
    {
        _lastGammaWrite = DateTime.Now;
        if (!Settings.FilterOn) return GammaRamp.Reset();
        var (r, g, b) = ColorTemperature.ToRgb(Settings.Kelvin);
        double dim = Settings.BrightnessPercent / 100.0;
        return GammaRamp.Apply(r * dim, g * dim, b * dim);
    }

    /// <summary>Utløses når gamma skrives på nytt uten endring (brukes av --trace).</summary>
    internal event Action<string>? GammaReapplied;

    /// <summary>
    /// Søvn, skjermer som kobles fra og drivere kan nullstille gamma-tabellen uten at vi får vite det.
    /// Skriv derfor samme verdier på nytt. Stille (ingen varsel), og bare når filteret er på:
    /// nøytralt er riktig når det er av, og spill som bruker gamma selv skal få være i fred.
    /// </summary>
    private void ReapplyGamma(string reason)
    {
        if (!Settings.FilterOn) return;
        WriteGamma();
        GammaReapplied?.Invoke(reason);
    }

    private readonly Queue<DateTime> _watchRewrites = new();
    private DateTime _watchPausedUntil;

    /// <summary>
    /// Skriver fargen på nytt med en gang noe annet har overskrevet tabellen.
    /// Et spill som selv skriver gamma hele tiden ville gitt en evig dragkamp (flimring),
    /// så etter mange omskrivinger på kort tid tar vakten pause og lar 30-sekunders-skrivingen ta over.
    /// </summary>
    private void WatchGamma()
    {
        if (!Settings.FilterOn) return;
        var now = DateTime.Now;
        if (now < _watchPausedUntil) return;
        if (GammaRamp.IsIntact()) return;

        while (_watchRewrites.Count > 0 && now - _watchRewrites.Peek() > TimeSpan.FromSeconds(10))
            _watchRewrites.Dequeue();
        _watchRewrites.Enqueue(now);
        if (_watchRewrites.Count > 8)
        {
            _watchPausedUntil = now.AddSeconds(60);
            _watchRewrites.Clear();
            GammaReapplied?.Invoke("watch paused");
            return;
        }
        ReapplyGamma("watch");
    }

    private void OnPowerModeChanged(object? sender, Microsoft.Win32.PowerModeChangedEventArgs e)
    {
        if (e.Mode == Microsoft.Win32.PowerModes.Resume) Dispatcher.BeginInvoke(() => OnScreenMayHaveReset("resume"));
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() => OnScreenMayHaveReset("display"));

    /// <summary>
    /// Etter oppvåkning eller skjermendring: sjekk automasjonene (tid kan ha passert),
    /// skriv fargen på nytt, og én gang til litt senere, siden skjermer og drivere
    /// kan bruke noen sekunder på å våkne og nullstille tabellen etter at hendelsen kom.
    /// </summary>
    private void OnScreenMayHaveReset(string reason)
    {
        CheckAutomations();
        ReapplyGamma(reason);
        var retry = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        retry.Tick += (_, _) =>
        {
            retry.Stop();
            ReapplyGamma(reason + "+3s");
        };
        retry.Start();
    }

    // ===== Lagring =====

    public void SaveSoon()
    {
        _saveTimer?.Stop();
        _saveTimer?.Start();
    }

    /// <summary>Lagrer automatisk når moduser eller automasjoner endres.</summary>
    private void WatchSettings()
    {
        void OnItemChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Kun visningsegenskaper: ingen grunn til å lagre.
            if (e.PropertyName is nameof(Mode.IsActive) or nameof(Mode.DisplayName) or nameof(Mode.Summary)
                or nameof(Mode.PreviewBrush) or nameof(Automation.FadeText) or nameof(Automation.TimeText)
                or nameof(Automation.InDayOff)
                or nameof(DayOffWindow.StartText) or nameof(DayOffWindow.EndText)) return;
            SaveSoon();
            if (sender is Automation or DayOffWindow) CheckAutomations();
            if (sender is Mode) UpdateTrayText();
        }

        void Hook(System.Collections.IList? items)
        {
            if (items == null) return;
            foreach (var item in items.OfType<INotifyPropertyChanged>())
                item.PropertyChanged += OnItemChanged;
        }

        void Unhook(System.Collections.IList? items)
        {
            if (items == null) return;
            foreach (var item in items.OfType<INotifyPropertyChanged>())
                item.PropertyChanged -= OnItemChanged;
        }

        void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Unhook(e.OldItems);
            Hook(e.NewItems);
            SaveSoon();
            if (sender == Settings.Automations) CheckAutomations();
        }

        Hook(Settings.Modes);
        Hook(Settings.Automations);
        Settings.Modes.CollectionChanged += OnCollectionChanged;
        Settings.Automations.CollectionChanged += OnCollectionChanged;
        Settings.DayOff.PropertyChanged += OnItemChanged;
    }

    // ===== Vindu og avslutning =====

    public void ShowSettings()
    {
        _window ??= new SettingsWindow(this);
        _window.Show();
        if (_window.WindowState == System.Windows.WindowState.Minimized)
            _window.WindowState = System.Windows.WindowState.Normal;
        _window.Activate();
    }

    public void Quit()
    {
        // SystemEvents er statiske og må kobles fra eksplisitt.
        Microsoft.Win32.SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _saveTimer?.Stop();
        _gammaWatchTimer?.Stop();
        Settings.Save();
        GammaRamp.Reset();
        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
        _trayIcon?.Dispose();
        Shutdown();
    }

    /// <summary>Tray-menyen (brukes ved testing).</summary>
    internal TrayMenu? TrayMenuForTest => _trayMenu;
}
