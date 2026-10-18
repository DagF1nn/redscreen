namespace RedScreen;

/// <summary>Filterverdier: av/på, fargetemperatur og lysstyrke.</summary>
public readonly record struct ScreenValues(bool FilterOn, int Kelvin, int BrightnessPercent)
{
    /// <summary>Verdiene en modus representerer. Filter av regnes som nøytral skjerm.</summary>
    public static ScreenValues Of(Mode m) =>
        m.FilterOn ? new(true, m.Kelvin, m.BrightnessPercent) : new(false, ColorTemperature.Neutral, 100);

    /// <summary>Glir fra <paramref name="from"/> til <paramref name="to"/>. p = 0 er start, 1 er mål.</summary>
    public static ScreenValues Lerp(ScreenValues from, ScreenValues to, double p)
    {
        p = Math.Clamp(p, 0, 1);
        // Et avslått filter regnes som nøytral skjerm, så overgangen starter/slutter mykt.
        var a = from.FilterOn ? from : new ScreenValues(false, ColorTemperature.Neutral, 100);
        var b = to.FilterOn ? to : new ScreenValues(false, ColorTemperature.Neutral, 100);
        return new ScreenValues(
            from.FilterOn || to.FilterOn,
            (int)Math.Round(a.Kelvin + (b.Kelvin - a.Kelvin) * p),
            (int)Math.Round(a.BrightnessPercent + (b.BrightnessPercent - a.BrightnessPercent) * p));
    }
}

/// <summary>
/// Én konkret hendelse: en automasjon på et bestemt tidspunkt, eller starten på dagtid-vinduet
/// (<see cref="Rule"/> er da null og <see cref="Mode"/> er <see cref="AutomationEngine.DayOffMode"/>).
/// </summary>
public sealed record ScheduleEvent(DateTime At, Automation? Rule, Mode Mode, DateTime? QuietUntil)
{
    public bool IsDayOff => Rule == null;
    public string Key => $"{Rule?.Id ?? "dayoff"}@{At:O}";
}

/// <summary>En overgang som pågår mot neste automasjon.</summary>
public sealed record FadeInfo(string Key, Automation Rule, Mode Target, DateTime From, DateTime To, double Progress);

/// <summary>Resultatet av å evaluere automasjonene på et gitt tidspunkt.</summary>
public sealed record AutomationState(
    ScheduleEvent Current,
    ScheduleEvent? Next,
    FadeInfo? Fade)
{
    public Mode Mode => Current.Mode;
    public Mode? NextMode => Next?.Mode;
    public DateTime? NextAt => Next?.At;
}

/// <summary>
/// Finner hva som gjelder nå: siste hendelse, neste hendelse og om en overgang pågår.
/// Overgangen starter «FadeMinutes» før klokkeslettet og er ferdig akkurat på klokkeslettet.
/// Dagtid-vinduet er en egen hendelse som slår av filteret; automasjoner inne i vinduet hoppes over.
/// </summary>
public static class AutomationEngine
{
    /// <summary>Hva dagtid-vinduet setter skjermen til: filter av.</summary>
    public static readonly Mode DayOffMode = new() { Id = "__dayoff", Name = "Day", FilterOn = false };

    /// <summary>Alle hendelser i tidsrommet, sortert. Automasjoner inne i dagtid-vinduet er utelatt.</summary>
    public static List<ScheduleEvent> Events(Settings settings, DateTime from, DateTime to)
    {
        var list = new List<ScheduleEvent>();
        var dayOff = settings.DayOff;
        for (var day = from.Date.AddDays(-1); day <= to.Date; day = day.AddDays(1))
        {
            foreach (var rule in settings.Automations)
            {
                if (!rule.Enabled || !rule.IsOnDay(day.DayOfWeek)) continue;
                if (dayOff.Contains(rule.Time)) continue;
                var mode = settings.FindMode(rule.ModeId);
                if (mode == null) continue;
                list.Add(new ScheduleEvent(day + rule.Time, rule, mode, null));
            }
            if (dayOff.Enabled && dayOff.Start != dayOff.End)
            {
                var at = day + dayOff.Start;
                list.Add(new ScheduleEvent(at, null, DayOffMode, dayOff.EndAfter(at)));
            }
        }
        return list.Where(e => e.At >= from.AddDays(-1) && e.At <= to).OrderBy(e => e.At).ToList();
    }

    /// <summary>
    /// Siste hendelse som skjedde i tidsrommet (fra, til]. Brukes for å utløse en automasjon
    /// akkurat når klokka passerer den, eller ved oppstart hvis den ble passert mens appen var av.
    /// </summary>
    public static ScheduleEvent? LastTriggerBetween(Settings settings, DateTime fromExclusive, DateTime toInclusive)
    {
        if (toInclusive <= fromExclusive) return null;
        // Ikke se lenger enn en uke tilbake (f.eks. PC-en har vært av lenge).
        if (toInclusive - fromExclusive > TimeSpan.FromDays(7)) fromExclusive = toInclusive - TimeSpan.FromDays(7);
        return Events(settings, fromExclusive, toInclusive).LastOrDefault(e => e.At > fromExclusive && e.At <= toInclusive);
    }

    public static AutomationState? Evaluate(Settings settings, DateTime now)
    {
        var events = Events(settings, now.AddDays(-8), now.AddDays(2));
        int idx = events.FindLastIndex(e => e.At <= now);
        if (idx < 0) return null;

        var current = events[idx];
        var next = idx + 1 < events.Count ? events[idx + 1] : null;

        FadeInfo? fade = null;
        if (next is { Rule: { FadeMinutes: > 0 } rule })
        {
            // Overgangen kan ikke starte før forrige hendelse, og ikke mens dagtid-vinduet varer.
            var from = next.At - TimeSpan.FromMinutes(rule.FadeMinutes);
            var earliest = current.QuietUntil is { } quiet && quiet > current.At ? quiet : current.At;
            if (from < earliest) from = earliest;
            if (now >= from && now < next.At)
            {
                double p = (now - from) / (next.At - from);
                fade = new FadeInfo(next.Key, rule, next.Mode, from, next.At, p);
            }
        }

        return new AutomationState(current, next, fade);
    }
}
