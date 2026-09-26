using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

/// <summary>What each list panel has in front of the reader.</summary>
public sealed record PanelStates(
    FileExplorerState Files,
    ExplorerState Tests,
    ChangesetState Changes,
    IssueState Issues,
    CommentsState Comments);

/// <summary>
/// What the preview shows for the selection in the list it follows. A test's
/// source has to be searched for, and a change's diff asked of git, so those
/// are named here and fetched by whoever shows them.
/// </summary>
public abstract record PreviewSubject
{
    public sealed record Nothing : PreviewSubject;
    public sealed record SourceFile(string Path, int Line) : PreviewSubject;
    public sealed record SelectedTest(VisibleTestNode Node) : PreviewSubject;
    public sealed record ChangeDiff(string Path) : PreviewSubject;

    public static PreviewSubject For(
        PanelKind list,
        PanelStates panels,
        bool previewsChangedFile = false) =>
        SubjectAt(list, panels, SelectedRowOf(list, panels), previewsChangedFile);

    /// <returns>The nearest row in the direction of <paramref name="step"/>
    /// that has something to preview, or null when none does. A folder or a
    /// project has nothing to show, so stepping goes straight past it.</returns>
    public static int? NextShown(
        PanelKind list,
        PanelStates panels,
        int step,
        bool previewsChangedFile = false)
    {
        var rowCount = RowCountOf(list, panels);
        var row = SelectedRowOf(list, panels) + Math.Sign(step);
        for (; row >= 0 && row < rowCount; row += Math.Sign(step))
        {
            if (SubjectAt(list, panels, row, previewsChangedFile) is not Nothing)
            {
                return row;
            }
        }

        return null;
    }

    private static PreviewSubject SubjectAt(
        PanelKind list,
        PanelStates panels,
        int row,
        bool previewsChangedFile) => list switch
    {
        PanelKind.Explorer => Selected(panels.Files.VisibleNodes, row) is { Kind: FileNodeKind.File } node
            ? new SourceFile(node.Files[0].Path, 1)
            : new Nothing(),
        PanelKind.Tests => Selected(panels.Tests.VisibleNodes, row) is { } test
            ? new SelectedTest(test)
            : new Nothing(),
        PanelKind.Changes => Selected(panels.Changes.Files, row) is { } change
            ? Changed(change, previewsChangedFile)
            : new Nothing(),
        PanelKind.Issues => Selected(panels.Issues.Issues, row) is { } issue
            ? new SourceFile(issue.Path, issue.Line)
            : new Nothing(),
        PanelKind.Comments => Selected(panels.Comments.Comments, row) is { } comment
            ? new SourceFile(comment.Path, 1)
            : new Nothing(),
        _ => new Nothing()
    };

    private static int SelectedRowOf(PanelKind list, PanelStates panels) => list switch
    {
        PanelKind.Explorer => panels.Files.SelectedIndex,
        PanelKind.Tests => panels.Tests.SelectedIndex,
        PanelKind.Changes => panels.Changes.SelectedIndex,
        PanelKind.Issues => panels.Issues.SelectedIndex,
        PanelKind.Comments => panels.Comments.SelectedIndex,
        _ => 0
    };

    private static int RowCountOf(PanelKind list, PanelStates panels) => list switch
    {
        PanelKind.Explorer => panels.Files.VisibleNodes.Count,
        PanelKind.Tests => panels.Tests.VisibleNodes.Count,
        PanelKind.Changes => panels.Changes.Files.Count,
        PanelKind.Issues => panels.Issues.Issues.Count,
        PanelKind.Comments => panels.Comments.Comments.Count,
        _ => 0
    };

    /// <summary>A deleted file has nothing left to read but its diff.</summary>
    private static PreviewSubject Changed(ChangedFile change, bool previewsFile) =>
        previewsFile && change.Kind != ChangeKind.Deleted
            ? new SourceFile(change.Path, 1)
            : new ChangeDiff(change.Path);

    private static T? Selected<T>(IReadOnlyList<T> rows, int index) where T : class =>
        index >= 0 && index < rows.Count ? rows[index] : null;
}
