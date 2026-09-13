using TerminalDotnet.Flags;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Flags;

public sealed class WhenBrowsingTheFlags
{
    [Fact]
    public async Task It_lists_flags_in_category_and_kind_order()
    {
        // Arrange
        var session = new FlagSession(new StubBackend([
            new Flag("Bug.cs", "Bug.cs", 8, FlagKind.Bug, "BUG broken"),
            new Flag("Todo.cs", "Todo.cs", 2, FlagKind.Todo, "TODO do this"),
            new Flag("Fix.cs", "Fix.cs", 4, FlagKind.Fixme, "FIXME repair")
        ]));

        // Act
        await session.LoadAsync("App.slnx");

        // Assert
        Assert.Equal([FlagKind.Todo, FlagKind.Fixme, FlagKind.Bug], session.State.Flags.Select(flag => flag.Kind));
    }

    [Fact]
    public async Task It_recognizes_comment_flags_without_regard_to_spacing_or_case()
    {
        // Arrange
        var folder = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-flags-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "Work.cs");
        await File.WriteAllLinesAsync(path, ["//todo first", "// TODO: second", "/*fixme*/", "// ReViEw consider this"]);
        var session = new FlagSession(new FileFlagBackend(new StubFileBackend(path)));

        // Act
        await session.LoadAsync(Path.Combine(folder, "App.slnx"));

        // Assert
        Assert.Equal(
            [(FlagKind.Todo, "first"), (FlagKind.Todo, "second"), (FlagKind.Fixme, ""), (FlagKind.Review, "consider this")],
            session.State.Flags.Select(flag => (flag.Kind, flag.Comment)));
    }

    [Fact]
    public async Task It_filters_flags_by_the_four_categories()
    {
        // Arrange
        var session = new FlagSession(new StubBackend([
            new Flag("a", "a", 1, FlagKind.Todo, "task"),
            new Flag("b", "b", 2, FlagKind.Note, "context"),
            new Flag("c", "c", 3, FlagKind.Hack, "warning"),
            new Flag("d", "d", 4, FlagKind.Optimize, "improve")
        ]));
        await session.LoadAsync("App.slnx");

        // Act
        await session.DispatchAsync(new FlagCommand.ToggleFilter(FlagCategory.Review));

        // Assert
        Assert.Equal([FlagKind.Note], session.State.Flags.Select(flag => flag.Kind));
    }

    [Fact]
    public void It_places_the_heading_above_the_location_and_comment()
    {
        // Arrange
        var state = new FlagState([new Flag("Work.cs", "src/Work.cs", 12, FlagKind.Todo, "finish it")]);

        // Act
        var snapshot = FlagPanelSnapshot.From(state);

        // Assert
        Assert.Equal(["TODO", "  src/Work.cs:12", "    finish it"], snapshot.Rows.Select(row => row.Text));
    }

    [Fact]
    public void It_exposes_the_complete_selected_flag_for_the_preview()
    {
        // Arrange
        var state = new FlagState([new Flag("Work.cs", "src/Work.cs", 12, FlagKind.Todo, "finish it")]);

        // Act
        var snapshot = FlagPanelSnapshot.From(state);

        // Assert
        Assert.Equal("src/Work.cs:12: TODO finish it", snapshot.SelectedDetails);
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
