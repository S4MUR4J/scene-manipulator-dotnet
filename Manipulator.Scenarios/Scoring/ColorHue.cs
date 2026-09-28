namespace Manipulator.Scenarios.Scoring;

/// <summary>Extracts HSL hue (degrees, [0, 360)) from a "#rrggbb" color, for hue-range constraints.</summary>
public static class ColorHue
{
    /// <summary>Grey (R == G == B) has no defined hue; this returns 0 by convention.</summary>
    public static double FromHex(string hex)
    {
        var value = hex.TrimStart('#');
        var r = Convert.ToInt32(value.Substring(0, 2), 16) / 255.0;
        var g = Convert.ToInt32(value.Substring(2, 2), 16) / 255.0;
        var b = Convert.ToInt32(value.Substring(4, 2), 16) / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        if (delta == 0)
            return 0.0;

        var hue =
            max == r ? 60.0 * ((g - b) / delta)
            : max == g ? 60.0 * ((b - r) / delta + 2.0)
            : 60.0 * ((r - g) / delta + 4.0);

        return hue < 0 ? hue + 360.0 : hue;
    }
}
