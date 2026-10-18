namespace RedScreen;

/// <summary>
/// Alle tekster i appen, per språk. Engelsk er hovedspråket og reserve:
/// mangler en nøkkel i et annet språk, brukes den engelske teksten.
/// For å legge til et språk: kopier "en"-blokken, oversett, og legg språket til i Loc.Languages.
/// </summary>
internal static class Translations
{
    public static readonly Dictionary<string, Dictionary<string, string>> All = new()
    {
        ["en"] = new()
        {
            // Systemfelt
            ["tray.open"] = "Open RedScreen",
            ["tray.filter"] = "Filter on",
            ["tray.modes"] = "Modes",
            ["tray.temperature"] = "Color temperature",
            ["tray.brightness"] = "Brightness",
            ["tray.quit"] = "Quit",
            ["tray.autostart"] = "Start with Windows",
            ["tray.rejected"] = "Windows rejected the color change.",            ["state.custom"] = "Custom",
            ["state.off"] = "Off",

            // Vindu
            ["win.minimize"] = "Minimize",
            ["win.close"] = "Close (RedScreen keeps running in the system tray)",
            ["win.language"] = "Language",
            ["tab.modes"] = "Modes",
            ["tab.automations"] = "Automations",

            // Nå-kortet
            ["now.filterOff"] = "Filter off",
            ["now.normal"] = "Screen shows normal colors",
            ["now.detail"] = "{0} · {1} % brightness",
            ["now.fading"] = "Fading to {0}, ready at {1}",
            ["now.toggleTip"] = "Filter on/off",
            ["now.byAutomation"] = "Set by automation at {0}",
            ["now.byManual"] = "Set manually",
            ["slider.temperature"] = "Color temperature",
            ["slider.fullRed"] = "Full red",
            ["slider.cool"] = "Cool",
            ["slider.brightness"] = "Brightness",
            ["temp.fullRed"] = "Full red",
            ["temp.warm"] = "Warm",
            ["temp.normal"] = "Normal",
            ["temp.cool"] = "Cool",
            ["override.title"] = "Transition paused",
            ["override.sub"] = "You changed the screen during the fade to {0}",
            ["override.resume"] = "Resume",

            // Moduser
            ["modes.title"] = "My modes",
            ["modes.hint"] = "Click to use. + saves the values above.",
            ["modes.addTip"] = "Save as new mode",
            ["modes.newTitle"] = "Save the values above as a new mode",
            ["modes.renameTitle"] = "New name",
            ["modes.defaultName"] = "Mode {0}",
            ["modes.updateTip"] = "Save the values above into this mode",
            ["modes.renameTip"] = "Rename",
            ["modes.deleteConfirm"] = "Delete \"{0}\"?",
            ["modes.deleteUsed"] = "It is used by {0} automation(s), which will be left without a mode.",
            ["modes.empty"] = "No modes yet. Set the sliders above, then press + to save them as a mode.",
            ["tray.noModes"] = "No modes yet",
            ["mode.summaryOff"] = "Filter off",

            // Felles
            ["common.cancel"] = "Cancel",
            ["common.save"] = "Save",
            ["common.delete"] = "Delete",

            // Automasjoner
            ["auto.timeTip"] = "Time, e.g. 21:15",
            ["auto.enabledTip"] = "On/off",
            ["auto.modeTip"] = "Mode to switch to",
            ["auto.fade"] = "Transition before",
            ["auto.fadeTip"] = "Starts this many minutes early from the current screen, and reaches the mode exactly at the set time.",
            ["auto.add"] = "New automation",
            ["autostart.title"] = "Start with Windows",
            ["autostart.sub"] = "Needed for automations to run after the PC restarts.",
            ["auto.empty"] = "No automations yet. Add one to switch modes automatically at set times.",
            ["auto.needMode"] = "Save a mode first (Modes tab), then add an automation that switches to it.",
            ["auto.daysTip"] = "Highlighted days are active",
            ["auto.noMode"] = "Choose mode",
            ["fade.none"] = "None",
            ["fade.minutes"] = "{0} min",
            ["dayoff.title"] = "Daytime: filter off",
            ["dayoff.sub"] = "Turns the filter off when this period starts, also if the PC was off. Automations inside it are skipped.",
            ["dayoff.to"] = "to",
            ["dayoff.skipped"] = "Skipped: inside daytime",
            ["dayoff.state"] = "Daytime",

            // Ukedager (bokstav og fullt navn)
            ["day.mon.short"] = "M", ["day.mon"] = "Monday",
            ["day.tue.short"] = "T", ["day.tue"] = "Tuesday",
            ["day.wed.short"] = "W", ["day.wed"] = "Wednesday",
            ["day.thu.short"] = "T", ["day.thu"] = "Thursday",
            ["day.fri.short"] = "F", ["day.fri"] = "Friday",
            ["day.sat.short"] = "S", ["day.sat"] = "Saturday",
            ["day.sun.short"] = "S", ["day.sun"] = "Sunday",
        },

        ["nb"] = new()
        {
            ["tray.open"] = "Åpne RedScreen",
            ["tray.filter"] = "Filter på",
            ["tray.modes"] = "Moduser",
            ["tray.temperature"] = "Fargetemperatur",
            ["tray.brightness"] = "Lysstyrke",
            ["tray.quit"] = "Avslutt",
            ["tray.autostart"] = "Start med Windows",
            ["tray.rejected"] = "Windows avviste fargeendringen.",            ["state.custom"] = "Egendefinert",
            ["state.off"] = "Av",

            ["win.minimize"] = "Minimer",
            ["win.close"] = "Lukk (RedScreen kjører videre i systemfeltet)",
            ["win.language"] = "Språk",
            ["tab.modes"] = "Moduser",
            ["tab.automations"] = "Automasjoner",

            ["now.filterOff"] = "Filter av",
            ["now.normal"] = "Skjermen vises som normalt",
            ["now.detail"] = "{0} · {1} % lysstyrke",
            ["now.fading"] = "Glir over til {0}, ferdig kl. {1}",
            ["now.toggleTip"] = "Filter av/på",
            ["now.byAutomation"] = "Satt av automasjonen kl. {0}",
            ["now.byManual"] = "Satt manuelt",
            ["slider.temperature"] = "Fargetemperatur",
            ["slider.fullRed"] = "Helt rødt",
            ["slider.cool"] = "Kald",
            ["slider.brightness"] = "Lysstyrke",
            ["temp.fullRed"] = "Helt rødt",
            ["temp.warm"] = "Varm",
            ["temp.normal"] = "Normal",
            ["temp.cool"] = "Kald",
            ["override.title"] = "Overgangen er satt på pause",
            ["override.sub"] = "Du endret skjermen mens den gled over til {0}",
            ["override.resume"] = "Fortsett",

            ["modes.title"] = "Mine moduser",
            ["modes.hint"] = "Trykk for å bruke. + lagrer verdiene over.",
            ["modes.addTip"] = "Lagre som ny modus",
            ["modes.newTitle"] = "Lagre verdiene over som ny modus",
            ["modes.renameTitle"] = "Nytt navn",
            ["modes.defaultName"] = "Modus {0}",
            ["modes.updateTip"] = "Lagre verdiene over i denne modusen",
            ["modes.renameTip"] = "Gi nytt navn",
            ["modes.deleteConfirm"] = "Slette «{0}»?",
            ["modes.deleteUsed"] = "Den brukes i {0} automasjon(er), som da står uten modus.",
            ["modes.empty"] = "Ingen moduser ennå. Still inn glidebryterne over og trykk + for å lagre dem som en modus.",
            ["tray.noModes"] = "Ingen moduser ennå",
            ["mode.summaryOff"] = "Filter av",

            ["common.cancel"] = "Avbryt",
            ["common.save"] = "Lagre",
            ["common.delete"] = "Slett",

            ["auto.timeTip"] = "Klokkeslett, f.eks. 21:15",
            ["auto.enabledTip"] = "Av/på",
            ["auto.modeTip"] = "Modus det byttes til",
            ["auto.fade"] = "Overgang før",
            ["auto.fadeTip"] = "Starter så mange minutter før, fra det skjermen viser, og er ferdig akkurat på klokkeslettet.",
            ["auto.add"] = "Ny automasjon",
            ["autostart.title"] = "Start med Windows",
            ["autostart.sub"] = "Trengs for at automasjonene skal virke etter at PC-en er startet på nytt.",
            ["auto.empty"] = "Ingen automasjoner ennå. Legg til en for å bytte modus automatisk til faste tider.",
            ["auto.needMode"] = "Lagre en modus først (fanen Moduser), og legg så til en automasjon som bytter til den.",
            ["auto.daysTip"] = "Uthevede dager er aktive",
            ["auto.noMode"] = "Velg modus",
            ["fade.none"] = "Ingen",
            ["fade.minutes"] = "{0} min",
            ["dayoff.title"] = "Dagtid: filter av",
            ["dayoff.sub"] = "Slår av filteret når tidsrommet starter, også om PC-en var av. Automasjoner innenfor hoppes over.",
            ["dayoff.to"] = "til",
            ["dayoff.skipped"] = "Hoppes over: ligger i dagtid",
            ["dayoff.state"] = "Dagtid",

            ["day.mon.short"] = "M", ["day.mon"] = "Mandag",
            ["day.tue.short"] = "T", ["day.tue"] = "Tirsdag",
            ["day.wed.short"] = "O", ["day.wed"] = "Onsdag",
            ["day.thu.short"] = "T", ["day.thu"] = "Torsdag",
            ["day.fri.short"] = "F", ["day.fri"] = "Fredag",
            ["day.sat.short"] = "L", ["day.sat"] = "Lørdag",
            ["day.sun.short"] = "S", ["day.sun"] = "Søndag",
        },
    };
}
