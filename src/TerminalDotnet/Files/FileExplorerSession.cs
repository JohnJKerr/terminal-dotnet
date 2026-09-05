using TerminalDotnet.Filters;
using TerminalDotnet.Search;

namespace TerminalDotnet.Files;

public sealed class FileExplorerSession(IFileExplorerBackend backend)
{
    private IReadOnlyList<TreeNode> tree = [];
    private IReadOnlyList<FileEntry> discoveredFiles = [];
    private readonly HashSet<string> collapsedNodes = [];

    public FileExplorerState State { get; private set; } = new([]);

    public async Task LoadAsync(string target, CancellationToken cancellationToken = default)
    {
        var files = await backend.DiscoverAsync(target, cancellationToken);
        discoveredFiles = files;
        tree = TreeFrom(files);
        State = new FileExplorerState(VisibleNodes()) { Changes = SummaryFrom(files) };
    }

    public Task DispatchAsync(FileExplorerCommand command)
    {
        Apply(command);
        return Task.CompletedTask;
    }

    private void Apply(FileExplorerCommand command)
    {
        switch (command)
        {
            case FileExplorerCommand.Search search:
                ApplySearch(search.Query);
                return;
            case FileExplorerCommand.ClearSearch:
                ApplySearch("");
                return;
            case FileExplorerCommand.ToggleFilter filter:
                ApplyFilter(filter.Filter);
                return;
            case FileExplorerCommand.NextSearchMatch:
                Select(SelectionRing.Next(FileIndices(), State.SelectedIndex));
                return;
            case FileExplorerCommand.PreviousSearchMatch:
                Select(SelectionRing.Previous(FileIndices(), State.SelectedIndex));
                return;
            case FileExplorerCommand.ToggleExpanded:
                ToggleSelectedExpansion();
                return;
            default:
                MoveSelection(command);
                return;
        }
    }

    private void ApplySearch(string query) => Show(query, State.ActiveFilter);

    private void ApplyFilter(ExplorerFilter filter) =>
        Show(State.SearchQuery, State.ActiveFilter == filter ? null : filter);

    private void Show(string query, ExplorerFilter? filter)
    {
        tree = TreeFrom(FilesMatching(query, filter));
        State = State with
        {
            VisibleNodes = VisibleNodes(),
            SelectedIndex = 0,
            SearchQuery = query,
            ActiveFilter = filter
        };
    }

    private void ToggleSelectedExpansion()
    {
        if (State.VisibleNodes.Count == 0)
        {
            return;
        }

        Collapse(VisibleKeys()[State.SelectedIndex]);
        State = State with { VisibleNodes = VisibleNodes() };
    }

    private void Collapse(string key)
    {
        if (!collapsedNodes.Add(key))
        {
            collapsedNodes.Remove(key);
        }
    }

    private void MoveSelection(FileExplorerCommand command)
    {
        var lastIndex = Math.Max(0, State.VisibleNodes.Count - 1);
        State = State with
        {
            SelectedIndex = command switch
            {
                FileExplorerCommand.MoveUp => Math.Max(0, State.SelectedIndex - 1),
                FileExplorerCommand.MoveDown => Math.Min(lastIndex, State.SelectedIndex + 1),
                _ => State.SelectedIndex
            }
        };
    }

    private void Select(int index)
    {
        if (index == SelectionRing.None)
        {
            return;
        }

        State = State with { SelectedIndex = index };
    }

    private IReadOnlyList<VisibleFileNode> VisibleNodes() =>
        Unfolded().Select(node => node.Node with { IsExpanded = IsExpanded(node.Key) }).ToArray();

    private IReadOnlyList<string> VisibleKeys() =>
        Unfolded().Select(node => node.Key).ToArray();

    private IReadOnlyList<TreeNode> Unfolded()
    {
        var visible = new List<TreeNode>();
        int? hiddenBelowDepth = null;
        foreach (var node in tree)
        {
            if (hiddenBelowDepth is not null && node.Node.Depth > hiddenBelowDepth)
            {
                continue;
            }

            hiddenBelowDepth = IsExpanded(node.Key) ? null : node.Node.Depth;
            visible.Add(node);
        }

        return visible;
    }

