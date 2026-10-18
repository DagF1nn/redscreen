using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;

namespace RedScreen;

/// <summary>
/// Tegner RedScreen-ikonet i kode: en rund, glødende prikk. I systemfeltet får prikken
/// fargen skjermen har akkurat nå, så man ser modusen uten å åpne noe.
/// </summary>
public static class IconFactory
{
    private static readonly Color Accent = Color.FromArgb(255, 107, 74);
    private static readonly Color Dark = Color.FromArgb(20, 20, 23);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>Tray-ikon. Filter av = hul ring, så man ser at ingenting er aktivt.</summary>
    public static Bitmap DrawTray(int size, bool filterOn, int kelvin, int brightnessPercent)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        float pad = size * 0.06f;
        var outer = new RectangleF(pad, pad, size - 2 * pad, size - 2 * pad);

        if (!filterOn)
        {
            // Hul ring i aksentfarge, med mørk kant så den synes på lys oppgavelinje.
            float ring = Math.Max(1.5f, size * 0.14f);
            var r = RectangleF.Inflate(outer, -ring / 2, -ring / 2);
            using var darkPen = new Pen(Color.FromArgb(150, Dark), ring + Math.Max(1f, size / 16f));
            using var pen = new Pen(Accent, ring);
            g.DrawEllipse(darkPen, r);
            g.DrawEllipse(pen, r);
            return bmp;
        }

        var c = ColorPreview.ToColor(true, kelvin, brightnessPercent);
        var fill = Color.FromArgb(c.R, c.G, c.B);

        // Mørk kant for kontrast, deretter fylt prikk med lys glans øverst.
        using (var edge = new SolidBrush(Color.FromArgb(170, Dark)))
            g.FillEllipse(edge, outer);
        var inner = RectangleF.Inflate(outer, -Math.Max(1f, size / 16f), -Math.Max(1f, size / 16f));
        using (var path = new GraphicsPath())
        {
            path.AddEllipse(inner);
            using var grad = new PathGradientBrush(path)
            {
                CenterPoint = new PointF(inner.X + inner.Width * 0.38f, inner.Y + inner.Height * 0.32f),
                CenterColor = Lighten(fill, 0.35f),
                SurroundColors = new[] { fill },
            };
            g.FillEllipse(grad, inner);
        }
        return bmp;
    }

    /// <summary>Appikon: glødende prikk på mørk, avrundet flate (som tittellinjen i vinduet).</summary>
    public static Bitmap DrawApp(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float radius = size * 0.22f;
        using (var bg = RoundedRect(new RectangleF(0, 0, size, size), radius))
        using (var brush = new SolidBrush(Dark))
            g.FillPath(brush, bg);

        var dot = new RectangleF(size * 0.2f, size * 0.2f, size * 0.6f, size * 0.6f);
        // Glød
        using (var glowPath = new GraphicsPath())
        {
            var glow = RectangleF.Inflate(dot, size * 0.12f, size * 0.12f);
            glowPath.AddEllipse(glow);
            using var glowBrush = new PathGradientBrush(glowPath)
            {
                CenterColor = Color.FromArgb(120, Accent),
                SurroundColors = new[] { Color.FromArgb(0, Accent) },
            };
            g.FillEllipse(glowBrush, glow);
        }
        using (var path = new GraphicsPath())
        {
            path.AddEllipse(dot);
            using var grad = new PathGradientBrush(path)
            {
                CenterPoint = new PointF(dot.X + dot.Width * 0.38f, dot.Y + dot.Height * 0.32f),
                CenterColor = Lighten(Accent, 0.3f),
                SurroundColors = new[] { Color.FromArgb(230, 60, 40) },
            };
            g.FillEllipse(grad, dot);
        }
        return bmp;
    }

    /// <summary>Lager et Icon fra en bitmap. Kopien eier sitt eget håndtak, så vi rydder opp med en gang.</summary>
    public static Icon ToIcon(Bitmap bmp)
    {
        IntPtr h = bmp.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(h);
            return (Icon)temp.Clone();
        }
        finally
        {
            DestroyIcon(h);
        }
    }

    /// <summary>Skriver en .ico med flere størrelser (PNG-komprimert), brukt for exe og vindu.</summary>
    public static void SaveAppIco(string path)
    {
        int[] sizes = { 16, 20, 24, 32, 48, 64, 128, 256 };
        var images = sizes.Select(s =>
        {
            using var bmp = DrawApp(s);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }).ToList();

        using var fs = File.Create(path);
        using var w = new BinaryWriter(fs);
        w.Write((short)0); w.Write((short)1); w.Write((short)sizes.Length);
        int offset = 6 + 16 * sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
        {
            w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
            w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
            w.Write((byte)0); w.Write((byte)0);
            w.Write((short)1); w.Write((short)32);
            w.Write(images[i].Length); w.Write(offset);
            offset += images[i].Length;
        }
        foreach (var img in images) w.Write(img);
    }

    private static Color Lighten(Color c, float amount) => Color.FromArgb(
        c.R + (int)((255 - c.R) * amount), c.G + (int)((255 - c.G) * amount), c.B + (int)((255 - c.B) * amount));

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        var p = new GraphicsPath();
        float d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
