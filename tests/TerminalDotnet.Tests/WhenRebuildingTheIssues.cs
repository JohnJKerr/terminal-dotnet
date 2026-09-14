using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using TerminalDotnet.Comments;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Issues;

/// <summary>The issues cannot answer without a build, so they are the one panel
/// left out of the reload an outside edit brings on. The reader asks instead.
/// </summary>
public sealed class WhenRebuildingTheIssues
{
    [Fact]
    public void Pressing_r_rebuilds_them()
    {
        // Act
        var action = IssuePanelKeyBindings.ActionFor(Key(KeyCode.R), Warning(), searchActive: false);

        // Assert
        Assert.IsType<IssuePanelAction.Rebuild>(action);
    }

    [Fact]
    public void It_rebuilds_even_when_there_is_nothing_listed_to_stand_on()
    {
        // Act
        var action = IssuePanelKeyBindings.ActionFor(Key(KeyCode.R), issue: null, searchActive: false);

        // Assert
        Assert.IsType<IssuePanelAction.Rebuild>(action);
    }

    [Fact]
    public void It_types_an_r_into_the_search_instead_while_the_reader_is_searching()
    {
        // Act
        var action = IssuePanelKeyBindings.ActionFor(Key(KeyCode.R), Warning(), searchActive: true);

        // Assert
        Assert.Null(action);
    }

    [Fact]
    public void The_shortcut_line_offers_it()
    {
        // Act
        var shortcuts = PanelShortcuts.For(
            PanelKind.Issues, Files(), Changes(), Tests(), Comments(),
            issueState: new IssueState([Warning()]) { Loading = false });

        // Assert
        Assert.Contains("r rebuild", shortcuts);
    }

    [Fact]
    public void The_shortcut_line_offers_it_when_nothing_is_listed()
    {
        // Act
        var shortcuts = PanelShortcuts.For(
            PanelKind.Issues, Files(), Changes(), Tests(), Comments(),
            issueState: new IssueState([]) { Loading = false });

        // Assert
        Assert.Contains("r rebuild", shortcuts);
    }

    [Fact]
    public void The_command_list_says_what_it_does()
    {
        // Act
        var issues = CommandMenu.Sections().Single(section => section.Title == "Issues");

        // Assert
        Assert.Contains(issues.Entries, entry => entry.Keys == "r" && entry.Description == "rebuild");
    }

    [Fact]
    public async Task A_rebuild_says_it_is_building_while_it_runs()
    {
        // Arrange
        var session = new IssueSession(new StubIssueBackend(), new SilentClipboard());
        await session.LoadAsync("App.csproj");

        // Act
        session.Rebuilding();

        // Assert
        Assert.Contains(
            IssuePanelSnapshot.From(session.State).StatusSegments,
            segment => segment.Text == "Building");
    }

    [Fact]
    public async Task It_keeps_the_issues_from_the_last_build_in_front_of_the_reader()
    {
        // Arrange
        var session = new IssueSession(new StubIssueBackend(), new SilentClipboard());
        await session.LoadAsync("App.csproj");

        // Act
        session.Rebuilding();

        // Assert
        Assert.Equal(2, session.State.Issues.Count);
    }

    [Fact]
    public async Task It_stops_saying_it_is_building_once_the_build_lands()
    {
        // Arrange
        var session = new IssueSession(new StubIssueBackend(), new SilentClipboard());
        session.Rebuilding();

        // Act
        await session.LoadAsync("App.csproj");

        // Assert
        Assert.DoesNotContain(
            IssuePanelSnapshot.From(session.State).StatusSegments,
            segment => segment.Text == "Building");
    }

    [Fact]
    public async Task It_keeps_the_row_the_reader_was_on()
    {
        // Arrange
        var session = new IssueSession(new StubIssueBackend(), new SilentClipboard());
        await session.LoadAsync("App.csproj");
        await session.DispatchAsync(new IssueCommand.SelectIndex(1));

        // Act
        await session.LoadAsync("App.csproj");

        // Assert
        Assert.Equal(1, session.State.SelectedIndex);
    }

    private static Key Key(KeyCode code) => new(code);

    private static CompilationIssue Warning() => new(
        "/repo/Order.cs", "Order.cs", 12, 3, "CS0168", "unused", IssueSeverity.Warning);

    private static TerminalDotnet.Files.FileExplorerState Files() => new([]);

    private static TerminalDotnet.Changes.ChangesetState Changes() => new([]);

    private static TerminalDotnet.Explorer.ExplorerState Tests() =>
        new(TerminalDotnet.Explorer.ExplorerStatus.Ready, [], 0, "");

    private static CommentsState Comments() => new([]);

    private sealed class StubIssueBackend : IIssueBackend
    {
        public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CompilationIssue>>(
            [
                new("/repo/Order.cs", "Order.cs", 12, 3, "CS0168", "unused", IssueSeverity.Warning),
                new("/repo/Basket.cs", "Basket.cs", 4, 1, "CS0219", "assigned", IssueSeverity.Warning)
            ]);
    }

    private sealed class SilentClipboard : ICommentClipboard
    {
        public Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
