namespace Nav;

/// <summary>Textos en espanol e ingles (en DOS eran los DEFINE SPANISH / ENGLISH).</summary>
internal static class Strings
{
    public static bool Spanish { get; private set; }

    /// <summary>NAV_LANG manda; si no, el locale (LC_ALL, LC_MESSAGES, LANG) o el idioma de Windows.</summary>
    public static void Detect()
    {
        string? lang = FirstNonEmpty("NAV_LANG", "LC_ALL", "LC_MESSAGES", "LANG");
        if (lang is null && OperatingSystem.IsWindows())
            lang = (Terminal.GetUserDefaultUILanguage() & 0x3FF) == 0x0A ? "es" : "en";
        Spanish = lang is not null && lang.StartsWith("es", StringComparison.OrdinalIgnoreCase);
    }

    private static string? FirstNonEmpty(params string[] variables)
    {
        foreach (var name in variables)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }
        return null;
    }

    private static string T(string english, string spanish) => Spanish ? spanish : english;

    public static string HelpEnter => T("[Enter]      Browse / Execute / View text", "[Enter]      Navegar / Ejecutar / Ver texto");
    public static string ViewerKeys => T("Esc/Q: close  Up/Down/PgUp/PgDn/Home/End: move", "Esc/Q: cerrar  Arriba/Abajo/RePag/AvPag/Inicio/Fin: mover");
    public static string ViewerTruncated => T("[truncated]", "[truncado]");
    public static string BinaryFile => T("Binary file: cannot be shown", "Archivo binario: no se puede mostrar");
    public static string HelpBack => T("[Backspace]  Parent folder", "[Retroceso]  Carpeta superior");
    public static string HelpArrows => T("[Arrows]     Select", "[Flechas]    Seleccionar");
    public static string HelpPages => T("[PgUp/PgDn]  Page  [Home/End]", "[RePag/AvPag] Pagina  [Inicio/Fin]");
    public static string HelpShell => T("[Ctrl+O]     Shell here", "[Ctrl+O]     Shell aqui");
    public static string HelpEsc => T("[Esc]        Exit", "[Esc]        Salir");
    public static string HelpSearch => T("Type to do a fast search.", "Escribe para buscar.");
    public static string AnyKey => T("Press any key", "Pulsa una tecla");
    public static string F1Help => T("F1 Help", "F1 Ayuda");

    public static string KindDirectory => T("Folder", "Carpeta");
    public static string KindExecutable => T("Executable", "Ejecutable");
    public static string KindScript => T("Script", "Script");
    public static string KindOther => T("Other", "Otros");

    public static string Kind(EntryKind kind) => kind switch
    {
        EntryKind.Directory => KindDirectory,
        EntryKind.Executable => KindExecutable,
        EntryKind.Script => KindScript,
        _ => KindOther,
    };

    public static string Empty => T("(empty)", "(vacia)");
    public static string NeedTerminal => T("nav needs an interactive terminal.", "nav necesita una terminal interactiva.");
    public static string TooManyPaths => T("Only one path can be given.", "Solo se puede indicar una ruta.");

    public static string NotADirectory(string path) =>
        T($"Not a folder: {path}", $"No es una carpeta: {path}");

    public static string ShellBanner(string dir) => T(
        $"[nav] Shell in {dir}. 'exit' returns to NAV; 'nav [path]' returns to NAV in that folder.",
        $"[nav] Shell en {dir}. 'exit' vuelve a NAV; 'nav [ruta]' vuelve a NAV en esa carpeta.");

    public static string ReturnHint(string dir) => T(
        $"[nav] NAV will open {dir} when this shell exits (type 'exit').",
        $"[nav] NAV abrira {dir} al cerrar esta shell (escribe 'exit').");

    public static string UnknownOption(string option) =>
        T($"Unknown option: {option}", $"Opcion desconocida: {option}");

    public static string MissingValue(string option) =>
        T($"Missing value for {option}", $"Falta el valor de {option}");

    public static string BackToNav(int? exitCode)
    {
        string code = exitCode is int c && c != 0
            ? T($" (exit code {c})", $" (codigo de salida {c})")
            : "";
        return T($"[nav] Press any key to return{code}...", $"[nav] Pulsa una tecla para volver{code}...");
    }

    public static string Usage => T(
$"""
NAV v{AppInfo.Version} - Folder navigator by XWolfOverride

usage: nav [path] [-options]
  path          Start folder (default: current folder)
  -a            Show hidden files
  -d            Show only folders
  -h            This help
  --cd-file F   On exit, write the final folder to F (used by the shell scripts)
  --new         Start a new NAV even inside a shell opened from NAV (Ctrl+O)
  --version     Show version

Options can also be set in the NAVOPT variable (e.g. NAVOPT=a).
Language: NAV_LANG=es / NAV_LANG=en. Shell for Ctrl+O: NAV_SHELL.

""",
$"""
NAV v{AppInfo.Version} - Navegador de carpetas por XWolfOverride

uso: nav [ruta] [-opciones]
  ruta          Carpeta inicial (por defecto, la actual)
  -a            Mostrar archivos ocultos
  -d            Mostrar solo carpetas
  -h            Esta ayuda
  --cd-file F   Al salir, escribe en F la carpeta final (lo usan los scripts de shell)
  --new         Abrir un NAV nuevo aunque se este en una shell abierta desde NAV (Ctrl+O)
  --version     Mostrar version

Las opciones tambien se pueden poner en la variable NAVOPT (p. ej. NAVOPT=a).
Idioma: NAV_LANG=es / NAV_LANG=en. Shell para Ctrl+O: NAV_SHELL.

""");
}
