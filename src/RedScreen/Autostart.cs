using System.IO;
using Microsoft.Win32;

namespace RedScreen;

/// <summary>
/// «Start with Windows»: en verdi i HKCU\...\Run som peker på RedScreen.
/// Vises også i Oppgavebehandling under Oppstart, der brukeren kan slå den av.
/// </summary>
internal static class Autostart
{
    private const string ValueName = "RedScreen";

    /// <summary>Testkjøringer («--settings») bruker en egen nøkkel, så den ekte oppstarten ikke røres.</summary>
    public static string KeyPath { get; set; } = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsEnabled => Current() != null;

    /// <summary>Slår oppstart av eller på. False hvis registeret ikke kunne endres.</summary>
    public static bool Set(bool on)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            if (on) key.SetValue(ValueName, Command());
            else key.DeleteValue(ValueName, throwOnMissingValue: false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Er exe-filen flyttet siden oppstart ble slått på, pekes den om til der den ligger nå.</summary>
    public static void Refresh()
    {
        string? current = Current();
        if (current != null && current != Command()) Set(true);
    }

    private static string? Current()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(ValueName) as string;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Command()
    {
        string exe = Environment.ProcessPath ?? "";
        if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            // Kjøres via dotnet.exe (utvikling og install-local.cmd): uten konsollvindu, som install-local.cmd gjør.
            string conhost = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "conhost.exe");
            return $"\"{conhost}\" --headless \"{exe}\" \"{typeof(Autostart).Assembly.Location}\"";
        }
        return $"\"{exe}\"";
    }
}
