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

    public static PreviewSubject For(PanelKind list, PanelStates panels) => list switch
    {
        PanelKind.Explorer => Selected(panels.Files.VisibleNodes, panels.Files.SelectedIndex) is
            { Kind: FileNodeKind.File } node
            ? new SourceFile(node.Files[0].Path, 1)
            : new Nothing(),
        PanelKind.Tests => Selected(panels.Tests.VisibleNodes, panels.Tests.SelectedIndex) is { } test
            ? new SelectedTest(test)
            : new Nothing(),
        PanelKind.Changes => Selected(panels.Changes.Files, panels.Changes.SelectedIndex) is { } change
            ? new ChangeDiff(change.Path)
            : new Nothing(),
        PanelKind.Issues => Selected(panels.Issues.Issues, panels.Issues.SelectedIndex) is { } issue
            ? new SourceFile(issue.Path, issue.Line)
            : new Nothing(),
        PanelKind.Comments => Selected(panels.Comments.Comments, panels.Comments.SelectedIndex) is { } comment
            ? new SourceFile(comment.Path, 1)
            : new Nothing(),
        _ => new Nothing()
    };

    private static T? Selected<T>(IReadOnlyList<T> rows, int index) where T : class =>
        index >= 0 && index < rows.Count ? rows[index] : null;
}
