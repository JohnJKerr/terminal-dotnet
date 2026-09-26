using TerminalDotnet.Files;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Files;

public sealed class WhenDiscoveringProjectFiles
{
    private const string Solution = "TerminalDotnet.slnx";

    [Fact]
    public async Task It_finds_the_files_belonging_to_each_project()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution()
            .WithFile("src/App/Order.cs", "namespace App.Domain; public sealed class Order;");
        var runner = new RepositoryCommandRunner(workspace.Root, "", "App.csproj\0Order.cs\0");

        // Act
        var files = await DiscoverAsync(workspace, runner);

        // Assert
        Assert.Equal(
            [("App.csproj", "App.csproj"), ("App.csproj", "Order.cs")],
            files.Select(file => (Path.GetFileName(file.ProjectPath), Path.GetFileName(file.Path))));
    }

    [Fact]
    public async Task It_marks_modified_and_new_files_from_git_status()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution()
            .WithFile("src/App/Changed.cs", "namespace App;")
            .WithFile("src/App/Added.cs", "namespace App;");
        var runner = new RepositoryCommandRunner(
            workspace.Root,
            " M src/App/Changed.cs\0?? src/App/Added.cs\0",
            "Added.cs\0Changed.cs\0");

        // Act
        var files = await DiscoverAsync(workspace, runner);

        // Assert
        Assert.Equal(
            [("Added.cs", FileGitStatus.New), ("Changed.cs", FileGitStatus.Modified)],
            files.OrderBy(file => file.Path).Select(file => (Path.GetFileName(file.Path), file.GitStatus)));
    }

    [PosixFact]
    public async Task It_lists_a_tracked_file_whose_name_holds_a_newline()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution()
            .WithFile("src/App/two\nlines.cs", "namespace App;");
        var runner = new RepositoryCommandRunner(workspace.Root, "", "two\nlines.cs\0");

        // Act
        var files = await DiscoverAsync(workspace, runner);

        // Assert
        Assert.Equal(["two\nlines.cs"], files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_reports_files_git_says_were_deleted()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution();
        var runner = new RepositoryCommandRunner(workspace.Root, " D src/App/Gone.cs\0");

        // Act
        var files = await DiscoverAsync(workspace, runner);

        // Assert
        Assert.Equal(
            [("Gone.cs", FileGitStatus.Deleted)],
            files.Select(file => (Path.GetFileName(file.Path), file.GitStatus)));
    }

    [Fact]
    public async Task It_ignores_deletions_outside_the_solution_projects()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution();
        var runner = new RepositoryCommandRunner(workspace.Root, " D docs/Notes.md\0 D other/Gone.cs\0");

        // Act
        var files = await DiscoverAsync(workspace, runner);

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public async Task It_resolves_git_paths_from_the_repository_root()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create()
            .WithFile("samples/App/App.csproj", "<Project />")
            .WithFile("samples/App/Changed.cs", "namespace App;");
        var runner = new RepositoryCommandRunner(workspace.Root, " M samples/App/Changed.cs\0", "Changed.cs\0");

        // Act
        var files = await new FileSystemExplorerBackend(runner)
            .DiscoverAsync(workspace.PathTo("samples/App/App.csproj"));

        // Assert
        Assert.Equal(FileGitStatus.Modified, files[0].GitStatus);
    }

    [Fact]
    public async Task It_finds_files_that_are_not_csharp_sources()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution()
            .WithFile("src/App/appsettings.json", "{}");
        var runner = new RepositoryCommandRunner(workspace.Root, "", "App.csproj\0appsettings.json\0");

        // Act
        var files = await DiscoverAsync(workspace, runner);

        // Assert
        Assert.Contains("appsettings.json", files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_leaves_out_files_git_ignores()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution()
            .WithFile("src/App/secrets.env", "TOKEN=1");
        var runner = new RepositoryCommandRunner(workspace.Root, "", "App.csproj\0");

        // Act
        var files = await DiscoverAsync(workspace, runner);

        // Assert
        Assert.DoesNotContain("secrets.env", files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_falls_back_to_the_files_on_disk_outside_a_repository()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution()
            .WithFile("src/App/appsettings.json", "{}");

        // Act
        var files = await DiscoverAsync(workspace, new UntrackedCommandRunner());

        // Assert
        Assert.Equal(
            ["App.csproj", "appsettings.json"],
            files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_leaves_build_output_out_of_the_files_it_finds_on_disk()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution()
            .WithFile("src/App/obj/App.AssemblyInfo.cs", "// generated");

        // Act
        var files = await DiscoverAsync(workspace, new UntrackedCommandRunner());

        // Assert
        Assert.Equal(["App.csproj"], files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_reads_solution_project_paths_written_with_windows_separators()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create()
            .WithFile("onboard.sln", "Project(\"{GUID}\") = \"Api\", \"src\\Api\\Api.csproj\", \"{GUID}\"\n")
            .WithFile("src/Api/Api.csproj", "<Project />")
            .WithFile("src/Api/appsettings.json", "{}");

        // Act
        var files = await new FileSystemExplorerBackend(new UntrackedCommandRunner())
            .DiscoverAsync(workspace.PathTo("onboard.sln"));

        // Assert
        Assert.Equal(
            [workspace.PathTo("src/Api/Api.csproj")],
            files.Select(file => file.ProjectPath).Distinct());
    }

    [Fact]
    public async Task It_reports_a_file_deleted_on_disk_but_still_in_the_index_only_once()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.WithAppSolution();
        var runner = new RepositoryCommandRunner(workspace.Root, " D src/App/Gone.cs\0", "App.csproj\0Gone.cs\0");

        // Act
        var files = await DiscoverAsync(workspace, runner);

        // Assert
        Assert.Single(files, file => Path.GetFileName(file.Path) == "Gone.cs");
    }

    [PosixFact(Timeout = 10_000)]
    public async Task It_finds_no_projects_in_a_solution_that_is_a_named_pipe()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create();
        NamedPipe.TryCreate(workspace.PathTo(Solution));

        // Act
        var files = await Task.Run(() => DiscoverAsync(workspace, new UntrackedCommandRunner()));

        // Assert
        Assert.Empty(files);
    }

    [PosixFact(Timeout = 10_000)]
    public async Task It_finds_no_projects_in_a_classic_solution_that_is_a_named_pipe()
    {
        // Arrange
        using var workspace = TemporaryWorkspace.Create();
        NamedPipe.TryCreate(workspace.PathTo("onboard.sln"));

        // Act
        var files = await Task.Run(() => new FileSystemExplorerBackend(new UntrackedCommandRunner())
            .DiscoverAsync(workspace.PathTo("onboard.sln")));

        // Assert
        Assert.Empty(files);
    }

    [Fact]
    public async Task It_asks_git_for_one_listing_however_many_projects_there_are()
    {
        // Arrange
        using var workspace = TwoProjectSolution();
        var git = new ListingGit(workspace.Root, "src/App/App.csproj", "src/App/Order.cs", "src/Api/Api.csproj");

        // Act
        await DiscoverAsync(workspace, git);

        // Assert
        Assert.Equal(1, git.Listings);
    }

    [Fact]
    public async Task It_gives_each_project_the_files_beneath_it_from_that_listing()
    {
        // Arrange
        using var workspace = TwoProjectSolution();
        var git = new ListingGit(workspace.Root, "src/App/App.csproj", "src/App/Order.cs", "src/Api/Api.csproj");

        // Act
        var files = await DiscoverAsync(workspace, git);

        // Assert
        Assert.Equal(
            [("Api.csproj", "Api.csproj"), ("App.csproj", "App.csproj"), ("App.csproj", "Order.cs")],
            files
                .Select(file => (Path.GetFileName(file.ProjectPath), Path.GetFileName(file.Path)))
                .OrderBy(pair => pair));
    }

    private static TemporaryWorkspace TwoProjectSolution() => TemporaryWorkspace.Create()
        .WithFile(
            Solution,
            "<Solution><Project Path=\"src/App/App.csproj\" /><Project Path=\"src/Api/Api.csproj\" /></Solution>")
        .WithFile("src/App/App.csproj", "<Project />")
        .WithFile("src/App/Order.cs", "namespace App;")
        .WithFile("src/Api/Api.csproj", "<Project />");

    private static Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        TemporaryWorkspace workspace,
        ICommandRunner runner) =>
        new FileSystemExplorerBackend(runner).DiscoverAsync(workspace.PathTo(Solution));

    private sealed class RepositoryCommandRunner(string root, string status, string listing = "")
        : ICommandRunner
    {
        public Task<CommandResult> RunAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CommandResult(0, OutputFor(request), ""));

        private string OutputFor(CommandRequest request)
        {
            if (request.Arguments.Contains("--show-toplevel"))
            {
                return root;
            }

            return request.Arguments.Contains("ls-files") ? listing : status;
        }
    }

    /// <summary>Answers as git does: `ls-files` names only what lies under the
    /// folder it runs in, relative to it.</summary>
    private sealed class ListingGit(string root, params string[] trackedPaths) : ICommandRunner
    {
        public int Listings { get; private set; }

        public Task<CommandResult> RunAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CommandResult(0, OutputFor(request), ""));

        private string OutputFor(CommandRequest request)
        {
            if (request.Arguments.Contains("--show-toplevel"))
            {
                return root;
            }

            return request.Arguments.Contains("ls-files") ? Listing(request.WorkingDirectory) : "";
        }

        private string Listing(string workingDirectory)
        {
            Listings++;
            return string.Concat(trackedPaths
                .Select(path => Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)))
                .Where(path => path.StartsWith(workingDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(workingDirectory, path) + '\0'));
        }
    }

    private sealed class UntrackedCommandRunner : ICommandRunner
    {
        public Task<CommandResult> RunAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CommandResult(1, "", "not a git repository"));
    }
}
