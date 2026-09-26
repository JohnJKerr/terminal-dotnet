using TerminalDotnet.Comments;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Flags;

public sealed class WhenBrowsingTheFlags
{
    [Fact]
    public async Task It_lists_flags_in_category_and_kind_order()
    {
        // Arrange
        var session = Session(new StubBackend([
            new Flag("Bug.cs", "Bug.cs", 8, FlagKind.Bug, "BUG broken"),
            new Flag("Todo.cs", "Todo.cs", 2, FlagKind.Todo, "TODO do this"),
            new Flag("Fix.cs", "Fix.cs", 4, FlagKind.Fixme, "FIXME repair")
        ]));

        // Act
        await session.LoadFlagsAsync("App.slnx");

        // Assert
        Assert.Equal(["TODO", "FIXME", "BUG"], session.State.Issues.Select(issue => issue.Code));
    }

    [Fact]
    public async Task It_recognizes_comment_flags_without_regard_to_spacing_or_case()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create()
            .WithFile("Work.cs", "//todo first\n// TODO: second\n/*fixme*/\n// ReViEw consider this\n");
        var session = Session(new FileFlagBackend(new StubFileBackend(workspace.PathTo("Work.cs"))));

        // Act
        await session.LoadFlagsAsync(workspace.PathTo("App.slnx"));

        // Assert
        Assert.Equal(
            [("TODO", "first"), ("TODO", "second"), ("FIXME", ""), ("REVIEW", "consider this")],
            session.State.Issues.Select(issue => (issue.Code, issue.Message)));
    }

    [Fact(Timeout = 10_000)]
    public async Task It_skips_a_named_pipe_rather_than_waiting_for_a_writer()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create();
        if (!NamedPipe.TryCreate(workspace.PathTo("Pipe.cs")))
        {
            return;
        }

        var session = Session(new FileFlagBackend(new StubFileBackend(workspace.PathTo("Pipe.cs"))));

        // Act
        await Task.Run(() => session.LoadFlagsAsync(workspace.PathTo("App.slnx")));

        // Assert
        Assert.Empty(session.State.Issues);
    }

    private static IssueSession Session(IFlagBackend flags) => GivenA.IssuePanel().WithFlagBackend(flags).Build();

    private sealed class StubBackend(IReadOnlyList<Flag> flags) : IFlagBackend
    {
        public Task<IReadOnlyList<Flag>> DiscoverAsync(string target, CancellationToken cancellationToken = default) =>
            Task.FromResult(flags);
    }

    private sealed class StubFileBackend(string path) : TerminalDotnet.Files.IFileExplorerBackend
    {
        public Task<IReadOnlyList<TerminalDotnet.Files.FileEntry>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TerminalDotnet.Files.FileEntry>>([
                new("App.csproj", path, TerminalDotnet.Files.FileGitStatus.Unchanged)
            ]);
    }
}
