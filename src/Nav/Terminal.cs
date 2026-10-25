using System.Runtime.InteropServices;
using System.Text;

namespace Nav;

/// <summary>
/// Capa minima de terminal con secuencias ANSI/VT. Funciona en local y por SSH:
/// usa la "pantalla alternativa" de la terminal (como vim o htop), que al salir
/// devuelve la consola tal como estaba, igual que SaveScreen/RestoreScreen en DOS.
/// </summary>
internal static partial class Terminal
{
    public const string Csi = "\u001b[";

    public static int Width
    {
        get
        {
            try { return Math.Max(24, Console.WindowWidth); }
            catch (Exception) { return 80; }
        }
    }

    public static int Height
    {
        get
        {
            try { return Math.Max(8, Console.WindowHeight); }
            catch (Exception) { return 24; }
        }
    }

    public static void Enter()
    {
        EnableVirtualTerminal();
        Console.OutputEncoding = Encoding.UTF8;
        try { Console.TreatControlCAsInput = true; }
        catch (IOException) { }

        // Pantalla alternativa, cursor oculto, sin auto-wrap, borrar.
        Write(Csi + "?1049h" + Csi + "?25l" + Csi + "?7l" + Csi + "0m" + Csi + "2J");
    }

    public static void Leave()
    {
        Write(Csi + "0m" + Csi + "?7h" + Csi + "?25h" + Csi + "?1049l");
        try { Console.TreatControlCAsInput = false; }
        catch (IOException) { }
    }

    public static void Write(string text)
    {
        var output = Console.Out;
        output.Write(text);
        output.Flush();
    }

    public static void MoveTo(StringBuilder sb, int row, int column) =>
        sb.Append(Csi).Append(row).Append(';').Append(column).Append('H');

    /// <summary>La consola clasica de Windows necesita activar el modo VT a mano.</summary>
    private static void EnableVirtualTerminal()
    {
        if (!OperatingSystem.IsWindows())
            return;
        nint handle = GetStdHandle(StdOutputHandle);
        if (GetConsoleMode(handle, out uint mode))
            SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
    }

    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    [LibraryImport("kernel32.dll")]
    private static partial nint GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

    [LibraryImport("kernel32.dll")]
    internal static partial ushort GetUserDefaultUILanguage();
}
