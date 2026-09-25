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

/// <summary>What the tree hangs its top-level nodes from.</summary>
public enum FileGrouping
{
    /// <summary>Each .NET project heads a node holding the files it claims.</summary>
    Project,

    /// <summary>Everything in one folder, with its own top-level folders and
    /// files standing at the root rather than under a node of their own.</summary>
    Folder
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
    private readonly IReadOnlyList<VisibleFileNode> visibleNodes = Snapshot.Of(VisibleNodes);

    public IReadOnlyList<VisibleFileNode> VisibleNodes
    {
        get => visibleNodes;
        init => visibleNodes = Snapshot.Of(value);
    }

    public int VisibleFileCount => visibleNodes.Count(node => node.Kind == FileNodeKind.File);

    public bool HasGroups => visibleNodes.Any(node => node.Kind != FileNodeKind.File);

    public bool HasExpandedGroups => visibleNodes.Any(node => node.Kind != FileNodeKind.File && node.IsExpanded);

    public FileChangeSummary Changes { get; init; } = FileChangeSummary.Empty;

    /// <summary>Why the files could not be read, when they could not.</summary>
    public string Notice { get; init; } = "";

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
    public sealed record SelectIndex(int Index) : FileExplorerCommand;
    public sealed record MoveUp : FileExplorerCommand;
    public sealed record MoveDown : FileExplorerCommand;
}

public interface IFileExplorerBackend
{
    Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default);
}
