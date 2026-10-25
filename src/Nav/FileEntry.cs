namespace Nav;

/// <summary>El orden del enum es el orden en el listado (como en la version de DOS).</summary>
internal enum EntryKind
{
    Directory,
    Executable,
    Script,
    Other,
}

internal sealed record FileEntry(
    string Name,
    string FullPath,
    EntryKind Kind,
    long Size,
    DateTime Modified,
    string? LinkTarget);