    private bool IsExpanded(string key) => !collapsedNodes.Contains(key);

    private IReadOnlyList<FileEntry> FilesMatching(string query, ExplorerFilter? filter) =>
        discoveredFiles
            .Where(file => SearchMatch.Matches(file.Path, query))
            .Where(file => PassesFilter(file, filter))
            .ToArray();

    private static bool PassesFilter(FileEntry file, ExplorerFilter? filter) =>
        filter != ExplorerFilter.Updated || file.GitStatus != FileGitStatus.Unchanged;

    private IReadOnlyList<int> FileIndices() => State.VisibleNodes
        .Select((node, index) => (node, index))
        .Where(item => item.node.Kind == FileNodeKind.File)
        .Select(item => item.index)
        .ToArray();

    private static FileChangeSummary SummaryFrom(IReadOnlyList<FileEntry> files) => new(
        files.Count(file => file.GitStatus != FileGitStatus.Deleted),
        files.Count(file => file.GitStatus == FileGitStatus.New),
        files.Count(file => file.GitStatus == FileGitStatus.Modified),
        files.Count(file => file.GitStatus == FileGitStatus.Deleted));

    private static IReadOnlyList<TreeNode> TreeFrom(IReadOnlyList<FileEntry> files) => files
        .Where(file => file.GitStatus != FileGitStatus.Deleted)
        .GroupBy(file => file.ProjectPath, StringComparer.Ordinal)
        .OrderBy(project => project.Key, StringComparer.Ordinal)
        .SelectMany(ProjectNodes)
        .ToArray();

    private static IEnumerable<TreeNode> ProjectNodes(IGrouping<string, FileEntry> project)
    {
        var placements = project.Select(file => PlacementFor(project.Key, file)).ToArray();
        var projectNode = new TreeNode(
            project.Key,
            new VisibleFileNode(
                0,
                FileNodeKind.Project,
                Path.GetFileNameWithoutExtension(project.Key),
                placements.Select(placement => placement.File).ToArray()));

        return [projectNode, .. ChildNodes(placements, project.Key, 1)];
    }

    private static FilePlacement PlacementFor(string projectPath, FileEntry file)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath);
        var relativePath = Path.GetRelativePath(
            string.IsNullOrEmpty(projectDirectory) ? "." : projectDirectory,
            file.Path);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return new FilePlacement(segments[..^1], file);
    }

    private static IEnumerable<TreeNode> ChildNodes(
        IReadOnlyList<FilePlacement> placements,
        string parentKey,
        int depth)
    {
        var folders = placements
            .Where(placement => placement.Folders.Count > 0)
            .GroupBy(placement => placement.Folders[0], StringComparer.Ordinal)
            .OrderBy(folder => folder.Key, StringComparer.Ordinal)
            .SelectMany(folder => FolderNodes(folder, parentKey, depth));
        var files = placements
            .Where(placement => placement.Folders.Count == 0)
            .OrderBy(placement => placement.File.Path, StringComparer.Ordinal)
            .Select(placement => FileNode(placement.File, parentKey, depth));

        return [.. folders, .. files];
    }

    private static IEnumerable<TreeNode> FolderNodes(
        IGrouping<string, FilePlacement> folder,
        string parentKey,
        int depth)
    {
        var key = $"{parentKey}/{folder.Key}";
        var contents = folder.Select(placement => placement.WithoutLeadingFolder()).ToArray();
        var folderNode = new TreeNode(
            key,
            new VisibleFileNode(
                depth,
                FileNodeKind.Folder,
                folder.Key,
                contents.Select(placement => placement.File).ToArray()));

        return [folderNode, .. ChildNodes(contents, key, depth + 1)];
    }

    private static TreeNode FileNode(FileEntry file, string parentKey, int depth) => new(
        $"{parentKey}/{Path.GetFileName(file.Path)}",
        new VisibleFileNode(depth, FileNodeKind.File, Path.GetFileName(file.Path), [file]));

    private sealed record TreeNode(string Key, VisibleFileNode Node);

    private sealed record FilePlacement(IReadOnlyList<string> Folders, FileEntry File)
    {
        public FilePlacement WithoutLeadingFolder() => this with { Folders = Folders.Skip(1).ToArray() };
    }
}
