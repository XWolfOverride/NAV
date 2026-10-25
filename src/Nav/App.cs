using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Nav;

/// <summary>Bucle principal: rejilla de archivos, busqueda tecleando y ejecucion.</summary>
internal sealed class App
{
    private readonly Options _options;
    private string _dir = "";
    private List<FileEntry> _files = new();
    private int _selection;
    private int _scroll;
    private string _search = "";
    private string? _message;

    private int _width;
    private int _height;
    private int _columns = 1;
    private int _columnWidth = 12;
    private int _rows = 1;

    public App(Options options) => _options = options;

    public string CurrentDirectory => _dir;
    public string? Message => _message;

    public bool Open(string path) => Load(path, null);

    public void Run()
    {
        Terminal.Enter();
        try
        {
            Draw();
            while (Handle(WaitKey()))
                Draw();
        }
        finally
        {
            Terminal.Leave();
        }
    }

    // ---------------------------------------------------------------- input

    /// <summary>Espera una tecla redibujando si cambia el tamano de la terminal.</summary>
    private ConsoleKeyInfo WaitKey()
    {
        while (!Console.KeyAvailable)
        {
            if (Terminal.Width != _width || Terminal.Height != _height)
                Draw();
            Thread.Sleep(40);
        }
        return Console.ReadKey(intercept: true);
    }

    /// <summary>Procesa una tecla. Devuelve false para salir.</summary>
    private bool Handle(ConsoleKeyInfo key)
    {
        _message = null;
        bool ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;
        bool alt = (key.Modifiers & ConsoleModifiers.Alt) != 0;

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                return false;
            case ConsoleKey.C when ctrl:
                return false;
            case ConsoleKey.UpArrow:
                Move(-_columns);
                return true;
            case ConsoleKey.DownArrow:
                Move(_columns);
                return true;
            case ConsoleKey.LeftArrow:
                Move(-1);
                return true;
            case ConsoleKey.RightArrow:
                Move(1);
                return true;
            case ConsoleKey.PageUp:
                Move(-_columns * _rows);
                return true;
            case ConsoleKey.PageDown:
                Move(_columns * _rows);
                return true;
            case ConsoleKey.Home:
                Move(int.MinValue / 2);
                return true;
            case ConsoleKey.End:
                Move(int.MaxValue / 2);
                return true;
            case ConsoleKey.Enter:
                Activate();
                return true;
            case ConsoleKey.Backspace:
                if (_search.Length > 0)
                {
                    _search = _search[..^1];
                    int match = _search.Length > 0 ? Find(_search) : -1;
                    if (match >= 0)
                        _selection = match;
                }
                else
                {
                    GoUp();
                }
                return true;
            case ConsoleKey.F1:
                ShowHelp();
                return true;
            case ConsoleKey.O when ctrl:
                OpenShell();
                return true;
        }

