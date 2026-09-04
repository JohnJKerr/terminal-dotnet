using TerminalDotnet.Files;
using Terminal.Gui.Drawing;

namespace TerminalDotnet.Terminal;

public enum FileRowTone
{
    Neutral,
    Modified,
    New,
    Deleted
}

public sealed record FilePanelRow(string Text, FileRowTone Tone);

public sealed record FileStatusSegment(string Text, FileRowTone Tone);

public static class FileRowAppearance
{
    public static global::Terminal.Gui.Drawing.Attribute For(
        FileRowTone tone,
        bool isSelected,
        global::Terminal.Gui.Drawing.Attribute normal,
        global::Terminal.Gui.Drawing.Attribute selected)
    {
        var baseAppearance = isSelected ? selected : normal;
        return new global::Terminal.Gui.Drawing.Attribute(
            ForegroundFor(tone, baseAppearance.Foreground),
            baseAppearance.Background);
    }

    public static Color ForegroundFor(FileRowTone tone, Color unchanged) => tone switch
    {
        FileRowTone.Modified => Color.BrightBlue,
        FileRowTone.New => Color.BrightGreen,
        FileRowTone.Deleted => Color.BrightRed,
        _ => unchanged
    };
}

public sealed record FilePanelSnapshot(
    IReadOnlyList<VisibleFileNode> Nodes,
    IReadOnlyList<FilePanelRow> Rows,
    int SelectedIndex,
    string SearchQuery,
    int SearchHitCount,
    IReadOnlyList<FileStatusSegment> StatusSegments,
    string EmptyMessage)
{
    public static FilePanelSnapshot From(FileExplorerState state) => new(
        state.VisibleNodes,
        state.VisibleNodes.Select(RowFrom).ToArray(),
        state.SelectedIndex,
        state.SearchQuery,
        state.VisibleNodes.Count(node => node.Kind == FileNodeKind.File),
        StatusSegmentsFrom(state.Changes),
        PanelEmptyState.For("files", state.VisibleNodes.Count, state.SearchQuery));

    private static IReadOnlyList<FileStatusSegment> StatusSegmentsFrom(FileChangeSummary changes) =>
    [
        new($"{changes.Total} Files", FileRowTone.Neutral),
        new($"{changes.Added} Added", FileRowTone.New),
        new($"{changes.Edited} Edited", FileRowTone.Modified),
        new($"{changes.Deleted} Deleted", FileRowTone.Deleted)
    ];

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
                FileGitStatus.Deleted => FileRowTone.Deleted,
                _ => FileRowTone.Neutral
            }
            : FileRowTone.Neutral;
        return new FilePanelRow($"{new string(' ', node.Depth * 2)}{marker} {node.Name}", tone);
    }
}
