using TerminalDotnet.Comments;
using TerminalDotnet.Issues;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Issues;

public sealed class WhenUsingTheIssuesPanel
{
    [Fact]
    public async Task It_can_narrow_the_issues_to_the_warnings()
    {
        // Arrange
        var session = Session();
        await session.LoadAsync("/repo/Shop.slnx");

        // Act
        await session.DispatchAsync(new IssueCommand.ToggleWarnings());

        // Assert
        Assert.Equal([IssueSeverity.Warning], session.State.Issues.Select(issue => issue.Severity));
    }

    [Fact]
    public void It_starts_without_a_selected_filter()
    {
        // Arrange
        var state = new IssueState(Issues()) { Loading = false };

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.DoesNotContain(snapshot.Filters, filter => filter.IsActive);
    }

    [Fact]
    public async Task It_returns_to_all_issues_when_the_active_filter_is_toggled()
    {
        // Arrange
        var session = Session();
        await session.LoadAsync("/repo/Shop.slnx");
        await session.DispatchAsync(new IssueCommand.ToggleErrors());

        // Act
        await session.DispatchAsync(new IssueCommand.ToggleErrors());

        // Assert
        Assert.Equal(2, session.State.Issues.Count);
    }

    [Fact]
    public async Task It_copies_the_selected_issue_details()
    {
        // Arrange
        var clipboard = new RememberingClipboard();
        var session = Session(clipboard);
        await session.LoadAsync("/repo/Shop.slnx");

        // Act
        await session.DispatchAsync(new IssueCommand.CopySelected());

        // Assert
        Assert.Equal("src/Broken.cs(7,3): error CS1002: ; expected", clipboard.Text);
    }

    [Fact]
    public async Task It_moves_through_the_issues()
    {
        // Arrange
        var session = Session();
        await session.LoadAsync("/repo/Shop.slnx");

        // Act
        await session.DispatchAsync(new IssueCommand.MoveDown());

        // Assert
        Assert.Equal(1, session.State.SelectedIndex);
    }

    [Fact]
    public void It_renders_errors_red_and_warnings_yellow()
    {
        // Arrange
        var state = new IssueState(Issues()) { Loading = false };

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.Equal([FileRowTone.Deleted, FileRowTone.Warning], snapshot.Rows.Select(row => row.Tone));
    }

    [Fact]
    public void It_exposes_the_complete_selected_issue_for_wrapped_display()
    {
        // Arrange
        var issue = Issues()[0] with { Message = "A diagnostic message that is wider than the panel" };
        var state = new IssueState([issue]) { Loading = false };

        // Act
        var snapshot = IssuePanelSnapshot.From(state);

        // Assert
        Assert.Equal(issue.Details, snapshot.SelectedDetails);
    }

    [Fact]
    public void It_wraps_issue_details_inline_to_the_panel_width()
    {
        // Arrange
        var state = new IssueState([Issues()[0] with { Message = "one two three four" }]) { Loading = false };

        // Act
        var layout = IssuePanelLayout.From(IssuePanelSnapshot.From(state), 24);

        // Assert
        Assert.All(layout.Rows, row => Assert.True(row.Text.Length <= 24));
    }

    [Fact]
    public void It_leaves_a_line_between_issues()
    {
        // Arrange
        var state = new IssueState(Issues()) { Loading = false };

        // Act
        var layout = IssuePanelLayout.From(IssuePanelSnapshot.From(state), 200);

        // Assert
        Assert.Equal("", layout.Rows[1].Text);
    }

    [Fact]
    public void It_maps_selection_to_the_first_wrapped_line_of_the_issue()
    {
        // Arrange
        var state = new IssueState(Issues(), SelectedIndex: 1) { Loading = false };

        // Act
        var layout = IssuePanelLayout.From(IssuePanelSnapshot.From(state), 20);

        // Assert
        Assert.StartsWith("src/Risky.cs", layout.Rows[layout.SelectedRowIndex].Text);
    }

    [Fact]
    public void It_finds_the_issue_behind_a_wrapped_row()
    {
        // Arrange
        var state = new IssueState(Issues()) { Loading = false };
        var layout = IssuePanelLayout.From(IssuePanelSnapshot.From(state), 20);

        // Act
        var issue = layout.IssueAt(layout.Rows.Count - 1);

        // Assert
        Assert.Equal(1, issue);
    }

    [Fact]
    public void It_finds_the_issue_above_the_gap_between_two()
    {
        // Arrange
        var state = new IssueState(Issues()) { Loading = false };
        var layout = IssuePanelLayout.From(IssuePanelSnapshot.From(state), 200);

        // Act
        var issue = layout.IssueAt(1);

        // Assert
        Assert.Equal(0, issue);
    }

    private static IssueSession Session(ICommentClipboard? clipboard = null) =>
        new(new FixedBackend(Issues()), clipboard ?? new RememberingClipboard());

    private static IReadOnlyList<CompilationIssue> Issues() =>
    [
        new("/repo/src/Broken.cs", "src/Broken.cs", 7, 3, "CS1002", "; expected", IssueSeverity.Error),
        new("/repo/src/Risky.cs", "src/Risky.cs", 8, 4, "CS0168", "Unused variable", IssueSeverity.Warning)
    ];

    private sealed class FixedBackend(IReadOnlyList<CompilationIssue> issues) : IIssueBackend
    {
        public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(string target, CancellationToken cancellationToken = default) => Task.FromResult(issues);
    }

    private sealed class RememberingClipboard : ICommentClipboard
    {
        public string Text { get; private set; } = "";
        public Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default)
        {
            Text = text;
            return Task.FromResult(true);
        }
    }
}
