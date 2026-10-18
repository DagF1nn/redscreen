using System.Windows.Media;

namespace RedScreen;

/// <summary>Gjør om filterverdier til en farge for forhåndsvisning i vinduet.</summary>
public static class ColorPreview
{
    public static Color ToColor(bool filterOn, int kelvin, int brightnessPercent)
    {
        if (!filterOn) return Color.FromRgb(238, 238, 242);

        var (r, g, b) = ColorTemperature.ToRgb(kelvin);
        // Ikke la prikken bli helt svart ved lav lysstyrke.
        double f = 0.35 + 0.65 * brightnessPercent / 100.0;
        return Color.FromRgb(ToByte(r * f), ToByte(g * f), ToByte(b * f));
    }

    public static SolidColorBrush Brush(bool filterOn, int kelvin, int brightnessPercent)
    {
        var brush = new SolidColorBrush(ToColor(filterOn, kelvin, brightnessPercent));
        brush.Freeze();
        return brush;
    }

    public static string Describe(int kelvin) => Loc.T(kelvin switch
    {
        <= 600 => "temp.fullRed",
        < 6500 => "temp.warm",
        6500 => "temp.normal",
        _ => "temp.cool",
    });

    private static byte ToByte(double v) => (byte)Math.Round(Math.Clamp(v, 0, 1) * 255);
}
