namespace TerminalDotnet.Files;

public enum FileGitStatus
{
    Unchanged,
    Modified,
    New
}

public enum FileNodeKind
{
    Project,
    Namespace,
    File
}

public sealed record FileEntry(
    string ProjectPath,
    string Namespace,
    string Path,
    FileGitStatus GitStatus);

public sealed record VisibleFileNode(
    int Depth,
    FileNodeKind Kind,
    string Name,
    IReadOnlyList<FileEntry> Files,
    bool IsExpanded = true);

public sealed record FileExplorerState(
    IReadOnlyList<VisibleFileNode> VisibleNodes,
    int SelectedIndex = 0,
    string SearchQuery = "");

public abstract record FileExplorerCommand
{
    public sealed record ToggleExpanded : FileExplorerCommand;
    public sealed record MoveUp : FileExplorerCommand;
    public sealed record MoveDown : FileExplorerCommand;
}

public interface IFileExplorerBackend
{
    Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default);
}
