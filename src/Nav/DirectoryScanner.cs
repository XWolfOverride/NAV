using System.Runtime.Versioning;

namespace Nav;

internal static class DirectoryScanner
{
    private static readonly EnumerationOptions Enumeration = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = 0,
        RecurseSubdirectories = false,
        ReturnSpecialDirectories = false,
    };

    public static List<FileEntry> Scan(string path, Options options)
    {
        var result = new List<FileEntry>();
        foreach (var info in new DirectoryInfo(path).EnumerateFileSystemInfos("*", Enumeration))
        {
            // En Linux/macOS .NET marca como Hidden los nombres que empiezan por '.'
            bool hidden = (info.Attributes & FileAttributes.Hidden) != 0;
            if (hidden && !options.ShowHidden)
                continue;

            if (info is DirectoryInfo)
                result.Add(Create(info, EntryKind.Directory, 0));
            else if (!options.DirsOnly && info is FileInfo file)
                result.Add(Create(info, Classify(file), SafeLength(file)));
        }
        result.Sort(Compare);
        return result;
    }

    private static FileEntry Create(FileSystemInfo info, EntryKind kind, long size)
    {
        DateTime modified;
        try { modified = info.LastWriteTime; }
        catch (Exception) { modified = DateTime.MinValue; }

        string? link = null;
        try { link = info.LinkTarget; }
        catch (Exception) { }

        return new FileEntry(info.Name, info.FullName, kind, size, modified, link);
    }

    private static long SafeLength(FileInfo file)
    {
        try { return file.Length; }
        catch (Exception) { return 0; }
    }

    private static EntryKind Classify(FileInfo file)
    {
        string ext = file.Extension;
        if (OperatingSystem.IsWindows())
        {
            if (Is(ext, ".exe") || Is(ext, ".com"))
                return EntryKind.Executable;
            if (Is(ext, ".bat") || Is(ext, ".cmd") || Is(ext, ".ps1"))
                return EntryKind.Script;
            return EntryKind.Other;
        }
        return ClassifyUnix(file, ext);
    }

    [UnsupportedOSPlatform("windows")]
    private static EntryKind ClassifyUnix(FileInfo file, string ext)
    {
        const UnixFileMode anyExecute =
            UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
        try
        {
            if ((file.UnixFileMode & anyExecute) != 0)
                return EntryKind.Executable;
        }
        catch (Exception) { }

        // Script sin bit de ejecucion: se lanza con /bin/sh
        if (ext == ".sh")
            return EntryKind.Script;
        return EntryKind.Other;
    }

    private static bool Is(string a, string b) => a.Equals(b, StringComparison.OrdinalIgnoreCase);

    private static int Compare(FileEntry a, FileEntry b)
    {
        int c = ((int)a.Kind).CompareTo((int)b.Kind);
        if (c != 0)
            return c;
        c = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        return c != 0 ? c : string.CompareOrdinal(a.Name, b.Name);
    }
}