        char c = key.KeyChar;
        if (!ctrl && !alt && !char.IsControl(c))
        {
            // Solo se acepta la letra si algo coincide: la busqueda nunca queda "vacia".
            string candidate = _search + c;
            int found = Find(candidate);
            if (found >= 0)
            {
                _search = candidate;
                _selection = found;
            }
        }
        return true;
    }

    private void Move(int delta)
    {
        _search = "";
        if (_files.Count == 0)
            return;
        long target = (long)_selection + delta;
        _selection = (int)Math.Clamp(target, 0, _files.Count - 1);
    }

    /// <summary>Primero por prefijo (como en DOS); si no hay, por contenido.</summary>
    private int Find(string text)
    {
        int i = _files.FindIndex(f => f.Name.StartsWith(text, StringComparison.OrdinalIgnoreCase));
        if (i < 0)
            i = _files.FindIndex(f => f.Name.Contains(text, StringComparison.OrdinalIgnoreCase));
        return i;
    }

    // -------------------------------------------------------------- actions

    private bool Load(string path, string? selectName)
    {
        try
        {
            string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            var files = DirectoryScanner.Scan(full, _options);

            _dir = full;
            _files = files;
            _selection = 0;
            _scroll = 0;
            _search = "";
            if (selectName is not null)
            {
                int i = _files.FindIndex(f => f.Name == selectName);
                if (i >= 0)
                    _selection = i;
            }

            try { Environment.CurrentDirectory = full; }
            catch (Exception) { }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                      or System.Security.SecurityException or ArgumentException)
        {
            _message = ex.Message;
            return false;
        }
    }

    private void GoUp()
    {
        var parent = Directory.GetParent(_dir);
        if (parent is null)
            return;
        Load(parent.FullName, Path.GetFileName(_dir));
    }

    private void Activate()
    {
        _search = "";
        if (_files.Count == 0)
            return;
        var file = _files[_selection];
        switch (file.Kind)
        {
            case EntryKind.Directory:
                Load(file.FullPath, null);
                break;
            case EntryKind.Executable:
            case EntryKind.Script:
                Execute(file);
                break;
            default:
                View(file);
                break;
        }
    }

    /// <summary>Muestra un archivo que no es ejecutable en el visor de texto, salvo que sea binario.</summary>
    private void View(FileEntry file)
    {
        try
        {
            var document = TextDocument.Load(file.FullPath);
            if (document is null)
                _message = Strings.BinaryFile;
            else
                TextViewer.Show(file, document);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                      or System.Security.SecurityException)
        {
            _message = ex.Message;
        }
    }

    /// <summary>Sale de la pantalla de NAV, ejecuta en la consola normal y vuelve.</summary>
    private void Execute(FileEntry file)
    {
        var startInfo = Launcher.Create(file, _dir);
        Terminal.Leave();
        Console.WriteLine($"[nav] {file.FullPath}");

        int? exitCode = null;
        try
        {
            using var process = Process.Start(startInfo);
            if (process is not null)
            {
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            exitCode = -1;
        }

        Console.WriteLine();
        Console.Write(Strings.BackToNav(exitCode));
        Console.ReadKey(intercept: true);

        Terminal.Enter();
        Load(_dir, file.Name);
    }

    /// <summary>
    /// Abre una shell en la carpeta actual. Al salir de ella se vuelve a NAV.
    /// Si dentro se escribe "nav [ruta]", ese NAV no arranca: deja la ruta en el
    /// fichero de retorno y los scripts de shell cierran la shell, asi que se vuelve
    /// a este NAV en esa carpeta.
    /// </summary>
    private void OpenShell()
    {
        string returnFile = Path.Combine(Path.GetTempPath(), $"nav-{Environment.ProcessId}-{Guid.NewGuid():N}.dir");
        try
        {
            File.WriteAllText(returnFile, "");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _message = ex.Message;
            return;
        }

        string? selected = _files.Count > 0 ? _files[_selection].Name : null;
        var startInfo = Launcher.CreateShell(_dir);
        startInfo.Environment[AppInfo.ReturnFileVariable] = returnFile;

        Terminal.Leave();
        Console.WriteLine(Strings.ShellBanner(_dir));
        try
        {
            using var process = Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.Write(Strings.BackToNav(-1));
            Console.ReadKey(intercept: true);
        }

        string? target = null;
        try
        {
            target = File.ReadAllText(returnFile).Trim();
            File.Delete(returnFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }

        Terminal.Enter();
        if (string.IsNullOrEmpty(target) || !Load(target, null))
            Load(_dir, selected);
    }

    // -------------------------------------------------------------- drawing

    private static string DisplayName(FileEntry f) =>
        f.Kind == EntryKind.Directory ? f.Name + Path.DirectorySeparatorChar : f.Name;

    private void Layout()
    {
        int inner = _width - 2;
        _rows = Math.Max(1, _height - 4);

        int longest = 0;
        foreach (var f in _files)
            longest = Math.Max(longest, DisplayName(f).Length);
        int wanted = Math.Clamp(longest, 12, Math.Max(12, Math.Min(40, inner)));

        // Tantas columnas como quepan y luego se reparten el ancho sobrante.
        _columns = Math.Max(1, (inner + 1) / (wanted + 1));
        _columnWidth = (inner - (_columns - 1)) / _columns;

        int count = _files.Count;
        if (count == 0)
        {
            _selection = 0;
            _scroll = 0;
            return;
        }
        _selection = Math.Clamp(_selection, 0, count - 1);
        int selectedRow = _selection / _columns;
        int totalRows = (count + _columns - 1) / _columns;
        if (selectedRow < _scroll)
            _scroll = selectedRow;
        if (selectedRow >= _scroll + _rows)
            _scroll = selectedRow - _rows + 1;
        _scroll = Math.Clamp(_scroll, 0, Math.Max(0, totalRows - _rows));
    }

    private char[] BorderLine(int inner, char joint)
    {
        var line = new string('─', inner).ToCharArray();
        for (int c = 1; c < _columns; c++)
        {
            int pos = c * (_columnWidth + 1) - 1;
            if (pos >= 0 && pos < inner)
                line[pos] = joint;
        }
        return line;
    }

    private static void Place(char[] line, int start, string text)
    {
        if (start < 0 || start + text.Length > line.Length)
            return;
        text.CopyTo(0, line, start, text.Length);
    }

    private void Draw()
    {
        _width = Terminal.Width;
        _height = Terminal.Height;
        Layout();

        int inner = _width - 2;
        var sb = new StringBuilder(_width * _height * 4);

        // Linea superior con la ruta, como el titulo de la caja en DOS.
        var top = BorderLine(inner, '┬');
        Place(top, 1, "[" + Ui.ClipLeft(_dir, Math.Max(1, inner - 4)) + "]");
        Terminal.MoveTo(sb, 1, 1);
        sb.Append(Theme.Frame).Append('┌').Append(top).Append('┐');

        // Rejilla de archivos.
        int used = _columns * _columnWidth + (_columns - 1);
        for (int r = 0; r < _rows; r++)
        {
            Terminal.MoveTo(sb, r + 2, 1);
            sb.Append(Theme.Frame).Append('│');
            for (int c = 0; c < _columns; c++)
            {
                if (c > 0)
                    sb.Append(Theme.Frame).Append('│');

                int index = (_scroll + r) * _columns + c;
                if (index < _files.Count)
                {
                    var f = _files[index];
                    sb.Append(index == _selection ? Theme.Selected : Theme.For(f.Kind))
                      .Append(Ui.Pad(DisplayName(f), _columnWidth));
                }
                else if (_files.Count == 0 && r == 0 && c == 0)
                {
                    sb.Append(Theme.Other).Append(Ui.Pad(Strings.Empty, _columnWidth));
                }
                else
                {
                    sb.Append(Theme.Frame).Append(' ', _columnWidth);
                }
            }
            sb.Append(Theme.Frame).Append(' ', Math.Max(0, inner - used)).Append('│');
        }

        // Separador.
        Terminal.MoveTo(sb, _height - 2, 1);
        sb.Append(Theme.Frame).Append('├').Append(BorderLine(inner, '┴')).Append('┤');

        // Linea de estado: busqueda + informacion del seleccionado (el INFOBOX de DOS).
        Terminal.MoveTo(sb, _height - 1, 1);
        sb.Append(Theme.Frame).Append('│');
        int searchWidth = Math.Clamp(inner / 3, 8, 24);
        string field = _search.Length > searchWidth
            ? Ui.ClipLeft(_search, searchWidth)
            : _search.PadRight(searchWidth, '_');
        sb.Append(Theme.Search).Append(' ').Append(field).Append(' ');

        int rest = inner - (searchWidth + 2);
        string position = _files.Count > 0 ? $"{_selection + 1}/{_files.Count} " : "";
        if (position.Length >= rest)
            position = "";
        int infoWidth = rest - position.Length;
        if (_message is not null)
            sb.Append(Theme.Error).Append(Ui.Pad(" " + _message, infoWidth));
        else
            sb.Append(Theme.Frame).Append(Ui.Pad(" " + SelectedInfo(), infoWidth));
        sb.Append(Theme.Frame).Append(position).Append('│');

        // Linea inferior.
        var bottom = new string('─', inner).ToCharArray();
        string left = "[" + Strings.F1Help + "]";
        string right = "[NAV " + AppInfo.Version + " by XWolf]";
        Place(bottom, 1, left);
        if (inner - right.Length - 1 > left.Length + 2)
            Place(bottom, inner - right.Length - 1, right);
        Terminal.MoveTo(sb, _height, 1);
        sb.Append(Theme.Frame).Append('└').Append(bottom).Append('┘').Append(Theme.Reset);

        Terminal.Write(sb.ToString());
    }

    private string SelectedInfo()
    {
        if (_files.Count == 0)
            return "";
        var f = _files[_selection];
        string size = f.Kind == EntryKind.Directory ? "" : Ui.FormatSize(f.Size);
        string date = f.Modified == DateTime.MinValue
            ? ""
            : f.Modified.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        string link = f.LinkTarget is null ? "" : "  → " + f.LinkTarget;
        return $"{Strings.Kind(f.Kind),-10}  {size,9}  {date}{link}";
    }

    private void ShowHelp()
    {
        Draw();
        string[] lines =
        {
            @"\  / Folder Navigator v" + AppInfo.Version,
            @" XX   by XWolfOverride 2019-2026",
            @"/  \",
            "",
            Strings.HelpEnter,
            Strings.HelpBack,
            Strings.HelpArrows,
            Strings.HelpPages,
            Strings.HelpShell,
            Strings.HelpEsc,
            "",
            Strings.HelpSearch,
        };

        int boxWidth = Math.Min(_width - 2, lines.Max(l => l.Length) + 4);
        int boxHeight = Math.Min(_height - 2, lines.Length + 2);
        int x = (_width - boxWidth) / 2 + 1;
        int y = (_height - boxHeight) / 2 + 1;

        var sb = new StringBuilder();
        for (int i = 0; i < boxHeight; i++)
        {
            Terminal.MoveTo(sb, y + i, x);
            sb.Append(Theme.Help);
            if (i == 0)
                sb.Append('┌').Append('─', boxWidth - 2).Append('┐');
            else if (i == boxHeight - 1)
                sb.Append('└').Append('─', boxWidth - 2).Append('┘');
            else
                sb.Append("│ ").Append(Ui.Pad(lines[i - 1], boxWidth - 4)).Append(" │");
        }
        sb.Append(Theme.Reset);
        Terminal.Write(sb.ToString());
        Console.ReadKey(intercept: true);
    }
}
