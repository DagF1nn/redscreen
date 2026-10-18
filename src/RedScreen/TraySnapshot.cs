using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace RedScreen;

/// <summary>
/// Utviklerverktøy: tegner tray-menyen og ikonene til PNG, og skriver app.ico.
/// Kjøres med «--tray-snapshot &lt;mappe&gt;». Endrer ikke skjermen eller innstillingene.
/// </summary>
internal static class TraySnapshot
{
    public static void Run(App app, string folder)
    {
        Directory.CreateDirectory(folder);

        // Ikoner: tray i flere tilstander, i forstørret visning så de kan vurderes.
        var states = new (string Name, bool On, int K, int B)[]
        {
            ("av", false, 6500, 100), ("kveld", true, 4500, 90), ("natt", true, 3000, 70),
            ("rodt", true, 1200, 50), ("helt-rodt", true, 500, 40),
        };
        foreach (var bg in new[] { ("mork", Color.FromArgb(32, 32, 32)), ("lys", Color.FromArgb(243, 243, 243)) })
        {
            using var sheet = new Bitmap(states.Length * 80 + 20, 100);
            using var g = Graphics.FromImage(sheet);
            g.Clear(bg.Item2);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            for (int i = 0; i < states.Length; i++)
            {
                var st = states[i];
                using var small = IconFactory.DrawTray(16, st.On, st.K, st.B);
                using var big = IconFactory.DrawTray(32, st.On, st.K, st.B);
                g.DrawImage(small, 20 + i * 80, 10, 16, 16);          // faktisk størrelse
                g.DrawImage(big, 20 + i * 80, 40, 48, 48);            // 32 px, forstørret
            }
            sheet.Save(Path.Combine(folder, $"tray-ikoner-{bg.Item1}.png"), ImageFormat.Png);
        }

        using (var appIcon = IconFactory.DrawApp(256)) appIcon.Save(Path.Combine(folder, "app-256.png"), ImageFormat.Png);
        IconFactory.SaveAppIco(Path.Combine(folder, "app.ico"));

        // Menyen: tegn hovedmenyen og én undermeny til bilde.
        SampleData.Fill(app.Settings, Loc.Current == "nb");
        var mode = app.Settings.Modes.First(m => m.Id == "night");
        app.ActivateMode(mode);
        var menu = app.TrayMenuForTest!.Menu;
        app.TrayMenuForTest.Build();
        RenderMenu(menu, Path.Combine(folder, "meny.png"));

        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripMenuItem { HasDropDownItems: true } sub)
            {
                string name = sub.Text!.ToLowerInvariant().Replace(' ', '-');
                RenderMenu(sub.DropDown, Path.Combine(folder, $"undermeny-{name}.png"));
            }
        }

        app.Shutdown();
    }

    private static void RenderMenu(ToolStripDropDown menu, string path)
    {
        menu.CreateControl();
        var size = menu.GetPreferredSize(Size.Empty);
        menu.Size = size;
        menu.PerformLayout();
        using var bmp = new Bitmap(size.Width, size.Height);
        menu.DrawToBitmap(bmp, new Rectangle(Point.Empty, size));
        bmp.Save(path, ImageFormat.Png);
    }
}
