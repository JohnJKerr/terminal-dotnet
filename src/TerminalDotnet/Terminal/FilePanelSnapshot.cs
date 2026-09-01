using TerminalDotnet.Files;
using Terminal.Gui.Drawing;

namespace TerminalDotnet.Terminal;

public enum FileRowTone
{
    Neutral,
    Modified,
    New
}

public sealed record FilePanelRow(string Text, FileRowTone Tone);

public static class FileRowAppearance
{
    public static global::Terminal.Gui.Drawing.Attribute For(
        FileRowTone tone,
        bool isSelected,
        global::Terminal.Gui.Drawing.Attribute normal,
        global::Terminal.Gui.Drawing.Attribute selected)
    {
        var baseAppearance = isSelected ? selected : normal;
        var foreground = tone switch
        {
            FileRowTone.Modified => Color.BrightBlue,
            FileRowTone.New => Color.BrightGreen,
            _ => baseAppearance.Foreground
        };
        return new global::Terminal.Gui.Drawing.Attribute(foreground, baseAppearance.Background);
    }
}

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
