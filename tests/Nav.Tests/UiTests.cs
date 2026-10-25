namespace Nav.Tests;

public class UiTests
{
    [Theory]
    [InlineData("hola", 10, "hola")]
    [InlineData("hola", 4, "hola")]
    [InlineData("holamundo", 5, "hola…")]
    [InlineData("holamundo", 1, "…")]
    [InlineData("hola", 0, "")]
    [InlineData("hola", -3, "")]
    public void Clip_recorta_por_la_derecha(string text, int width, string expected) =>
        Assert.Equal(expected, Ui.Clip(text, width));

    [Theory]
    [InlineData("/home/ivan/proyectos", 30, "/home/ivan/proyectos")]
    [InlineData("/home/ivan/proyectos", 10, "…proyectos")]
    [InlineData("/home/ivan/proyectos", 1, "…")]
    [InlineData("/home/ivan/proyectos", 0, "")]
    public void ClipLeft_conserva_el_final(string text, int width, string expected) =>
        Assert.Equal(expected, Ui.ClipLeft(text, width));

    [Fact]
    public void Clip_sustituye_caracteres_de_control()
    {
        Assert.Equal("a?b?c", Ui.Clip("a\nb\tc", 10));
    }

    [Fact]
    public void Pad_rellena_y_recorta_al_ancho_exacto()
    {
        Assert.Equal("ab   ", Ui.Pad("ab", 5));
        Assert.Equal("abc…", Ui.Pad("abcdef", 4));
        Assert.Equal(5, Ui.Pad("ab", 5).Length);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(10 * 1024, "10 KB")]
    [InlineData(1024L * 1024, "1.0 MB")]
    [InlineData(5L * 1024 * 1024 * 1024, "5.0 GB")]
    public void FormatSize_usa_la_unidad_adecuada(long bytes, string expected) =>
        Assert.Equal(expected, Ui.FormatSize(bytes));
}
