using System.Text;

namespace Nav.Tests;

public class TextDocumentTests
{
    private static TextDocument? LoadBytes(byte[] content)
    {
        using var dir = new TempDir();
        string path = Path.Combine(dir.Path, "f");
        File.WriteAllBytes(path, content);
        return TextDocument.Load(path);
    }

    private static TextDocument LoadText(string text) =>
        LoadBytes(Encoding.UTF8.GetBytes(text))!;

    // ------------------------------------------------------------ binarios

    [Fact]
    public void Un_byte_nulo_indica_binario() =>
        Assert.Null(LoadBytes(new byte[] { 0x41, 0x00, 0x42 }));

    [Fact]
    public void Muchos_caracteres_de_control_indican_binario()
    {
        var bytes = Enumerable.Range(0, 200).Select(i => (byte)(i % 8 + 1)).ToArray();
        Assert.Null(LoadBytes(bytes));
    }

    [Fact]
    public void Un_archivo_vacio_es_texto_sin_lineas()
    {
        var doc = LoadBytes(Array.Empty<byte>());
        Assert.NotNull(doc);
        Assert.Empty(doc!.Lines);
    }

    [Fact]
    public void Texto_con_colores_ansi_no_es_binario() =>
        Assert.NotNull(LoadText("\u001b[31mrojo\u001b[0m\nlinea\n"));

    [Fact]
    public void Utf16_con_bom_es_texto()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("hola\nmundo")).ToArray();
        var doc = LoadBytes(bytes);
        Assert.NotNull(doc);
        Assert.Equal(new[] { "hola", "mundo" }, doc!.Lines);
    }

    // ----------------------------------------------------------- decodificar

    [Fact]
    public void Divide_en_lineas_con_cualquier_salto()
    {
        var doc = LoadText("a\r\nb\rc\nd");
        Assert.Equal(new[] { "a", "b", "c", "d" }, doc.Lines);
    }

    [Fact]
    public void El_salto_final_no_crea_una_linea_vacia_extra()
    {
        Assert.Equal(new[] { "a", "b" }, LoadText("a\nb\n").Lines);
        Assert.Equal(new[] { "a", "", "b" }, LoadText("a\n\nb").Lines);
    }

    [Fact]
    public void Quita_el_bom_utf8_y_conserva_acentos()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("camión ñ")).ToArray();
        Assert.Equal(new[] { "camión ñ" }, LoadBytes(bytes)!.Lines);
    }

    [Fact]
    public void Si_no_es_utf8_valido_se_lee_como_latin1()
    {
        var doc = LoadBytes(new byte[] { 0x63, 0x61, 0x6D, 0x69, 0xF3, 0x6E }); // "camión" en Latin-1
        Assert.Equal(new[] { "camión" }, doc!.Lines);
    }

    [Fact]
    public void Un_archivo_grande_se_trunca_sin_cortar_un_caracter()
    {
        // Una "ñ" (2 bytes) en la frontera del limite.
        var bytes = new byte[TextDocument.MaxBytes + 10];
        Array.Fill(bytes, (byte)'a');
        bytes[TextDocument.MaxBytes - 1] = 0xC3;
        bytes[TextDocument.MaxBytes] = 0xB1;

        var doc = LoadBytes(bytes)!;

        Assert.True(doc.Truncated);
        Assert.DoesNotContain('�', doc.Lines[0]);
        Assert.DoesNotContain('?', doc.Lines[0]);
    }

    // ------------------------------------------------------------ sanitizar

    [Fact]
    public void Los_caracteres_de_control_se_sustituyen()
    {
        string result = TextDocument.Sanitize("a\u001b[2Jb\u0007c\u007fd\u0085e");
        Assert.Equal("a?[2Jb?c?d?e", result);
        Assert.DoesNotContain(result, ch => char.IsControl(ch));
    }

    [Theory]
    [InlineData(0x2028)]
    [InlineData(0x2029)]
    [InlineData(0x202E)] // anulacion de direccion: puede invertir lo que se ve
    [InlineData(0x2066)]
    [InlineData(0xFEFF)]
    public void Los_caracteres_unicode_peligrosos_se_sustituyen(int code) =>
        Assert.Equal("a?b", TextDocument.Sanitize($"a{(char)code}b"));

    [Fact]
    public void Los_tabuladores_se_expanden_a_multiplos_de_cuatro()
    {
        Assert.Equal("a   b", TextDocument.Sanitize("a\tb"));
        Assert.Equal("    b", TextDocument.Sanitize("\tb"));
        Assert.Equal("abcd    e", TextDocument.Sanitize("abcd\te"));
    }

    [Fact]
    public void Los_pares_de_sustitutos_validos_se_conservan_y_los_sueltos_no()
    {
        Assert.Equal("a😀b", TextDocument.Sanitize("a😀b"));
        Assert.Equal("a?b", TextDocument.Sanitize("a\uD83Db"));
        Assert.Equal("a?b", TextDocument.Sanitize("a\uDE00b"));
    }

    [Fact]
    public void Un_archivo_con_secuencias_de_escape_no_las_deja_pasar()
    {
        var doc = LoadText("\u001b]0;titulo\u0007\u001b[31mtexto\u001b[0m\n");
        Assert.All(doc.Lines, l => Assert.DoesNotContain(l, ch => char.IsControl(ch)));
    }

    // ----------------------------------------------------------------- wrap

    [Fact]
    public void Wrap_parte_las_lineas_largas_y_guarda_las_vacias()
    {
        var rows = TextDocument.Wrap(new[] { "abcdefg", "", "xy" }, 3, out var starts);

        Assert.Equal(new[] { "abc", "def", "g", "", "xy" }, rows);
        Assert.Equal(new[] { 0, 3, 4 }, starts);
    }

    [Fact]
    public void Wrap_con_ancho_cero_se_trata_como_uno()
    {
        var rows = TextDocument.Wrap(new[] { "ab" }, 0, out _);
        Assert.Equal(new[] { "a", "b" }, rows);
    }

    [Fact]
    public void Wrap_no_parte_un_par_de_sustitutos()
    {
        var rows = TextDocument.Wrap(new[] { "a😀b" }, 2, out _);

        Assert.Equal("a😀b", string.Concat(rows));
        Assert.All(rows, r => Assert.False(r.Length > 0 && char.IsHighSurrogate(r[^1])));
    }
}
