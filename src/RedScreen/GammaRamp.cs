using System.Runtime.InteropServices;

namespace RedScreen;

/// <summary>
/// Endrer skjermkortets gamma-tabell (fargetemperatur) via gdi32.
/// Må gjøres per skjerm: en DC for hele skrivebordet (GetDC(NULL))
/// avvises av Windows når det er flere skjermer.
/// </summary>
public static class GammaRamp
{
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateDC(string? driver, string device, string? output, IntPtr initData);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool SetDeviceGammaRamp(IntPtr hdc, ushort[] ramp);

    [DllImport("gdi32.dll")]
    private static extern bool GetDeviceGammaRamp(IntPtr hdc, ushort[] ramp);

    /// <summary>
    /// Tabellen slik Windows rapporterte den rett etter at vi skrev den, per skjerm.
    /// Lest tilbake i stedet for å lagre det vi sendte, fordi noen drivere runder av verdiene.
    /// </summary>
    private static readonly Dictionary<string, ushort[]> Written = new();

    /// <summary>Når true endres ikke skjermen (brukes av --snapshot).</summary>
    public static bool Disabled { get; set; }

    /// <summary>
    /// Setter gamma med multiplikator per farge (0.0–1.0) på alle skjermer. 1/1/1 = normal.
    /// Returnerer false hvis minst én skjerm avviste tabellen.
    /// </summary>
    public static bool Apply(double red, double green, double blue)
    {
        if (Disabled) return true;
        var ramp = new ushort[768]; // 256 rød, 256 grønn, 256 blå
        for (int i = 0; i < 256; i++)
        {
            int baseValue = i * 257; // 0..65535
            ramp[i] = Clamp(baseValue * red);
            ramp[256 + i] = Clamp(baseValue * green);
            ramp[512 + i] = Clamp(baseValue * blue);
        }

        bool allOk = true;
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            IntPtr hdc = CreateDC(null, screen.DeviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero)
            {
                allOk = false;
                continue;
            }
            try
            {
                bool ok = SetDeviceGammaRamp(hdc, ramp);
                allOk &= ok;
                var readBack = new ushort[768];
                if (ok && GetDeviceGammaRamp(hdc, readBack)) Written[screen.DeviceName] = readBack;
                else Written.Remove(screen.DeviceName);
            }
            finally
            {
                DeleteDC(hdc);
            }
        }
        return allOk;
    }

    public static bool Reset() => Apply(1.0, 1.0, 1.0);

    /// <summary>
    /// True hvis alle skjermer fortsatt har tabellen vi skrev sist. False hvis noe annet
    /// (fullskjerm-spill, driver, Windows) har skrevet over den, eller en ny skjerm har kommet til.
    /// </summary>
    public static bool IsIntact()
    {
        if (Disabled) return true;
        var current = new ushort[768];
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            if (!Written.TryGetValue(screen.DeviceName, out var expected)) return false;
            IntPtr hdc = CreateDC(null, screen.DeviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero) continue;
            try
            {
                if (!GetDeviceGammaRamp(hdc, current)) continue;
            }
            finally
            {
                DeleteDC(hdc);
            }
            for (int i = 0; i < current.Length; i++)
                if (Math.Abs(current[i] - expected[i]) > 256) return false;
        }
        return true;
    }

    private static ushort Clamp(double v) => (ushort)Math.Clamp(v, 0, 65535);
}
