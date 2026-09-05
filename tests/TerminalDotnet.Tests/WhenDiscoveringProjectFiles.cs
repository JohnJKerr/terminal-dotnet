using TerminalDotnet.Files;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenDiscoveringProjectFiles
{
    [Fact]
    public async Task It_finds_the_files_belonging_to_each_project()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        await File.WriteAllTextAsync(
            Path.Combine(root, "src", "App", "Order.cs"),
            "namespace App.Domain; public sealed class Order;");

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(
                new RepositoryCommandRunner(root, "", "App.csproj\nOrder.cs\n")).DiscoverAsync(
                Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Equal(
                [("App.csproj", "App.csproj"), ("App.csproj", "Order.cs")],
                files.Select(file => (Path.GetFileName(file.ProjectPath), Path.GetFileName(file.Path))));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }


    [Fact]
    public async Task It_marks_modified_and_new_files_from_git_status()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "Changed.cs"), "namespace App;");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "Added.cs"), "namespace App;");
        var gitStatus = " M src/App/Changed.cs\n?? src/App/Added.cs\n";
        var listing = "Added.cs\nChanged.cs\n";

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(new RepositoryCommandRunner(root, gitStatus, listing))
                .DiscoverAsync(Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Equal(
                [("Added.cs", FileGitStatus.New), ("Changed.cs", FileGitStatus.Modified)],
                files.OrderBy(file => file.Path).Select(file => (Path.GetFileName(file.Path), file.GitStatus)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task It_reports_files_git_says_were_deleted()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        var gitStatus = " D src/App/Gone.cs\n";

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(new RepositoryCommandRunner(root, gitStatus))
                .DiscoverAsync(Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Equal(
                [("Gone.cs", FileGitStatus.Deleted)],
                files.Select(file => (Path.GetFileName(file.Path), file.GitStatus)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task It_ignores_deletions_outside_the_solution_projects()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        var gitStatus = " D docs/Notes.md\n D other/Gone.cs\n";

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(new RepositoryCommandRunner(root, gitStatus))
                .DiscoverAsync(Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Empty(files);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task It_resolves_git_paths_from_the_repository_root()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        var projectDirectory = Path.Combine(root, "samples", "App");
        Directory.CreateDirectory(projectDirectory);
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "App.csproj"), "<Project />");
        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "Changed.cs"), "namespace App;");
        var runner = new RepositoryCommandRunner(root, " M samples/App/Changed.cs\n", "Changed.cs\n");

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(runner)
                .DiscoverAsync(Path.Combine(projectDirectory, "App.csproj"));

            // Assert
            Assert.Equal(FileGitStatus.Modified, files[0].GitStatus);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task It_finds_files_that_are_not_csharp_sources()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "appsettings.json"), "{}");
        var runner = new RepositoryCommandRunner(root, "", "App.csproj\nappsettings.json\n");

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(runner)
                .DiscoverAsync(Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Contains("appsettings.json", files.Select(file => Path.GetFileName(file.Path)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task It_leaves_out_files_git_ignores()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "secrets.env"), "TOKEN=1");
        var runner = new RepositoryCommandRunner(root, "", "App.csproj\n");

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(runner)
                .DiscoverAsync(Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.DoesNotContain("secrets.env", files.Select(file => Path.GetFileName(file.Path)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task It_falls_back_to_the_files_on_disk_outside_a_repository()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "appsettings.json"), "{}");

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(new UntrackedCommandRunner())
                .DiscoverAsync(Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Equal(
                ["App.csproj", "appsettings.json"],
                files.Select(file => Path.GetFileName(file.Path)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task It_leaves_build_output_out_of_the_files_it_finds_on_disk()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App", "obj"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        await File.WriteAllTextAsync(
            Path.Combine(root, "src", "App", "obj", "App.AssemblyInfo.cs"),
            "// generated");

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(new UntrackedCommandRunner())
                .DiscoverAsync(Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Equal(["App.csproj"], files.Select(file => Path.GetFileName(file.Path)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task It_reads_solution_project_paths_written_with_windows_separators()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "Api"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "onboard.sln"),
            "Project(\"{GUID}\") = \"Api\", \"src\\Api\\Api.csproj\", \"{GUID}\"\n");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "Api", "Api.csproj"), "<Project />");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "Api", "appsettings.json"), "{}");

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(new UntrackedCommandRunner())
                .DiscoverAsync(Path.Combine(root, "onboard.sln"));

            // Assert
            Assert.Equal(
                [Path.Combine(root, "src", "Api", "Api.csproj")],
                files.Select(file => file.ProjectPath).Distinct());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task It_reports_a_file_deleted_on_disk_but_still_in_the_index_only_once()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "src", "App"));
        await File.WriteAllTextAsync(
            Path.Combine(root, "TerminalDotnet.slnx"),
            "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "App", "App.csproj"), "<Project />");
        var runner = new RepositoryCommandRunner(root, " D src/App/Gone.cs\n", "App.csproj\nGone.cs\n");

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(runner)
                .DiscoverAsync(Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Single(files.Where(file => Path.GetFileName(file.Path) == "Gone.cs"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

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

    private sealed class UntrackedCommandRunner : ICommandRunner
    {
        public Task<CommandResult> RunAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CommandResult(1, "", "not a git repository"));
    }
}
