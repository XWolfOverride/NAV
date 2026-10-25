namespace Nav;

/// <summary>Opciones de linea de comandos (y de la variable NAVOPT).</summary>
internal sealed class Options
{
    public bool ShowHidden { get; private set; }
    public bool DirsOnly { get; private set; }
    public bool Help { get; private set; }
    public bool Version { get; private set; }
    public bool New { get; private set; }
    public string? StartDirectory { get; private set; }
    public string? CdFile { get; private set; }
    public List<string> Errors { get; } = new();

    public static Options Parse(string[] args)
    {
        var o = new Options();

        // Igual que LSOPT en la version de DOS: letras de opcion, p. ej. NAVOPT=ad
        var env = Environment.GetEnvironmentVariable("NAVOPT");
        if (!string.IsNullOrWhiteSpace(env))
            o.ApplyFlags(env.Trim().TrimStart('-', '/'));

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            if (a == "--help")
                o.Help = true;
            else if (a == "--version")
                o.Version = true;
            else if (a == "--new")
                o.New = true;
            else if (a == "--cd-file")
            {
                if (i + 1 < args.Length)
                    o.CdFile = args[++i];
                else
                    o.Errors.Add(Strings.MissingValue(a));
            }
            else if (a.StartsWith("--", StringComparison.Ordinal))
                o.Errors.Add(Strings.UnknownOption(a));
            else if (a.Length > 1 && (a[0] == '-' || (OperatingSystem.IsWindows() && a[0] == '/')))
                o.ApplyFlags(a[1..]);
            else if (o.StartDirectory is null)
                o.StartDirectory = a;
            else
                o.Errors.Add(Strings.TooManyPaths);
        }
        return o;
    }

    private void ApplyFlags(string flags)
    {
        foreach (char c in flags)
        {
            switch (c)
            {
                case 'a': ShowHidden = true; break;
                case 'd': DirsOnly = true; break;
                case 'h':
                case '?': Help = true; break;
                default: Errors.Add(Strings.UnknownOption("-" + c)); break;
            }
        }
    }
}
