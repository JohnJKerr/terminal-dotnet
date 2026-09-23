using TerminalDotnet.Comments;
using TerminalDotnet.Flags;
using TerminalDotnet.Issues;
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
        var folder = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-flags-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "Work.cs");
        await File.WriteAllLinesAsync(path, ["//todo first", "// TODO: second", "/*fixme*/", "// ReViEw consider this"]);
        var session = Session(new FileFlagBackend(new StubFileBackend(path)));

        // Act
        await session.LoadFlagsAsync(Path.Combine(folder, "App.slnx"));

        // Assert
        Assert.Equal(
            [("TODO", "first"), ("TODO", "second"), ("FIXME", ""), ("REVIEW", "consider this")],
            session.State.Issues.Select(issue => (issue.Code, issue.Message)));
    }

    [Fact(Timeout = 10_000)]
    public async Task It_skips_a_named_pipe_rather_than_waiting_for_a_writer()
    {
        // Arrange
        var folder = Directory.CreateTempSubdirectory("terminal-dotnet-flags-");
        try
        {
            var path = Path.Combine(folder.FullName, "Pipe.cs");
            if (!NamedPipe.TryCreate(path))
            {
                return;
            }

            var session = Session(new FileFlagBackend(new StubFileBackend(path)));

            // Act
            await Task.Run(() => session.LoadFlagsAsync(Path.Combine(folder.FullName, "App.slnx")));

            // Assert
            Assert.Empty(session.State.Issues);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    private static IssueSession Session(IFlagBackend flags) =>
        new(new NoIssues(), new UnusedClipboard(), flags);

    private sealed class NoIssues : IIssueBackend
    {
        public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CompilationIssue>>([]);
    }

    private sealed class UnusedClipboard : ICommentClipboard
    {
        public Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

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
