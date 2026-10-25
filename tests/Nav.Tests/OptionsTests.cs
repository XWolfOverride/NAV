namespace Nav.Tests;

[Collection(EnvironmentCollection.Name)]
public class OptionsTests
{
    private static Options Parse(string? navopt, params string[] args)
    {
        using var env = new EnvScope().Set("NAVOPT", navopt);
        return Options.Parse(args);
    }

    [Fact]
    public void Sin_argumentos_todo_esta_desactivado()
    {
        var o = Parse(null);
        Assert.False(o.ShowHidden);
        Assert.False(o.DirsOnly);
        Assert.False(o.Help);
        Assert.False(o.Version);
        Assert.False(o.New);
        Assert.Null(o.StartDirectory);
        Assert.Null(o.CdFile);
        Assert.Empty(o.Errors);
    }

    [Fact]
    public void Letras_de_opcion_se_pueden_combinar()
    {
        var o = Parse(null, "-ad");
        Assert.True(o.ShowHidden);
        Assert.True(o.DirsOnly);
        Assert.Empty(o.Errors);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("-?")]
    [InlineData("--help")]
    public void Ayuda_se_activa_de_varias_formas(string arg) =>
        Assert.True(Parse(null, arg).Help);

    [Fact]
    public void Version_y_new()
    {
        var o = Parse(null, "--version", "--new");
        Assert.True(o.Version);
        Assert.True(o.New);
    }

    [Fact]
    public void Primer_argumento_sin_guion_es_la_ruta()
    {
        var o = Parse(null, "algo", "-a");
        Assert.Equal("algo", o.StartDirectory);
        Assert.True(o.ShowHidden);
        Assert.Empty(o.Errors);
    }

    [Fact]
    public void Dos_rutas_son_un_error()
    {
        var o = Parse(null, "uno", "dos");
        Assert.Equal("uno", o.StartDirectory);
        Assert.Single(o.Errors);
    }

    [Fact]
    public void CdFile_toma_el_valor_siguiente()
    {
        var o = Parse(null, "--cd-file", "/tmp/x", "ruta");
        Assert.Equal("/tmp/x", o.CdFile);
        Assert.Equal("ruta", o.StartDirectory);
        Assert.Empty(o.Errors);
    }

    [Fact]
    public void CdFile_sin_valor_es_un_error()
    {
        var o = Parse(null, "--cd-file");
        Assert.Null(o.CdFile);
        Assert.Single(o.Errors);
    }

    [Theory]
    [InlineData("--nada")]
    [InlineData("-z")]
    public void Opciones_desconocidas_son_un_error(string arg) =>
        Assert.Single(Parse(null, arg).Errors);

    [Theory]
    [InlineData("a", true, false)]
    [InlineData("-d", false, true)]
    [InlineData("ad", true, true)]
    [InlineData("  a  ", true, false)]
    public void NAVOPT_aplica_letras(string navopt, bool hidden, bool dirsOnly)
    {
        var o = Parse(navopt);
        Assert.Equal(hidden, o.ShowHidden);
        Assert.Equal(dirsOnly, o.DirsOnly);
        Assert.Empty(o.Errors);
    }

    [Fact]
    public void NAVOPT_y_argumentos_se_suman()
    {
        var o = Parse("a", "-d");
        Assert.True(o.ShowHidden);
        Assert.True(o.DirsOnly);
    }

    [Fact]
    public void NAVOPT_con_letra_invalida_da_error() =>
        Assert.Single(Parse("z").Errors);
}
