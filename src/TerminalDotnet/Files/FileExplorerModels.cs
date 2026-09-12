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
    private (IReadOnlyList<VisibleFileNode> Nodes, int Files, bool Groups, bool Expanded) content =
        Summarize(VisibleNodes);

    public IReadOnlyList<VisibleFileNode> VisibleNodes
    {
        get => content.Nodes;
        init => content = Summarize(value);
    }

    public int VisibleFileCount => content.Files;
    public bool HasGroups => content.Groups;
    public bool HasExpandedGroups => content.Expanded;

    private static (IReadOnlyList<VisibleFileNode>, int, bool, bool) Summarize(
        IReadOnlyList<VisibleFileNode> nodes)
    {
        var frozen = Snapshot.Of(nodes);
        return (
            frozen,
            frozen.Count(node => node.Kind == FileNodeKind.File),
            frozen.Any(node => node.Kind != FileNodeKind.File),
            frozen.Any(node => node.Kind != FileNodeKind.File && node.IsExpanded));
    }

    public FileChangeSummary Changes { get; init; } = FileChangeSummary.Empty;

    /// <summary>Set while the first discovery is still running, so the panel
    /// does not claim there are no files before it has looked.</summary>
    public bool Loading { get; init; }
}

public abstract record FileExplorerCommand
{
    public sealed record Search(string Query) : FileExplorerCommand;
    public sealed record ClearSearch : FileExplorerCommand;
    public sealed record ToggleFilter(ExplorerFilter Filter) : FileExplorerCommand;
    public sealed record ToggleExpanded : FileExplorerCommand;
    public sealed record ToggleAllExpanded : FileExplorerCommand;
    public sealed record MoveUp : FileExplorerCommand;
    public sealed record MoveDown : FileExplorerCommand;
}

public interface IFileExplorerBackend
{
    Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default);
}
