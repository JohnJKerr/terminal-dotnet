using TerminalDotnet.Changes;
using TerminalDotnet.Comments;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Issues;

namespace TerminalDotnet.Terminal;

/// <summary>
/// What the shell asks of whichever list panel it is working with: search
/// it, move through it, or pick a row in it. Each panel answers through the
/// commands of its own session.
/// </summary>
internal interface IListNavigation
{
    string SearchQuery { get; }

    Task SearchAsync(string query);

    Task ClearSearchAsync();

    /// <summary>A row the reader picked in the list, such as with a click.
    /// </summary>
    Task ChooseRowAsync(int row);

    Task StepAsync(bool down);
}

/// <summary>The Explorer browses the projects' files or every file beneath
/// the launch folder, so it asks which session it is showing each time.
/// </summary>
internal sealed class FileListNavigation(Func<FileExplorerSession> shown) : IListNavigation
{
    public string SearchQuery => shown().State.SearchQuery;

    public Task SearchAsync(string query) => shown().DispatchAsync(new FileExplorerCommand.Search(query));

    public Task ClearSearchAsync() => shown().DispatchAsync(new FileExplorerCommand.ClearSearch());

    public Task ChooseRowAsync(int row) => shown().DispatchAsync(new FileExplorerCommand.SelectIndex(row));

    public Task StepAsync(bool down) => shown().DispatchAsync(
        down ? new FileExplorerCommand.MoveDown() : new FileExplorerCommand.MoveUp());
}

internal sealed class TestListNavigation(TestExplorerSession tests) : IListNavigation
{
    public string SearchQuery => tests.State.SearchQuery;

    public Task SearchAsync(string query) => tests.DispatchAsync(new ExplorerCommand.Search(query));

    public Task ClearSearchAsync() => tests.DispatchAsync(new ExplorerCommand.ClearSearch());

    public Task ChooseRowAsync(int row) => tests.DispatchAsync(new ExplorerCommand.SelectIndex(row));

    public Task StepAsync(bool down) => tests.DispatchAsync(
        down ? new ExplorerCommand.MoveDown() : new ExplorerCommand.MoveUp());
}

internal sealed class ChangesetListNavigation(ChangesetSession changes) : IListNavigation
{
    public string SearchQuery => changes.State.SearchQuery;

    public Task SearchAsync(string query) => changes.DispatchAsync(new ChangesetCommand.Search(query));

    public Task ClearSearchAsync() => changes.DispatchAsync(new ChangesetCommand.ClearSearch());

    public Task ChooseRowAsync(int row) => changes.DispatchAsync(new ChangesetCommand.SelectIndex(row));

    public Task StepAsync(bool down) => changes.DispatchAsync(
        down ? new ChangesetCommand.MoveDown() : new ChangesetCommand.MoveUp());
}

/// <summary>An issue wraps across several rows, so a chosen row is turned
/// into the issue it belongs to before it is selected.</summary>
internal sealed class IssueListNavigation(IssueSession issues, Func<int, int> issueAtRow) : IListNavigation
{
    public string SearchQuery => issues.State.SearchQuery;

    public Task SearchAsync(string query) => issues.DispatchAsync(new IssueCommand.Search(query));

    public Task ClearSearchAsync() => issues.DispatchAsync(new IssueCommand.ClearSearch());

    public Task ChooseRowAsync(int row) => issues.DispatchAsync(new IssueCommand.SelectIndex(issueAtRow(row)));

    public Task StepAsync(bool down) => issues.DispatchAsync(
        down ? new IssueCommand.MoveDown() : new IssueCommand.MoveUp());
}

internal sealed class CommentListNavigation(CommentSession comments) : IListNavigation
{
    public string SearchQuery => comments.State.SearchQuery;

    public Task SearchAsync(string query) => comments.DispatchAsync(new CommentCommand.Search(query));

    public Task ClearSearchAsync() => comments.DispatchAsync(new CommentCommand.ClearSearch());

    public Task ChooseRowAsync(int row) => comments.DispatchAsync(new CommentCommand.SelectIndex(row));

    public Task StepAsync(bool down) => comments.DispatchAsync(
        down ? new CommentCommand.MoveDown() : new CommentCommand.MoveUp());
}
