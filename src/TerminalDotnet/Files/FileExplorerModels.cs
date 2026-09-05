using TerminalDotnet.Filters;

namespace TerminalDotnet.Files;

public enum FileGitStatus
{
    Unchanged,
    Modified,
    New,
    Deleted
}

public enum FileNodeKind
{
    Project,
    Folder,
    File
}

public sealed record FileEntry(
    string ProjectPath,
    string Path,
    FileGitStatus GitStatus);

public sealed record VisibleFileNode(
    int Depth,
    FileNodeKind Kind,
    string Name,
    IReadOnlyList<FileEntry> Files,
    bool IsExpanded = true);

public sealed record FileChangeSummary(int Total, int Added, int Edited, int Deleted)
{
    public static readonly FileChangeSummary Empty = new(0, 0, 0, 0);
}

public sealed record FileExplorerState(
    IReadOnlyList<VisibleFileNode> VisibleNodes,
    int SelectedIndex = 0,
    string SearchQuery = "",
    ExplorerFilter? ActiveFilter = null)
{
    public FileChangeSummary Changes { get; init; } = FileChangeSummary.Empty;
}

public abstract record FileExplorerCommand
{
    public sealed record Search(string Query) : FileExplorerCommand;
    public sealed record ClearSearch : FileExplorerCommand;
    public sealed record ToggleFilter(ExplorerFilter Filter) : FileExplorerCommand;
    public sealed record ToggleExpanded : FileExplorerCommand;
    public sealed record MoveUp : FileExplorerCommand;
    public sealed record MoveDown : FileExplorerCommand;
    public sealed record NextSearchMatch : FileExplorerCommand;
    public sealed record PreviousSearchMatch : FileExplorerCommand;
}

public interface IFileExplorerBackend
{
    Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default);
}
