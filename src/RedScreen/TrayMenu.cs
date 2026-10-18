using System.Drawing;
using System.Windows.Forms;

namespace RedScreen;

/// <summary>
/// Høyreklikkmenyen på tray-ikonet. Bygges på nytt hver gang den åpnes,
/// så den alltid viser gjeldende modus, verdier og språk.
/// </summary>
internal sealed class TrayMenu
{
    private readonly App _app;
    public ContextMenuStrip Menu { get; }

    /// <summary>Forhåndsvalg for fargetemperatur i menyen.</summary>
    private static readonly int[] KelvinPresets = { 6500, 5500, 4500, 3400, 2700, 2000, 1200, 500 };
    private static readonly int[] BrightnessPresets = { 100, 90, 80, 70, 60, 50, 40, 30, 20 };

    public TrayMenu(App app)
    {
        _app = app;
        Menu = new ContextMenuStrip
        {
            Renderer = new DarkMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = true,
            Padding = new Padding(0, 5, 0, 5),
            Font = new Font("Segoe UI", 9.75f),
        };
        DarkMenuRenderer.RoundCorners(Menu);
        Menu.Opening += (_, _) => Build();
        Build();
    }

    public void Build()
    {
        var s = _app.Settings;
        Menu.SuspendLayout();
        Menu.Items.Clear();

        var open = Item(Loc.T("tray.open"), () => _app.ShowSettings());
        open.Font = new Font(Menu.Font, FontStyle.Bold);
        Menu.Items.Add(open);
        Menu.Items.Add(new ToolStripSeparator());

        Menu.Items.Add(Item(Loc.T("tray.filter"), () => _app.SetFilterOn(!s.FilterOn), s.FilterOn));

        // Moduser
        var modes = Sub(Loc.T("tray.modes"));
        string? activeId = s.Modes.FirstOrDefault(m => m.IsActive)?.Id;
        foreach (var mode in s.Modes)
        {
            var m = mode;
            var item = Item(m.DisplayName, () => _app.ActivateMode(m), m.Id == activeId);
            item.Image = ColorDot(m.FilterOn, m.Kelvin, m.BrightnessPercent);
            modes.DropDownItems.Add(item);
        }
        if (s.Modes.Count == 0)
            modes.DropDownItems.Add(new ToolStripMenuItem(Loc.T("tray.noModes")) { Enabled = false, Padding = new Padding(0, 3, 0, 3) });
        Menu.Items.Add(modes);

        // Fargetemperatur
        var temp = Sub(Loc.T("tray.temperature"));
        foreach (int k in KelvinPresets)
        {
            int kelvin = k;
            var item = Item($"{k} K · {ColorPreview.Describe(k)}",
                () => _app.SetManual(kelvin, s.BrightnessPercent), s.FilterOn && s.Kelvin == k);
            item.Image = ColorDot(true, k, 100);
            temp.DropDownItems.Add(item);
        }
        Menu.Items.Add(temp);

        // Lysstyrke
        var bright = Sub(Loc.T("tray.brightness"));
        foreach (int b in BrightnessPresets)
        {
            int brightness = b;
            bright.DropDownItems.Add(Item($"{b} %",
                () => _app.SetManual(s.Kelvin, brightness), s.FilterOn && s.BrightnessPercent == b));
        }
        Menu.Items.Add(bright);

        Menu.Items.Add(new ToolStripSeparator());

        // Språk
        var lang = Sub(Loc.T("win.language"));
        foreach (var (code, name, _) in Loc.Languages)
        {
            string c = code;
            lang.DropDownItems.Add(Item(name, () => _app.SetLanguage(c), Loc.Current == c));
        }
        Menu.Items.Add(lang);
        Menu.Items.Add(Item(Loc.T("tray.autostart"), () => Autostart.Set(!Autostart.IsEnabled), Autostart.IsEnabled));

        Menu.Items.Add(new ToolStripSeparator());
        Menu.Items.Add(Item(Loc.T("tray.quit"), () => _app.Quit()));

        Menu.ResumeLayout();
    }

    private ToolStripMenuItem Item(string text, Action onClick, bool isChecked = false)
    {
        var item = new ToolStripMenuItem(text) { Checked = isChecked, Padding = new Padding(0, 3, 0, 3) };
        item.Click += (_, _) => onClick();
        return item;
    }

    private ToolStripMenuItem Sub(string text)
    {
        var item = new ToolStripMenuItem(text) { Padding = new Padding(0, 3, 0, 3) };
        var dd = (ToolStripDropDownMenu)item.DropDown;
        dd.Renderer = Menu.Renderer;
        dd.ShowImageMargin = true;
        dd.ShowCheckMargin = true;
        dd.Padding = new Padding(0, 5, 0, 5);
        dd.Font = Menu.Font;
        DarkMenuRenderer.RoundCorners(dd);
        return item;
    }

    /// <summary>Liten fargeprikk ved moduser og temperaturer.</summary>
    private static Bitmap ColorDot(bool filterOn, int kelvin, int brightness)
    {
        var c = ColorPreview.ToColor(filterOn, kelvin, brightness);
        var bmp = new Bitmap(14, 14);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Color.FromArgb(c.R, c.G, c.B));
        g.FillEllipse(brush, 1, 1, 12, 12);
        return bmp;
    }
}
