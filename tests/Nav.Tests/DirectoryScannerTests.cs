namespace Nav.Tests;

[Collection(EnvironmentCollection.Name)]
public class DirectoryScannerTests
{
    private static Options Parse(params string[] args)
    {
        using var env = new EnvScope().Set("NAVOPT", null);
        return Options.Parse(args);
    }

    private static List<string> Names(List<FileEntry> entries) => entries.Select(e => e.Name).ToList();

    [Fact]
    public void Carpeta_vacia_da_lista_vacia()
    {
        using var dir = new TempDir();
        Assert.Empty(DirectoryScanner.Scan(dir.Path, Parse()));
    }

    [Fact]
    public void Las_carpetas_van_primero_y_el_orden_ignora_mayusculas()
    {
        using var dir = new TempDir();
        dir.File("b.txt");
        dir.File("A.txt");
        dir.Folder("zeta");
        dir.Folder("Alfa");

        var entries = DirectoryScanner.Scan(dir.Path, Parse());

        Assert.Equal(new[] { "Alfa", "zeta", "A.txt", "b.txt" }, Names(entries));
        Assert.All(entries.Take(2), e => Assert.Equal(EntryKind.Directory, e.Kind));
    }

    [Fact]
    public void Dirs_only_deja_solo_carpetas()
    {
        using var dir = new TempDir();
        dir.File("a.txt");
        dir.Folder("sub");

        var entries = DirectoryScanner.Scan(dir.Path, Parse("-d"));

        Assert.Equal(new[] { "sub" }, Names(entries));
    }

    [Fact]
    public void Los_ocultos_solo_salen_con_la_opcion_a()
    {
        using var dir = new TempDir();
        dir.File("visible.txt");
        string hidden = dir.File(".oculto");
        // En Windows el atributo hay que ponerlo; en Unix .NET lo deduce del punto.
        File.SetAttributes(hidden, File.GetAttributes(hidden) | FileAttributes.Hidden);

        Assert.Equal(new[] { "visible.txt" }, Names(DirectoryScanner.Scan(dir.Path, Parse())));
        Assert.Equal(new[] { ".oculto", "visible.txt" }, Names(DirectoryScanner.Scan(dir.Path, Parse("-a"))));
    }

    [Fact]
    public void Se_rellenan_tamano_y_ruta_completa()
    {
        using var dir = new TempDir();
        string path = dir.File("datos.bin", "12345");

        var entry = Assert.Single(DirectoryScanner.Scan(dir.Path, Parse()));

        Assert.Equal(5, entry.Size);
        Assert.Equal(path, entry.FullPath);
        Assert.NotEqual(DateTime.MinValue, entry.Modified);
    }

    [Fact]
    public void Las_carpetas_tienen_tamano_cero()
    {
        using var dir = new TempDir();
        dir.Folder("sub");

        var entry = Assert.Single(DirectoryScanner.Scan(dir.Path, Parse()));

        Assert.Equal(EntryKind.Directory, entry.Kind);
        Assert.Equal(0, entry.Size);
    }

    [Fact]
    public void Una_carpeta_inexistente_lanza_excepcion()
    {
        using var dir = new TempDir();
        string missing = Path.Combine(dir.Path, "no-existe");

        Assert.ThrowsAny<IOException>(() => DirectoryScanner.Scan(missing, Parse()));
    }

    [Fact]
    public void En_windows_se_clasifica_por_extension()
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var dir = new TempDir();
        dir.File("a.exe");
        dir.File("b.COM");
        dir.File("c.bat");
        dir.File("d.cmd");
        dir.File("e.ps1");
        dir.File("f.txt");

        var kinds = DirectoryScanner.Scan(dir.Path, Parse()).ToDictionary(e => e.Name, e => e.Kind);

        Assert.Equal(EntryKind.Executable, kinds["a.exe"]);
        Assert.Equal(EntryKind.Executable, kinds["b.COM"]);
        Assert.Equal(EntryKind.Script, kinds["c.bat"]);
        Assert.Equal(EntryKind.Script, kinds["d.cmd"]);
        Assert.Equal(EntryKind.Script, kinds["e.ps1"]);
        Assert.Equal(EntryKind.Other, kinds["f.txt"]);
    }

    [Fact]
    public void En_windows_el_orden_es_carpeta_ejecutable_script_otro()
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var dir = new TempDir();
        dir.File("a.txt");
        dir.File("b.bat");
        dir.File("c.exe");
        dir.Folder("d");

        Assert.Equal(new[] { "d", "c.exe", "b.bat", "a.txt" }, Names(DirectoryScanner.Scan(dir.Path, Parse())));
    }

    [Fact]
    public void En_unix_se_clasifica_por_bit_de_ejecucion_y_sh()
    {
        if (OperatingSystem.IsWindows())
            return;

        using var dir = new TempDir();
        string exe = dir.File("programa");
        File.SetUnixFileMode(exe, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        string plain = dir.File("notas.txt");
        File.SetUnixFileMode(plain, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        string script = dir.File("script.sh");
        File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        var kinds = DirectoryScanner.Scan(dir.Path, Parse()).ToDictionary(e => e.Name, e => e.Kind);

        Assert.Equal(EntryKind.Executable, kinds["programa"]);
        Assert.Equal(EntryKind.Script, kinds["script.sh"]);
        Assert.Equal(EntryKind.Other, kinds["notas.txt"]);
    }
}
