using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RedScreen;

/// <summary>
/// Utviklerverktøy: kjører tray-menyen og ikonet slik brukeren møter dem.
/// Viser ekte ikon i systemfeltet, åpner menyen på skjermen og tar skjermdump,
/// klikker menyvalg og logger hva som skjedde. Kjøres med «--tray-selftest &lt;mappe&gt;»
/// sammen med «--settings &lt;fil&gt; --dry», så skjermen og ekte innstillinger ikke røres.
/// </summary>
internal static class TraySelfTest
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NOTIFYICONIDENTIFIER
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public Guid guidItem;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [DllImport("shell32.dll")]
    private static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER id, out RECT rect);

    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(IntPtr process, uint flags);

    public static void Run(App app, NotifyIcon tray, TrayMenu trayMenu, string folder)
    {
        Directory.CreateDirectory(folder);
        var log = new List<string>();
        void Log(string s) => log.Add(s);
        var menu = trayMenu.Menu;

        // 1) Ligger ikonet faktisk i systemfeltet?
        var rect = FindTrayRect(tray);
        Log($"ikon i systemfelt: {(rect.HasValue ? $"ja ({rect.Value.Width}x{rect.Value.Height} px ved {rect.Value.X},{rect.Value.Y})" : "nei, " + LastError)}");
        Log($"ikon satt: {tray.Icon != null}, størrelse {tray.Icon?.Width}x{tray.Icon?.Height}, " +
            $"systemets små ikoner {SystemInformation.SmallIconSize.Width} px, DPI-bevissthet {Application.HighDpiMode}");
        if (rect.HasValue)
        {
            var r = Rectangle.Inflate(rect.Value, 8, 4);
            Capture(r, Path.Combine(folder, "1-ikon-i-systemfelt.png"));
            // Forstørret 6x uten utjevning, så man ser om ikonet er skarpt.
            using var src = new Bitmap(Path.Combine(folder, "1-ikon-i-systemfelt.png"));
            using var big = new Bitmap(src.Width * 6, src.Height * 6);
            using (var g = Graphics.FromImage(big))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(src, 0, 0, big.Width, big.Height);
            }
            big.Save(Path.Combine(folder, "1b-ikon-forstorret.png"), ImageFormat.Png);
        }

        // 2) Åpne menyen på skjermen, som ved høyreklikk, og ta skjermdump.
        // Først som ny bruker uten moduser: undermenyen skal si det i stedet for å være tom.
        trayMenu.Build();
        var emptyModes = menu.Items.OfType<ToolStripMenuItem>().First(i => i.Text == Loc.T("tray.modes"));
        Log($"ny bruker, Moduser-undermeny: {string.Join(" | ", emptyModes.DropDownItems.OfType<ToolStripItem>().Select(i => $"{i.Text} (aktiv={i.Enabled})"))}");
        SampleData.Fill(app.Settings, Loc.Current == "nb");
        app.ActivateMode(app.Settings.Modes.First(m => m.Id == "night"));

        var at = rect.HasValue ? new Point(rect.Value.X, rect.Value.Y) : new Point(400, 400);
        menu.Show(at, ToolStripDropDownDirection.AboveLeft);
        Application.DoEvents(); Thread.Sleep(300); Application.DoEvents();
        Log($"meny åpen: {menu.Visible}, punkter: {string.Join(" | ", menu.Items.OfType<ToolStripItem>().Select(i => i is ToolStripSeparator ? "—" : i.Text))}");
        Capture(Rectangle.Inflate(menu.Bounds, 6, 6), Path.Combine(folder, "2-meny-pa-skjerm.png"));

        // Åpne undermenyen Moduser og ta skjermdump av begge.
        var modesItem = (ToolStripMenuItem)menu.Items.OfType<ToolStripMenuItem>().First(i => i.Text == Loc.T("tray.modes"));
        modesItem.ShowDropDown();
        Application.DoEvents(); Thread.Sleep(300); Application.DoEvents();
        Capture(Rectangle.Union(Rectangle.Inflate(menu.Bounds, 6, 6), Rectangle.Inflate(modesItem.DropDown.Bounds, 6, 6)),
            Path.Combine(folder, "3-meny-med-moduser.png"));
        modesItem.HideDropDown();
        menu.Close();
        Application.DoEvents();

        // 3) Klikk menyvalg gjennom menyens eget grensesnitt og les tilstanden.
        ToolStripMenuItem Find(string? parentText, string text)
        {
            trayMenu.Build();
            var items = parentText == null ? menu.Items
                : menu.Items.OfType<ToolStripMenuItem>().First(i => i.Text == parentText).DropDownItems;
            return items.OfType<ToolStripMenuItem>().First(i => i.Text == text);
        }
        string State() => $"filter={app.Settings.FilterOn} K={app.Settings.Kelvin} lys={app.Settings.BrightnessPercent} " +
                          $"modus={app.Settings.Modes.FirstOrDefault(m => m.IsActive)?.Id ?? "-"} språk={Loc.Current} tooltip=\"{tray.Text}\"";

        var night = app.Settings.Modes.First(m => m.Id == "night");
        Find(Loc.T("tray.modes"), night.DisplayName).PerformClick();
        Log("klikk Moduser > " + night.DisplayName + ": " + State());

        Find(Loc.T("tray.temperature"), $"2000 K · {ColorPreview.Describe(2000)}").PerformClick();
        Log("klikk Fargetemperatur > 2000 K: " + State());

        Find(Loc.T("tray.brightness"), "50 %").PerformClick();
        Log("klikk Lysstyrke > 50 %: " + State());

        Find(null, Loc.T("tray.filter")).PerformClick();
        Log("klikk Filter på (av): " + State());
        Find(null, Loc.T("tray.filter")).PerformClick();
        Log("klikk Filter på (på igjen): " + State());

        string other = Loc.Current == "en" ? "Norsk" : "English";
        Find(Loc.T("win.language"), other).PerformClick();
        trayMenu.Build();
        Log($"klikk Språk > {other}: {State()}, menytekster nå: {string.Join(" | ", menu.Items.OfType<ToolStripMenuItem>().Select(i => i.Text))}");

        var check = Find(Loc.T("tray.modes"), night.DisplayName);
        Log($"hake etter manuell endring på Natt/Night: {check.Checked} (forventet False, verdiene er egendefinerte)");

        // 4) Feilmåte: lekker ikonet GDI-håndtak ved mange fargeendringer (som under en overgang)?
        var proc = System.Diagnostics.Process.GetCurrentProcess().Handle;
        uint gdiBefore = GetGuiResources(proc, 0), userBefore = GetGuiResources(proc, 1);
        for (int i = 0; i < 300; i++) app.SetManual(1000 + i * 25, 20 + i % 80);
        GC.Collect(); GC.WaitForPendingFinalizers();
        uint gdiAfter = GetGuiResources(proc, 0), userAfter = GetGuiResources(proc, 1);
        Log($"300 fargeendringer: GDI {gdiBefore} -> {gdiAfter}, USER {userBefore} -> {userAfter}");

        File.WriteAllLines(Path.Combine(folder, "selvtest.txt"), log);
        app.Quit();
    }

    private static Rectangle? FindTrayRect(NotifyIcon tray)
    {
        // NotifyIcon har et skjult vindu og en id; hent dem via refleksjon (kun for testing).
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var fields = typeof(NotifyIcon).GetFields(flags);
        var window = fields.Select(f => f.GetValue(tray)).OfType<NativeWindow>().FirstOrDefault();
        var idField = fields.FirstOrDefault(f => f.Name == "_id");
        var added = fields.FirstOrDefault(f => f.Name == "_added")?.GetValue(tray);
        if (window == null || idField?.GetValue(tray) is not uint id)
        {
            LastError = "felter: " + string.Join(", ", fields.Select(f => $"{f.FieldType.Name} {f.Name}"));
            return null;
        }
        LastError = $"_added={added}";
        var nid = new NOTIFYICONIDENTIFIER { cbSize = (uint)Marshal.SizeOf<NOTIFYICONIDENTIFIER>(), hWnd = window.Handle, uID = id };
        int hr = Shell_NotifyIconGetRect(ref nid, out var r);
        if (hr != 0) { LastError = $"Shell_NotifyIconGetRect hr=0x{hr:X8} hwnd={window.Handle} id={id}"; return null; }
        return Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
    }

    private static string? LastError;

    private static void Capture(Rectangle r, string path)
    {
        if (r.Width <= 0 || r.Height <= 0) return;
        using var bmp = new Bitmap(r.Width, r.Height);
        using (var g = Graphics.FromImage(bmp)) g.CopyFromScreen(r.Location, Point.Empty, r.Size);
        bmp.Save(path, ImageFormat.Png);
    }
}
