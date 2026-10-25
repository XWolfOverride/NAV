using Nav;

Strings.Detect();

var options = Options.Parse(args);
if (options.Errors.Count > 0)
{
    foreach (var error in options.Errors)
        Console.Error.WriteLine(error);
    Console.Error.WriteLine();
    Console.Error.Write(Strings.Usage);
    return 2;
}
if (options.Help)
{
    Console.Write(Strings.Usage);
    return 0;
}
if (options.Version)
{
    Console.WriteLine($"nav {AppInfo.Version}");
    return 0;
}
// Dentro de una shell abierta con Ctrl+O: en vez de abrir otro NAV, se devuelve
// la carpeta al NAV padre (que la abrira cuando esta shell termine).
var returnFile = Environment.GetEnvironmentVariable(AppInfo.ReturnFileVariable);
if (!options.New && !string.IsNullOrEmpty(returnFile) && File.Exists(returnFile))
{
    string target = Path.GetFullPath(options.StartDirectory ?? Environment.CurrentDirectory);
    if (!Directory.Exists(target))
    {
        Console.Error.WriteLine(Strings.NotADirectory(target));
        return 1;
    }
    File.WriteAllText(returnFile, target);
    Console.Error.WriteLine(Strings.ReturnHint(target));
    return AppInfo.ReturnExitCode;
}

if (Console.IsInputRedirected || Console.IsOutputRedirected)
{
    Console.Error.WriteLine(Strings.NeedTerminal);
    return 1;
}

// Ctrl+C mientras corre un programa lanzado desde NAV no debe matar a NAV.
Console.CancelKeyPress += (_, e) => e.Cancel = true;

var app = new App(options);
if (!app.Open(options.StartDirectory ?? Environment.CurrentDirectory))
{
    Console.Error.WriteLine(app.Message);
    return 1;
}

app.Run();

if (options.CdFile is not null)
{
    try
    {
        File.WriteAllText(options.CdFile, app.CurrentDirectory);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine(ex.Message);
    }
}
return 0;

internal static class AppInfo
{
    public const string Version = "2.0";

    /// <summary>Fichero por el que una shell hija (Ctrl+O) devuelve una carpeta al NAV padre.</summary>
    public const string ReturnFileVariable = "NAV_RETURN_FILE";

    /// <summary>Codigo de salida del NAV anidado: los scripts de shell cierran la shell al verlo.</summary>
    public const int ReturnExitCode = 75;
}
