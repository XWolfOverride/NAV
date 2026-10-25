using System.Diagnostics;

namespace Nav;

/// <summary>Prepara el lanzamiento de ejecutables y scripts heredando la consola.</summary>
internal static class Launcher
{
    public static ProcessStartInfo Create(FileEntry file, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };

        if (OperatingSystem.IsWindows())
        {
            string ext = Path.GetExtension(file.Name);
            if (Is(ext, ".bat") || Is(ext, ".cmd"))
            {
                psi.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
                psi.ArgumentList.Add("/c");
                psi.ArgumentList.Add(file.FullPath);
            }
            else if (Is(ext, ".ps1"))
            {
                psi.FileName = FindInPath("pwsh.exe") ?? "powershell.exe";
                psi.ArgumentList.Add("-NoLogo");
                psi.ArgumentList.Add("-File");
                psi.ArgumentList.Add(file.FullPath);
            }
            else
            {
                psi.FileName = file.FullPath;
            }
        }
        else if (file.Kind == EntryKind.Script)
        {
            psi.FileName = "/bin/sh";
            psi.ArgumentList.Add(file.FullPath);
        }
        else
        {
            psi.FileName = file.FullPath;
        }
        return psi;
    }

    /// <summary>Shell interactiva: NAV_SHELL, o pwsh/powershell/cmd en Windows y $SHELL en Unix.</summary>
    public static ProcessStartInfo CreateShell(string workingDirectory)
    {
        string? shell = Variable("NAV_SHELL");
        if (shell is null)
        {
            shell = OperatingSystem.IsWindows()
                ? FindInPath("pwsh.exe") ?? FindInPath("powershell.exe") ?? Variable("ComSpec") ?? "cmd.exe"
                : Variable("SHELL") ?? "/bin/sh";
        }

        var psi = new ProcessStartInfo
        {
            FileName = shell,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
        };
        string name = Path.GetFileNameWithoutExtension(shell);
        if (Is(name, "pwsh") || Is(name, "powershell"))
            psi.ArgumentList.Add("-NoLogo");
        return psi;
    }

    private static string? Variable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? FindInPath(string executable)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), executable);
                if (File.Exists(candidate))
                    return candidate;
            }
            catch (ArgumentException) { }
        }
        return null;
    }

    private static bool Is(string a, string b) => a.Equals(b, StringComparison.OrdinalIgnoreCase);
}
