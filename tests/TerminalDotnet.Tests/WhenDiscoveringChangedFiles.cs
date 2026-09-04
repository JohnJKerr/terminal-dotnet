using TerminalDotnet.Changes;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Changeset;

public sealed class WhenDiscoveringChangedFiles
{
    [Fact]
    public async Task It_reports_the_added_modified_and_deleted_files_git_lists()
    {
        // Arrange
        var runner = new GitCommandRunner("/repo", "?? src/Added.cs\n M src/Changed.cs\n D src/Gone.cs\n");

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync("/repo/App.slnx");

        // Assert
        Assert.Equal(
            [
                ("src/Added.cs", ChangeKind.Added),
                ("src/Changed.cs", ChangeKind.Modified),
                ("src/Gone.cs", ChangeKind.Deleted)
            ],
            files.Select(file => (file.DisplayPath, file.Kind)));
    }

    [Fact]
    public async Task It_asks_git_only_for_the_changes_under_the_directory_it_started_in()
    {
        // Arrange
        var runner = new GitCommandRunner("/repo", " M samples/App/Changed.cs\n");

        // Act
        await new GitChangesetBackend(runner).DiscoverAsync("/repo/samples/App/App.csproj");

        // Assert
        Assert.Equal(
            ["status", "--porcelain=v1", "--untracked-files=all", "--", "/repo/samples/App"],
            runner.Requests.Single(arguments => arguments[0] == "status"));
    }

    [Fact]
    public async Task It_names_each_file_relative_to_the_directory_it_started_in()
    {
        // Arrange
        var runner = new GitCommandRunner("/repo", " M samples/App/Changed.cs\n");

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync("/repo/samples/App/App.csproj");

        // Assert
        Assert.Equal("Changed.cs", files[0].DisplayPath);
    }

    [Fact]
    public async Task It_resolves_each_path_from_the_repository_root()
    {
        // Arrange
        var runner = new GitCommandRunner("/repo", " M samples/App/Changed.cs\n");

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync("/repo/samples/App/App.csproj");

        // Assert
        Assert.Equal("/repo/samples/App/Changed.cs", files[0].Path);
    }

    [Fact]
    public async Task It_reports_the_destination_of_a_renamed_file()
    {
        // Arrange
        var runner = new GitCommandRunner("/repo", "R  src/Old.cs -> src/New.cs\n");

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync("/repo/App.slnx");

        // Assert
        Assert.Equal("src/New.cs", files[0].DisplayPath);
    }

    [Fact]
    public async Task It_reports_no_changes_outside_a_repository()
    {
        // Arrange
        var runner = new GitCommandRunner("/repo", "") { RootExitCode = 128 };

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync("/repo/App.slnx");

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public async Task It_asks_git_for_the_diff_of_a_modified_file()
    {
        // Arrange
        var runner = new GitCommandRunner("/repo", " M src/Changed.cs\n") { Diff = "@@ -1 +1 @@" };
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync("/repo/App.slnx");

        // Act
        var diff = await backend.DiffAsync(files[0]);

        // Assert
        Assert.Equal("@@ -1 +1 @@", diff);
    }

    [Fact]
    public async Task It_asks_git_to_restore_a_deleted_file()
    {
        // Arrange
        var runner = new GitCommandRunner("/repo", " D src/Gone.cs\n");
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync("/repo/App.slnx");

        // Act
        await backend.RestoreAsync(files[0]);

        // Assert
        Assert.Equal(
            ["restore", "--staged", "--worktree", "--", "/repo/src/Gone.cs"],
            runner.Requests.Last());
    }

    private sealed class GitCommandRunner(string root, string status) : ICommandRunner
    {
        public int RootExitCode { get; init; }

        public string Diff { get; init; } = "";

        public List<IReadOnlyList<string>> Requests { get; } = [];

        public Task<CommandResult> RunAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request.Arguments);
            return Task.FromResult(request.Arguments[0] switch
            {
                "rev-parse" => new CommandResult(RootExitCode, root, ""),
                "status" => new CommandResult(0, status, ""),
                _ => new CommandResult(0, Diff, "")
            });
        }
    }
}
