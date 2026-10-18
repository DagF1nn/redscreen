using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RedScreen;

/// <summary>
/// Utviklerverktøy: tegner innstillingsvinduet til PNG-filer, så utseendet
/// kan sjekkes uten å klikke rundt. Kjøres med «--snapshot &lt;mappe&gt;».
/// </summary>
internal static class Snapshot
{
    public static void Run(App app, string folder)
    {
        Directory.CreateDirectory(folder);

        var window = new SettingsWindow(app)
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000, // utenfor skjermen
            Top = 0,
            ShowActivated = false,
        };
        window.Show();

        void Render(string name) => RenderElement(window, name);

        void RenderElement(FrameworkElement element, string name)
        {
            element.UpdateLayout();
            var dpi = VisualTreeHelper.GetDpi(element);
            var bmp = new RenderTargetBitmap(
                Math.Max(1, (int)(element.ActualWidth * dpi.DpiScaleX)), Math.Max(1, (int)(element.ActualHeight * dpi.DpiScaleY)),
                dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            bmp.Render(element);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using var fs = File.Create(Path.Combine(folder, name));
            enc.Save(fs);
        }

        // Gi WPF tid til å tegne mellom hvert steg.
        var steps = new Queue<Action>(new Action[]
        {
            // Ny bruker: tomme lister.
            () => Render("0a-ny-bruker-moduser.png"),
            () => window.ShowTab(automations: true),
            () => Render("0b-ny-bruker-automasjoner.png"),
            // Fyll med eksempeldata og vis en kveldsmodus valgt for hånd.
            () =>
            {
                SampleData.Fill(app.Settings, Loc.Current == "nb");
                app.CheckAutomations();
                app.ActivateMode(app.Settings.Modes.First(m => m.Id == "night"));
                window.ShowTab(automations: false);
            },
            () => Render("1-moduser.png"),
            () => window.ShowTab(automations: true),
            () => Render("2-automasjoner.png"),
            // Flyt: lagre ny modus og legg til automasjon som bruker den.
            () =>
            {
                app.SetManual(2200, 60);
                var reading = app.AddMode(Loc.Current == "nb" ? "Lesing" : "Reading");
                var a = app.AddAutomation();
                a.ModeId = reading.Id;
                a.TimeText = "2330";
                a.Saturday = false;
                a.Sunday = false;
                a.FadeMinutes = 30;
                app.Settings.SortAutomations();
                window.ScrollToEnd();
            },
            () => Render("3-ny-automasjon.png"),
            () => { window.ShowTab(automations: false); window.ScrollToEnd(); },
            () => Render("4-ny-modus.png"),
            // Språkbytte mens appen kjører: alt skal oppdateres uten omstart.
            () => app.SetLanguage(Loc.Current == "nb" ? "en" : "nb"),
            () => Render("5-etter-sprakbytte.png"),
            () => { window.ShowTab(automations: true); window.ScrollToEnd(); },
            () => Render("6-automasjoner-etter-bytte.png"),
            () => window.OpenLanguageMenu(),
            () => RenderElement(window.LanguageMenuElement, "7-sprakmeny.png"),
            () => window.LanguageMenuElement.IsOpen = false,
            () => app.Shutdown(),
        });
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        timer.Tick += (_, _) =>
        {
            if (steps.Count == 0) { timer.Stop(); return; }
            steps.Dequeue()();
        };
        timer.Start();
    }
}
