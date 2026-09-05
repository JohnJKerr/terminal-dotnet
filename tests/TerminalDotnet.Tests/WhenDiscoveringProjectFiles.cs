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
            var files = await new FileSystemExplorerBackend(new RepositoryCommandRunner(root, "")).DiscoverAsync(
                Path.Combine(root, "TerminalDotnet.slnx"));

            // Assert
            Assert.Equal(
                [("App.csproj", "Order.cs")],
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

        try
        {
            // Act
            var files = await new FileSystemExplorerBackend(new RepositoryCommandRunner(root, gitStatus))
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
        var runner = new RepositoryCommandRunner(root, " M samples/App/Changed.cs\n");

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

    private sealed class RepositoryCommandRunner(string root, string status) : ICommandRunner
    {
        public Task<CommandResult> RunAsync(
            CommandRequest request,
            CancellationToken cancellationToken = default)
        {
            var output = request.Arguments.Contains("--show-toplevel") ? root : status;
            return Task.FromResult(new CommandResult(0, output, ""));
        }
    }
}
