namespace Nav.Tests;

[Collection(EnvironmentCollection.Name)]
public class LauncherTests
{
    private static FileEntry Entry(string path, EntryKind kind) =>
        new(Path.GetFileName(path), path, kind, 0, DateTime.MinValue, null);

    [Fact]
    public void Create_usa_la_carpeta_actual_y_no_el_shell_execute()
    {
        string dir = Path.GetTempPath();
        var psi = Launcher.Create(Entry(Path.Combine(dir, "x.exe"), EntryKind.Executable), dir);

        Assert.Equal(dir, psi.WorkingDirectory);
        Assert.False(psi.UseShellExecute);
    }

    [Fact]
    public void Create_en_windows_bat_va_por_comspec()
    {
        if (!OperatingSystem.IsWindows())
            return;

        string path = @"C:\carpeta\a.bat";
        var psi = Launcher.Create(Entry(path, EntryKind.Script), @"C:\carpeta");

        Assert.Equal(new[] { "/c", path }, psi.ArgumentList);
    }

    [Fact]
    public void Create_en_windows_exe_se_lanza_directamente()
    {
        if (!OperatingSystem.IsWindows())
            return;

        string path = @"C:\carpeta\a.exe";
        var psi = Launcher.Create(Entry(path, EntryKind.Executable), @"C:\carpeta");

        Assert.Equal(path, psi.FileName);
        Assert.Empty(psi.ArgumentList);
    }

    [Fact]
    public void Create_en_windows_ps1_usa_powershell_con_file()
    {
        if (!OperatingSystem.IsWindows())
            return;

        string path = @"C:\carpeta\a.ps1";
        var psi = Launcher.Create(Entry(path, EntryKind.Script), @"C:\carpeta");

        Assert.Contains("powershell", psi.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[] { "-NoLogo", "-File", path }, psi.ArgumentList);
    }

    [Fact]
    public void Create_en_unix_script_va_por_bin_sh()
    {
        if (OperatingSystem.IsWindows())
            return;

        var psi = Launcher.Create(Entry("/tmp/a.sh", EntryKind.Script), "/tmp");

        Assert.Equal("/bin/sh", psi.FileName);
        Assert.Equal(new[] { "/tmp/a.sh" }, psi.ArgumentList);
    }

    [Fact]
    public void Create_en_unix_ejecutable_se_lanza_directamente()
    {
        if (OperatingSystem.IsWindows())
            return;

        var psi = Launcher.Create(Entry("/tmp/a", EntryKind.Executable), "/tmp");

        Assert.Equal("/tmp/a", psi.FileName);
    }

    [Fact]
    public void CreateShell_respeta_NAV_SHELL()
    {
        using var env = new EnvScope().Set("NAV_SHELL", "/bin/miShell");

        var psi = Launcher.CreateShell("/tmp");

        Assert.Equal("/bin/miShell", psi.FileName);
        Assert.Equal("/tmp", psi.WorkingDirectory);
        Assert.Empty(psi.ArgumentList);
    }

    [Theory]
    [InlineData("pwsh")]
    [InlineData("powershell")]
    public void CreateShell_anade_NoLogo_a_powershell(string name)
    {
        using var env = new EnvScope().Set("NAV_SHELL", name);

        var psi = Launcher.CreateShell("/tmp");

        Assert.Equal(new[] { "-NoLogo" }, psi.ArgumentList);
    }

    [Fact]
    public void CreateShell_sin_NAV_SHELL_elige_una_shell_del_sistema()
    {
        using var env = new EnvScope().Set("NAV_SHELL", null);

        var psi = Launcher.CreateShell(Path.GetTempPath());

        Assert.False(string.IsNullOrWhiteSpace(psi.FileName));
    }
}
