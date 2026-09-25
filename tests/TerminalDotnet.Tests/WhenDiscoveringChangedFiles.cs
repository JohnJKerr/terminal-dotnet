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
        var runner = new GitCommandRunner(TestPaths.Repo,"?? src/Added.cs\0 M src/Changed.cs\0 D src/Gone.cs\0");

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync(TestPaths.In("App.slnx"));

        // Assert
        Assert.Equal(
            [
                (Path.Combine("src", "Added.cs"), ChangeKind.Added),
                (Path.Combine("src", "Changed.cs"), ChangeKind.Modified),
                (Path.Combine("src", "Gone.cs"), ChangeKind.Deleted)
            ],
            files.Select(file => (file.DisplayPath, file.Kind)));
    }

    [Fact]
    public async Task It_asks_git_only_for_the_changes_under_the_directory_it_started_in()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo," M samples/App/Changed.cs\0");

        // Act
        await new GitChangesetBackend(runner).DiscoverAsync(TestPaths.In("samples", "App", "App.csproj"));

        // Assert
        Assert.Equal(
            ["status", "--porcelain=v1", "-z", "--untracked-files=all", "--", TestPaths.In("samples", "App")],
            runner.Requests.Single(arguments => arguments[0] == "status"));
    }

    [Fact]
    public async Task It_names_each_file_relative_to_the_directory_it_started_in()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo," M samples/App/Changed.cs\0");

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync(TestPaths.In("samples", "App", "App.csproj"));

        // Assert
        Assert.Equal("Changed.cs", files[0].DisplayPath);
    }

    [Fact]
    public async Task It_resolves_each_path_from_the_repository_root()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo," M samples/App/Changed.cs\0");

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync(TestPaths.In("samples", "App", "App.csproj"));

        // Assert
        Assert.Equal(TestPaths.In("samples", "App", "Changed.cs"), files[0].Path);
    }

    [Fact]
    public async Task It_reports_the_destination_of_a_renamed_file()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo,"R  src/New.cs\0src/Old.cs\0");

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync(TestPaths.In("App.slnx"));

        // Assert
        Assert.Equal(Path.Combine("src", "New.cs"), files[0].DisplayPath);
    }

    [Fact]
    public async Task It_reports_no_changes_outside_a_repository()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo,"") { RootExitCode = 128 };

        // Act
        var files = await new GitChangesetBackend(runner).DiscoverAsync(TestPaths.In("App.slnx"));

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public async Task It_asks_git_for_the_diff_of_a_modified_file()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo," M src/Changed.cs\0") { Diff = "@@ -1 +1 @@" };
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync(TestPaths.In("App.slnx"));

        // Act
        var diff = await backend.DiffAsync(files[0]);

        // Assert
        Assert.Equal("@@ -1 +1 @@", diff);
    }

    [Fact]
    public async Task It_asks_git_to_restore_a_deleted_file_from_the_index()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo," D src/Gone.cs\0");
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync(TestPaths.In("App.slnx"));

        // Act
        await backend.RestoreAsync(files[0]);

        // Assert
        Assert.Equal(
            ["restore", "--worktree", "--", $":(literal){TestPaths.In("src", "Gone.cs")}"],
            runner.Requests.Last());
    }

    [Fact]
    public async Task It_keeps_a_staged_edit_when_restoring_the_file_deleted_over_it()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo,"MD src/Gone.cs\0");
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync(TestPaths.In("App.slnx"));

        // Act
        await backend.RestoreAsync(files[0]);

        // Assert
        Assert.DoesNotContain("--staged", runner.Requests.Last());
    }

    [Fact]
    public async Task It_asks_git_to_restore_a_staged_deletion_from_the_last_commit()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo,"D  src/Gone.cs\0");
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync(TestPaths.In("App.slnx"));

        // Act
        await backend.RestoreAsync(files[0]);

        // Assert
        Assert.Equal(
            ["restore", "--staged", "--worktree", "--", $":(literal){TestPaths.In("src", "Gone.cs")}"],
            runner.Requests.Last());
    }

    [Fact]
    public async Task It_leaves_a_recreated_file_alone_when_restoring_the_deletion_staged_over_it()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo,"D  src/Gone.cs\0?? src/Gone.cs\0");
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync(TestPaths.In("App.slnx"));

        // Act
        await backend.RestoreAsync(files[0]);

        // Assert
        Assert.Equal(
            ["restore", "--staged", "--", $":(literal){TestPaths.In("src", "Gone.cs")}"],
            runner.Requests.Last());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task It_preserves_a_recreated_file_even_when_git_does_not_list_it(bool afterDiscovery)
    {
        // Arrange
        var directory = Directory.CreateTempSubdirectory("restore-recreated-");
        var path = Path.Combine(directory.FullName, "Gone.cs");
        try
        {
            var runner = new GitCommandRunner(directory.FullName, "D  Gone.cs\0");
            var backend = new GitChangesetBackend(runner);
            if (!afterDiscovery)
            {
                await File.WriteAllTextAsync(path, "new work");
            }

            var files = await backend.DiscoverAsync(Path.Combine(directory.FullName, "App.slnx"));
            if (afterDiscovery)
            {
                await File.WriteAllTextAsync(path, "new work");
            }

            // Act
            await backend.RestoreAsync(files[0]);

            // Assert
            Assert.DoesNotContain("--worktree", runner.Requests.Last());
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task It_restores_only_the_file_whose_name_reads_as_a_pattern()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo," D src/*.cs\0 M src/Other.cs\0");
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync(TestPaths.In("App.slnx"));

        // Act
        await backend.RestoreAsync(files[0]);

        // Assert
        Assert.Equal(
            ["restore", "--worktree", "--", $":(literal){TestPaths.In("src", "*.cs")}"],
            runner.Requests.Last());
    }

    [Fact]
    public async Task It_turns_off_the_file_system_monitor_a_repository_configures()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo," D src/Gone.cs\0") { Diff = "@@ -1 +0,0 @@" };
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync(TestPaths.In("App.slnx"));

        // Act
        await backend.DiffAsync(files[0]);
        await backend.RestoreAsync(files[0]);

        // Assert
        Assert.Equal(
            ["-c core.fsmonitor=false"],
            runner.Invocations.Select(arguments => string.Join(' ', arguments.Take(2))).Distinct());
    }

    [Fact]
    public async Task It_shows_a_diff_without_the_repository_configured_diff_tools()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo," M src/Changed.cs\0") { Diff = "@@ -1 +1 @@" };
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync(TestPaths.In("App.slnx"));

        // Act
        await backend.DiffAsync(files[0]);

        // Assert
        Assert.Equal(
            ["diff", "--no-ext-diff", "--no-textconv", "HEAD", "--", $":(literal){TestPaths.In("src", "Changed.cs")}"],
            runner.Requests.Last());
    }

    [Fact]
    public async Task It_reports_a_restore_git_refused()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo," D src/Gone.cs\0") { RestoreExitCode = 1 };
        var backend = new GitChangesetBackend(runner);
        var files = await backend.DiscoverAsync(TestPaths.In("App.slnx"));

        // Act
        var restored = await backend.RestoreAsync(files[0]);

        // Assert
        Assert.False(restored);
    }

    [Fact]
    public async Task It_diffs_a_file_it_is_handed_without_having_discovered_it()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo, "") { Diff = "@@ -1 +1 @@" };
        var backend = new GitChangesetBackend(runner);

        // Act
        var diff = await backend.DiffAsync(
            new ChangedFile(TestPaths.In("src", "Changed.cs"), "src/Changed.cs", ChangeKind.Modified));

        // Assert
        Assert.Equal("@@ -1 +1 @@", diff);
    }

    [Fact]
    public async Task It_restores_a_file_it_is_handed_without_having_discovered_it()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo, "");
        var backend = new GitChangesetBackend(runner);

        // Act
        var restored = await backend.RestoreAsync(
            new ChangedFile(TestPaths.In("src", "Gone.cs"), "src/Gone.cs", ChangeKind.Deleted)
            {
                Unstaged = ChangeKind.Deleted
            });

        // Assert
        Assert.True(restored);
    }

    [Fact]
    public async Task It_asks_git_about_a_deleted_file_from_the_nearest_folder_still_on_disk()
    {
        // Arrange
        var runner = new GitCommandRunner(TestPaths.Repo, "");
        var backend = new GitChangesetBackend(runner);
        var gone = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}", "Gone.cs");

        // Act
        await backend.RestoreAsync(new ChangedFile(gone, "Gone.cs", ChangeKind.Deleted));

        // Assert
        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetTempPath()), runner.WorkingDirectories.Last());
    }

    private sealed class GitCommandRunner(string root, string status) : ICommandRunner
    {
        public int RootExitCode { get; init; }

        public int RestoreExitCode { get; init; }

        public string Diff { get; init; } = "";

        /// <summary>Each command as git is asked to run it, settings included.</summary>
        public List<IReadOnlyList<string>> Invocations { get; } = [];

        /// <summary>Each command's own arguments, after any `-c` settings.</summary>
        public List<IReadOnlyList<string>> Requests { get; } = [];

        public List<string> WorkingDirectories { get; } = [];

        public Task<CommandResult> RunAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default)
        {
            Invocations.Add(request.Arguments);
            WorkingDirectories.Add(request.WorkingDirectory);
            var command = WithoutSettings(request.Arguments);
            Requests.Add(command);
            return Task.FromResult(command[0] switch
            {
                "rev-parse" => new CommandResult(RootExitCode, root, ""),
                "status" => new CommandResult(0, status, ""),
                "restore" => new CommandResult(RestoreExitCode, "", ""),
                _ => new CommandResult(0, Diff, "")
            });
        }

        private static IReadOnlyList<string> WithoutSettings(IReadOnlyList<string> arguments) =>
            arguments.Count > 2 && arguments[0] == "-c"
                ? WithoutSettings([.. arguments.Skip(2)])
                : arguments;
    }
}
