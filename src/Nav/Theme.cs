namespace Nav;

/// <summary>Colores inspirados en la version de DOS (fondo azul).</summary>
internal static class Theme
{
    public const string Reset = "\u001b[0m";

    public static readonly string Frame = Sgr(97, 44);
    public static readonly string Directory = Sgr(97, 44);
    public static readonly string Executable = Sgr(92, 44);
    public static readonly string Script = Sgr(96, 44);
    public static readonly string Other = Sgr(37, 44);
    public static readonly string Selected = Sgr(97, 42);
    public static readonly string Search = Sgr(93, 44);
    public static readonly string Error = Sgr(97, 41);
    public static readonly string Help = Sgr(30, 47);
    public static readonly string Viewer = Sgr(37, 40);
    public static readonly string Bar = Sgr(30, 46);

    public static string For(EntryKind kind) => kind switch
    {
        EntryKind.Directory => Directory,
        EntryKind.Executable => Executable,
        EntryKind.Script => Script,
        _ => Other,
    };

    private static string Sgr(int foreground, int background) =>
        $"\u001b[0;{foreground};{background}m";
}
