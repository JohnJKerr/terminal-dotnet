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

public sealed record PlacedStatusSegment(string Text, FileRowTone Tone, int Column);

public static class StatusSegmentLayout
{
    public static IReadOnlyList<PlacedStatusSegment> Place(
        IReadOnlyList<FileStatusSegment> segments,
        int firstColumn,
        int gap)
    {
        var column = firstColumn;
        var placed = new List<PlacedStatusSegment>();
        foreach (var segment in segments)
        {
            placed.Add(new PlacedStatusSegment(segment.Text, segment.Tone, column));
            column += segment.Text.Length + gap;
        }

        return placed;
    }
}

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

    private static FilePanelRow RowFrom(VisibleFileNode node) => new(
        $"{new string(' ', node.Depth * 2)}{MarkerFor(node)} {node.Name}",
        ToneFor(node));

    private static string MarkerFor(VisibleFileNode node)
    {
        if (node.Kind == FileNodeKind.File)
        {
            return "•";
        }

        return node.IsExpanded ? "▼" : "▶";
    }

    private static FileRowTone ToneFor(VisibleFileNode node)
    {
        if (node.Kind != FileNodeKind.File)
        {
            return FileRowTone.Neutral;
        }

        return node.Files[0].GitStatus switch
        {
            FileGitStatus.Modified => FileRowTone.Modified,
            FileGitStatus.New => FileRowTone.New,
            FileGitStatus.Deleted => FileRowTone.Deleted,
            _ => FileRowTone.Neutral
        };
    }
}
