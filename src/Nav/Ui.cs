using System.Globalization;
using System.Text;

namespace Nav;

internal static class Ui
{
    /// <summary>Recorta por la derecha con "…" si no cabe.</summary>
    public static string Clip(string text, int width)
    {
        text = Clean(text);
        if (width <= 0)
            return "";
        if (text.Length <= width)
            return text;
        return width == 1 ? "…" : text[..(width - 1)] + "…";
    }

    /// <summary>Recorta por la izquierda (util para rutas: se ve el final).</summary>
    public static string ClipLeft(string text, int width)
    {
        text = Clean(text);
        if (width <= 0)
            return "";
        if (text.Length <= width)
            return text;
        return width == 1 ? "…" : "…" + text[^(width - 1)..];
    }

    public static string Pad(string text, int width) => Clip(text, width).PadRight(Math.Max(0, width));

    public static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0
            ? $"{bytes} B"
            : value.ToString(value < 10 ? "0.0" : "0", CultureInfo.InvariantCulture) + " " + units[unit];
    }

    /// <summary>Los nombres de archivo en Unix pueden contener caracteres de control.</summary>
    private static string Clean(string text)
    {
        bool dirty = false;
        foreach (char c in text)
        {
            if (char.IsControl(c))
            {
                dirty = true;
                break;
            }
        }
        if (!dirty)
            return text;

        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
            sb.Append(char.IsControl(c) ? '?' : c);
        return sb.ToString();
    }
}
