using System.Text;

namespace Nav;

/// <summary>Visor de texto a pantalla completa, solo lectura. Se dibuja sobre la pantalla alternativa de NAV.</summary>
internal sealed class TextViewer
{
    private readonly string _title;
    private readonly long _size;
    private readonly TextDocument _document;
    private List<string> _rows = new();
    private List<int> _lineStarts = new();
    private int _width;
    private int _height;
    private int _top;

    private TextViewer(FileEntry file, TextDocument document)
    {
        _title = file.Name;
        _size = file.Size;
        _document = document;
    }

    private int BodyHeight => Math.Max(1, _height - 2);
    private int MaxTop => Math.Max(0, _rows.Count - BodyHeight);

    /// <summary>Muestra el documento hasta que el usuario lo cierra.</summary>
    public static void Show(FileEntry file, TextDocument document) =>
        new TextViewer(file, document).Run();

    private void Run()
    {
        Refresh();
        Draw();
        while (true)
        {
            var key = WaitKey();
            if (!Handle(key))
                return;
            Draw();
        }
    }

    private ConsoleKeyInfo WaitKey()
    {
        while (!Console.KeyAvailable)
        {
            if (Terminal.Width != _width || Terminal.Height != _height)
            {
                Refresh();
                Draw();
            }
            Thread.Sleep(40);
        }
        return Console.ReadKey(intercept: true);
    }

    /// <summary>Procesa una tecla. Devuelve false para cerrar el visor.</summary>
    private bool Handle(ConsoleKeyInfo key)
    {
        bool ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;
        switch (key.Key)
        {
            case ConsoleKey.Escape:
            case ConsoleKey.Q when !ctrl:
            case ConsoleKey.C when ctrl:
                return false;
            case ConsoleKey.UpArrow:
                Scroll(-1);
                break;
            case ConsoleKey.DownArrow:
                Scroll(1);
                break;
            case ConsoleKey.PageUp:
                Scroll(-Math.Max(1, BodyHeight - 1));
                break;
            case ConsoleKey.PageDown:
                Scroll(Math.Max(1, BodyHeight - 1));
                break;
            case ConsoleKey.Home:
                _top = 0;
                break;
            case ConsoleKey.End:
                _top = MaxTop;
                break;
        }
        return true;
    }

    private void Scroll(int delta) =>
        _top = (int)Math.Clamp((long)_top + delta, 0, MaxTop);

    /// <summary>Recalcula el ajuste de linea para el tamano actual conservando la posicion.</summary>
    private void Refresh()
    {
        int topLine = _lineStarts.Count == 0 ? 0 : LineAt(_top);
        _width = Terminal.Width;
        _height = Terminal.Height;
        _rows = TextDocument.Wrap(_document.Lines, _width, out _lineStarts);
        _top = topLine < _lineStarts.Count ? _lineStarts[topLine] : 0;
        _top = Math.Clamp(_top, 0, MaxTop);
    }

    /// <summary>Linea original a la que pertenece una fila.</summary>
    private int LineAt(int row)
    {
        int index = _lineStarts.BinarySearch(row);
        return index >= 0 ? index : ~index - 1;
    }

    private void Draw()
    {
        var sb = new StringBuilder(_width * _height * 2);

        Terminal.MoveTo(sb, 1, 1);
        string size = Ui.FormatSize(_size) + " ";
        int titleWidth = Math.Max(0, _width - size.Length);
        sb.Append(Theme.Bar).Append(Ui.Pad(" " + _title, titleWidth));
        if (size.Length <= _width)
            sb.Append(size);

        int body = BodyHeight;
        for (int r = 0; r < body; r++)
        {
            Terminal.MoveTo(sb, r + 2, 1);
            int index = _top + r;
            if (index < _rows.Count)
                sb.Append(Theme.Viewer).Append(Ui.Pad(_rows[index], _width));
            else if (_rows.Count == 0 && r == 0)
                sb.Append(Theme.Viewer).Append(Ui.Pad(Strings.Empty, _width));
            else
                sb.Append(Theme.Viewer).Append(' ', _width);
        }

        Terminal.MoveTo(sb, _height, 1);
        sb.Append(Theme.Bar).Append(Ui.Pad(StatusLine(), _width)).Append(Theme.Reset);

        Terminal.Write(sb.ToString());
    }

    private string StatusLine()
    {
        int total = _rows.Count;
        int last = Math.Min(total, _top + BodyHeight);
        int percent = total == 0 ? 100 : (int)((long)last * 100 / total);
        string position = total == 0 ? "" : $"{_top + 1}-{last}/{total}  {percent}%";
        string truncated = _document.Truncated ? "  " + Strings.ViewerTruncated : "";
        return $" {Strings.ViewerKeys}   {position}{truncated}";
    }
}
