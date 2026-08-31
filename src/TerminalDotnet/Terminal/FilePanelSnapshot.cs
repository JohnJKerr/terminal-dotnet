using TerminalDotnet.Files;

namespace TerminalDotnet.Terminal;

public enum FileRowTone
{
    Neutral,
    Modified,
    New
}

public sealed record FilePanelRow(string Text, FileRowTone Tone);

public sealed record FilePanelSnapshot(
    IReadOnlyList<VisibleFileNode> Nodes,
    IReadOnlyList<FilePanelRow> Rows,
    int SelectedIndex,
    string SearchQuery,
    int SearchHitCount)
{
    public static FilePanelSnapshot From(FileExplorerState state) => new(
        state.VisibleNodes,
        state.VisibleNodes.Select(RowFrom).ToArray(),
        state.SelectedIndex,
        state.SearchQuery,
        state.VisibleNodes.Count(node => node.Kind == FileNodeKind.File));

    private static FilePanelRow RowFrom(VisibleFileNode node)
    {
        var marker = node.Kind == FileNodeKind.File
            ? "•"
            : node.IsExpanded ? "▼" : "▶";
        var tone = node.Kind == FileNodeKind.File
            ? node.Files[0].GitStatus switch
            {
                FileGitStatus.Modified => FileRowTone.Modified,
                FileGitStatus.New => FileRowTone.New,
                _ => FileRowTone.Neutral
            }
            : FileRowTone.Neutral;
        return new FilePanelRow($"{new string(' ', node.Depth * 2)}{marker} {node.Name}", tone);
    }
}
