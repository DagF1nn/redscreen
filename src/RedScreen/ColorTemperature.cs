namespace RedScreen;

/// <summary>
/// Gjør om fargetemperatur (Kelvin) til RGB-multiplikatorer 0..1.
/// Basert på Tanner Hellands tilnærming til svart-legeme-stråling.
/// 6500 K = nøytral (ingen endring), lavere = varmere/rødere, høyere = kaldere/blåere.
/// Under 1000 K glir vi videre mot helt rødt (bare rød kanal igjen ved 500 K).
/// </summary>
public static class ColorTemperature
{
    public const int Min = 500;
    public const int Max = 10000;
    public const int Neutral = 6500;

    private const int FormulaMin = 1000;

    public static (double R, double G, double B) ToRgb(int kelvin)
    {
        kelvin = Math.Clamp(kelvin, Min, Max);

        if (kelvin == Neutral) return (1, 1, 1);

        if (kelvin < FormulaMin)
        {
            // Lineært fra 1000 K-fargen ned til rent rødt ved 500 K.
            var (r, g, b) = Formula(FormulaMin);
            double f = (kelvin - Min) / (double)(FormulaMin - Min); // 0 ved 500 K, 1 ved 1000 K
            return (r, g * f, b * f);
        }

        if (kelvin < Neutral) return Formula(kelvin);

        // Kaldere enn nøytral: senk rød og grønn i forhold til 6500 K,
        // slik at overgangen ved 6500 K blir sømløs.
        var (rk, gk, bk) = Formula(kelvin);
        var (rn, gn, bn) = Formula(Neutral);
        return (Math.Min(1, rk / rn), Math.Min(1, gk / gn), Math.Min(1, bk / bn));
    }

    private static (double R, double G, double B) Formula(int kelvin)
    {
        double t = kelvin / 100.0;
        double r, g, b;

        if (t <= 66)
        {
            r = 255;
            g = 99.4708025861 * Math.Log(t) - 161.1195681661;
            b = t <= 19 ? 0 : 138.5177312231 * Math.Log(t - 10) - 305.0447927307;
        }
        else
        {
            r = 329.698727446 * Math.Pow(t - 60, -0.1332047592);
            g = 288.1221695283 * Math.Pow(t - 60, -0.0755148492);
            b = 255;
        }

        return (Norm(r), Norm(g), Norm(b));
    }

    private static double Norm(double v) => Math.Clamp(v, 0, 255) / 255.0;
}
