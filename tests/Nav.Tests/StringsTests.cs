namespace Nav.Tests;

[Collection(EnvironmentCollection.Name)]
public class StringsTests
{
    private static void Detect(string? navLang, string? lcAll = null, string? lcMessages = null, string? lang = null)
    {
        using var env = new EnvScope()
            .Set("NAV_LANG", navLang)
            .Set("LC_ALL", lcAll)
            .Set("LC_MESSAGES", lcMessages)
            .Set("LANG", lang);
        Strings.Detect();
    }

    [Theory]
    [InlineData("es", true)]
    [InlineData("ES", true)]
    [InlineData("es_ES.UTF-8", true)]
    [InlineData("en", false)]
    [InlineData("fr_FR", false)]
    public void NAV_LANG_decide_el_idioma(string value, bool spanish)
    {
        Detect(value);
        Assert.Equal(spanish, Strings.Spanish);
    }

    [Fact]
    public void NAV_LANG_tiene_prioridad_sobre_el_locale()
    {
        Detect("en", lcAll: "es_ES.UTF-8", lang: "es_ES.UTF-8");
        Assert.False(Strings.Spanish);

        Detect("es", lcAll: "en_US.UTF-8", lang: "en_US.UTF-8");
        Assert.True(Strings.Spanish);
    }

    [Fact]
    public void Se_usa_el_orden_LC_ALL_LC_MESSAGES_LANG()
    {
        Detect(null, lcAll: "es_ES", lcMessages: "en_US", lang: "en_US");
        Assert.True(Strings.Spanish);

        Detect(null, lcAll: null, lcMessages: "en_US", lang: "es_ES");
        Assert.False(Strings.Spanish);

        Detect(null, lcAll: "  ", lcMessages: null, lang: "es_ES");
        Assert.True(Strings.Spanish);
    }

    [Fact]
    public void Los_textos_cambian_con_el_idioma()
    {
        Detect("es");
        Assert.Equal("Carpeta", Strings.Kind(EntryKind.Directory));
        Assert.Contains("uso:", Strings.Usage);

        Detect("en");
        Assert.Equal("Folder", Strings.Kind(EntryKind.Directory));
        Assert.Contains("usage:", Strings.Usage);
    }

    [Fact]
    public void BackToNav_muestra_el_codigo_solo_si_no_es_cero()
    {
        Detect("en");
        Assert.DoesNotContain("exit code", Strings.BackToNav(0));
        Assert.DoesNotContain("exit code", Strings.BackToNav(null));
        Assert.Contains("exit code 3", Strings.BackToNav(3));
    }

    [Fact]
    public void Usage_incluye_la_version()
    {
        Detect("en");
        Assert.Contains(AppInfo.Version, Strings.Usage);
    }
}
