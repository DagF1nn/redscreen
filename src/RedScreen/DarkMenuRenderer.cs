using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RedScreen;

/// <summary>
/// Mørk tray-meny i samme stil som vinduet: avrundede hjørner, avrundet markering,
/// aksentfarget hake. Bygger på WinForms-menyen, så Windows plasserer den riktig ved ikonet.
/// </summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public static readonly Color Back = Color.FromArgb(36, 36, 42);
    public static readonly Color Hover = Color.FromArgb(52, 52, 60);
    public static readonly Color Line = Color.FromArgb(52, 52, 60);
    public static readonly Color Text = Color.FromArgb(242, 242, 245);
    public static readonly Color SubText = Color.FromArgb(154, 154, 165);
    public static readonly Color Accent = Color.FromArgb(255, 107, 74);

    public DarkMenuRenderer() : base(new DarkColors()) { RoundedEdges = false; }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) =>
        e.Graphics.Clear(Back);

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        // Windows 11 tegner avrundet kant selv (se RoundCorners). Ingen firkantet kant her.
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var b = new SolidBrush(Back);
        e.Graphics.FillRectangle(b, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected || !e.Item.Enabled) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = new Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
        using var path = Rounded(r, 6);
        using var b = new SolidBrush(Hover);
        g.FillPath(b, path);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Text : SubText;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = e.ImageRectangle;
        using var pen = new Pen(Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
        g.DrawLines(pen, new[]
        {
            new PointF(cx - 4.5f, cy + 0.2f),
            new PointF(cx - 1.5f, cy + 3.2f),
            new PointF(cx + 4.5f, cy - 3.2f),
        });
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = SubText;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using var pen = new Pen(Line);
        e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        int d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    private sealed class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Back;
        public override Color ImageMarginGradientBegin => Back;
        public override Color ImageMarginGradientMiddle => Back;
        public override Color ImageMarginGradientEnd => Back;
        public override Color MenuBorder => Back;
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Hover;
        public override Color SeparatorDark => Line;
        public override Color SeparatorLight => Line;
    }

    // ===== Windows 11: avrundede hjørner på menyvinduet =====

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    /// <summary>Gir menyen (og undermenyer) runde hjørner på Windows 11. Ignoreres på eldre Windows.</summary>
    public static void RoundCorners(ToolStripDropDown dropDown)
    {
        void Apply()
        {
            int pref = DWMWCP_ROUND;
            try { DwmSetWindowAttribute(dropDown.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int)); }
            catch (DllNotFoundException) { }
        }
        if (dropDown.IsHandleCreated) Apply();
        dropDown.HandleCreated += (_, _) => Apply();
    }
}
