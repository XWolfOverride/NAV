namespace Nav.Tests;

/// <summary>
/// Las variables de entorno son del proceso entero: las pruebas que las tocan
/// comparten esta coleccion para que xUnit no las ejecute en paralelo.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class EnvironmentCollection
{
    public const string Name = "Environment";
}

/// <summary>Fija variables de entorno y las restaura al terminar.</summary>
internal sealed class EnvScope : IDisposable
{
    private readonly Dictionary<string, string?> _saved = new();

    public EnvScope Set(string name, string? value)
    {
        if (!_saved.ContainsKey(name))
            _saved[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
        return this;
    }

    public void Dispose()
    {
        foreach (var (name, value) in _saved)
            Environment.SetEnvironmentVariable(name, value);
    }
}

/// <summary>Carpeta temporal que se borra al terminar.</summary>
internal sealed class TempDir : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nav-tests-" + Guid.NewGuid().ToString("N"));

    public TempDir() => Directory.CreateDirectory(Path);

    public string File(string name, string content = "")
    {
        string full = System.IO.Path.Combine(Path, name);
        System.IO.File.WriteAllText(full, content);
        return full;
    }

    public string Folder(string name)
    {
        string full = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(full);
        return full;
    }

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
