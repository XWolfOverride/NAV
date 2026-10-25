using System.Text;

namespace Nav;

/// <summary>
/// Texto de un archivo listo para mostrar: detecta binarios, decodifica y deja las lineas
/// sin ningun caracter que pueda romper la terminal.
/// </summary>
internal sealed class TextDocument
{
    /// <summary>Tope de lectura: un archivo mayor se muestra truncado.</summary>
    public const int MaxBytes = 8 * 1024 * 1024;

    private const int SampleSize = 8192;
    private const int TabSize = 4;

    public TextDocument(List<string> lines, bool truncated)
    {
        Lines = lines;
        Truncated = truncated;
    }

    public List<string> Lines { get; }
    public bool Truncated { get; }

    /// <summary>Lee el archivo. Devuelve null si parece binario.</summary>
    public static TextDocument? Load(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        while (buffer.Length <= MaxBytes)
        {
            int read = stream.Read(chunk, 0, (int)Math.Min(chunk.Length, MaxBytes + 1 - buffer.Length));
            if (read <= 0)
                break;
            buffer.Write(chunk, 0, read);
        }

        byte[] bytes = buffer.GetBuffer();
        int length = (int)buffer.Length;
        bool truncated = length > MaxBytes;
        if (truncated)
            length = TrimIncompleteUtf8(bytes, MaxBytes);

        if (LooksBinary(bytes.AsSpan(0, Math.Min(length, SampleSize))))
            return null;

        return new TextDocument(Split(Decode(bytes, length)), truncated);
    }

    /// <summary>Binario si hay bytes NUL o muchos caracteres de control (salvo texto UTF-16 con BOM).</summary>
    public static bool LooksBinary(ReadOnlySpan<byte> sample)
    {
        if (sample.IsEmpty)
            return false;
        if (sample.Length >= 2 && ((sample[0] == 0xFF && sample[1] == 0xFE) || (sample[0] == 0xFE && sample[1] == 0xFF)))
            return false;

        int control = 0;
        foreach (byte b in sample)
        {
            if (b == 0)
                return true;
            // Se toleran tabulador, saltos de linea, salto de pagina, retroceso y ESC (logs con colores).
            if (b < 0x20 && b is not (0x09 or 0x0A or 0x0C or 0x0D or 0x08 or 0x1B))
                control++;
        }
        return control * 10 > sample.Length;
    }

    /// <summary>Quita por el final un caracter UTF-8 que haya quedado cortado.</summary>
    private static int TrimIncompleteUtf8(byte[] bytes, int length)
    {
        for (int back = 1; back <= 3 && back <= length; back++)
        {
            byte b = bytes[length - back];
            if ((b & 0xC0) == 0x80)
                continue; // byte de continuacion: seguir buscando el inicial
            int expected = b >= 0xF0 ? 4 : b >= 0xE0 ? 3 : b >= 0xC0 ? 2 : 1;
            return expected > back ? length - back : length;
        }
        return length;
    }

    private static string Decode(byte[] bytes, int length)
    {
        int start = 0;
        Encoding encoding;
        if (length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            start = 3;
            encoding = new UTF8Encoding(false, true);
        }
        else if (length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            start = 2;
            encoding = Encoding.Unicode;
        }
        else if (length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            start = 2;
            encoding = Encoding.BigEndianUnicode;
        }
        else
        {
            encoding = new UTF8Encoding(false, true);
        }

        try
        {
            return encoding.GetString(bytes, start, length - start);
        }
        catch (DecoderFallbackException)
        {
            // No es UTF-8 valido: se asume una codificacion de un byte por caracter.
            return Encoding.Latin1.GetString(bytes, start, length - start);
        }
    }

    private static List<string> Split(string text)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = new List<string>();
        int start = 0;
        while (start < text.Length)
        {
            int end = text.IndexOf('\n', start);
            if (end < 0)
                end = text.Length;
            lines.Add(Sanitize(text.AsSpan(start, end - start)));
            start = end + 1;
        }
        return lines;
    }

    /// <summary>
    /// Expande tabuladores y sustituye por '?' todo lo que pueda alterar la terminal:
    /// caracteres de control (incluido ESC), separadores de linea Unicode, controles de
    /// direccion del texto, marca de orden de bytes y sustitutos sueltos.
    /// </summary>
    public static string Sanitize(ReadOnlySpan<char> line)
    {
        var sb = new StringBuilder(line.Length);
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\t')
            {
                sb.Append(' ', TabSize - sb.Length % TabSize);
            }
            else if (char.IsHighSurrogate(c) && i + 1 < line.Length && char.IsLowSurrogate(line[i + 1]))
            {
                sb.Append(c).Append(line[++i]);
            }
            else if (char.IsControl(c) || char.IsSurrogate(c) || IsUnsafeFormat(c))
            {
                sb.Append('?');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>Separadores de linea Unicode, BOM y controles de direccion (LRM, RLM, LRE..RLO, LRI..PDI).</summary>
    private static bool IsUnsafeFormat(char c) =>
        c is (char)0x2028 or (char)0x2029 or (char)0xFEFF or (char)0x200E or (char)0x200F
        or (>= (char)0x202A and <= (char)0x202E) or (>= (char)0x2066 and <= (char)0x2069);

    /// <summary>
    /// Divide las lineas en filas de como mucho <paramref name="width"/> caracteres.
    /// <paramref name="lineStarts"/> recibe la primera fila de cada linea original.
    /// </summary>
    public static List<string> Wrap(IReadOnlyList<string> lines, int width, out List<int> lineStarts)
    {
        width = Math.Max(1, width);
        var rows = new List<string>(lines.Count);
        lineStarts = new List<int>(lines.Count);
        foreach (var line in lines)
        {
            lineStarts.Add(rows.Count);
            if (line.Length == 0)
            {
                rows.Add("");
                continue;
            }
            int pos = 0;
            while (pos < line.Length)
            {
                int take = Math.Min(width, line.Length - pos);
                // No partir un par de sustitutos (emoji, etc.) por la mitad.
                if (pos + take < line.Length && take > 1 && char.IsHighSurrogate(line[pos + take - 1]))
                    take--;
                rows.Add(line.Substring(pos, take));
                pos += take;
            }
        }
        return rows;
    }
}
