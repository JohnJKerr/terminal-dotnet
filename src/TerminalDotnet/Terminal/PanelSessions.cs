using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

/// <summary>The sessions behind the list panels, one for each thing the
/// reader can browse.</summary>
public sealed record PanelSessions(
    TestExplorerSession Tests,
    FileExplorerSession ProjectFiles,
    FileExplorerSession FolderFiles,
    ChangesetSession Changes,
    CommentSession Comments,
    IssueSession Issues);
