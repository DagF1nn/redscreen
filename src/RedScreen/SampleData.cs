namespace RedScreen;

/// <summary>Eksempeldata for testverktøyene (snapshot og selvtest). Brukes aldri i vanlig drift.</summary>
internal static class SampleData
{
    public static void Fill(Settings s, bool norwegian)
    {
        s.Modes.Clear();
        s.Automations.Clear();
        s.Modes.Add(new Mode { Id = "day", Name = norwegian ? "Dag" : "Day", FilterOn = false, Kelvin = 6500, BrightnessPercent = 100 });
        s.Modes.Add(new Mode { Id = "evening", Name = norwegian ? "Kveld" : "Evening", Kelvin = 4500, BrightnessPercent = 90 });
        s.Modes.Add(new Mode { Id = "night", Name = norwegian ? "Natt" : "Night", Kelvin = 3000, BrightnessPercent = 70 });
        s.Modes.Add(new Mode { Id = "red", Name = norwegian ? "Rødt" : "Red", Kelvin = 1200, BrightnessPercent = 50 });
        s.Automations.Add(new Automation { Time = new TimeSpan(7, 0, 0), ModeId = "day" });
        s.Automations.Add(new Automation { Time = new TimeSpan(19, 0, 0), ModeId = "evening", FadeMinutes = 30 });
        s.Automations.Add(new Automation { Time = new TimeSpan(21, 0, 0), ModeId = "night", FadeMinutes = 30 });
        s.Automations.Add(new Automation { Time = new TimeSpan(22, 30, 0), ModeId = "red", FadeMinutes = 15,
            Saturday = false, Sunday = false });
    }
}
