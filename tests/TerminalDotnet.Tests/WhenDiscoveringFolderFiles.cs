using System.Runtime.Versioning;
using TerminalDotnet.Files;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenDiscoveringFolderFiles
{
    [Fact]
    public async Task It_finds_the_files_no_dotnet_project_claims()
    {
        // Arrange
        using var folder = LaunchFolder.At(
            "TerminalDotnet.slnx",
            "src/App/App.csproj",
            "scripts/build.sh",
            "docs/architecture.md",
            ".github/workflows/ci.yml");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.Equal(
            ["ci.yml", "architecture.md", "build.sh", "App.csproj"],
            files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_turns_off_the_file_system_monitor_a_repository_configures()
    {
        // Arrange
        using var folder = LaunchFolder.At("TerminalDotnet.slnx", "src/App/Order.cs");

        // Act
        await folder.DiscoverAsync();

        // Assert
        Assert.Equal(
            ["-c core.fsmonitor=false"],
            folder.Invocations.Select(arguments => string.Join(' ', arguments.Take(2))).Distinct());
    }

    [Fact]
    public async Task It_finds_the_dotnet_sources_alongside_them()
    {
        // Arrange
        using var folder = LaunchFolder.At(
            "TerminalDotnet.slnx",
            "src/App/Order.cs",
            "scripts/build.sh");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.Contains("Order.cs", files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_leaves_out_the_files_beside_the_folder_it_was_launched_in()
    {
        // Arrange
        using var folder = LaunchFolder.At(
            "samples/Demo/Demo.csproj",
            "samples/Demo/Program.cs",
            "src/App/Order.cs",
            "README.md");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.Equal(["Program.cs"], files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_roots_every_file_at_the_folder_it_was_launched_in()
    {
        // Arrange
        using var folder = LaunchFolder.At(
            "samples/Demo/Demo.csproj",
            "samples/Demo/Program.cs",
            "src/App/Order.cs");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.Equal(
            [Path.Combine(folder.Root, "samples", "Demo")],
            files.Select(file => file.ProjectPath).Distinct());
    }

    [Fact]
    public async Task It_marks_a_file_git_reports_as_modified()
    {
        // Arrange
        using var folder = LaunchFolder.At("TerminalDotnet.slnx", "scripts/build.sh")
            .Changed(" M scripts/build.sh\0");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.Equal(
            [FileGitStatus.Modified],
            files.Where(file => Path.GetFileName(file.Path) == "build.sh")
                .Select(file => file.GitStatus));
    }

    [Fact]
    public async Task It_reports_a_file_git_still_knows_about_but_the_disk_has_lost()
    {
        // Arrange
        using var folder = LaunchFolder.At("TerminalDotnet.slnx")
            .Changed(" D docs/gone.md\0");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.Equal(
            [("gone.md", FileGitStatus.Deleted)],
            files.Select(file => (Path.GetFileName(file.Path), file.GitStatus)));
    }

    [Fact]
    public async Task It_leaves_out_a_deletion_from_outside_the_folder_it_was_launched_in()
    {
        // Arrange
        using var folder = LaunchFolder.At("samples/Demo/Demo.csproj", "samples/Demo/Program.cs")
            .Changed(" D src/App/Gone.cs\0");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.DoesNotContain(FileGitStatus.Deleted, files.Select(file => file.GitStatus));
    }

    [Fact]
    public async Task It_falls_back_to_the_files_on_disk_outside_a_repository()
    {
        // Arrange
        using var folder = LaunchFolder.At("TerminalDotnet.slnx", "docs/architecture.md");

        // Act
        var files = await folder.WithoutGit().DiscoverAsync();

        // Assert
        Assert.Contains("architecture.md", files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_leaves_the_git_store_out_of_the_files_on_disk()
    {
        // Arrange
        using var folder = LaunchFolder.At("TerminalDotnet.slnx", ".git/config");

        // Act
        var files = await folder.WithoutGit().DiscoverAsync();

        // Assert
        Assert.DoesNotContain("config", files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_lists_a_file_once_when_a_folder_links_to_the_folder_holding_it()
    {
        // Arrange
        using var folder = LaunchFolder.At("TerminalDotnet.slnx", "real/Order.cs");
        Directory.CreateSymbolicLink(
            Path.Combine(folder.Root, "link"),
            Path.Combine(folder.Root, "real"));

        // Act
        var files = await folder.WithoutGit().DiscoverAsync();

        // Assert
        Assert.Single(files, file => Path.GetFileName(file.Path) == "Order.cs");
    }

    [PosixFact]
    [UnsupportedOSPlatform("windows")]
    public async Task It_lists_the_files_beside_a_folder_it_is_not_allowed_to_read()
    {
        // Arrange
        using var folder = LaunchFolder.At("TerminalDotnet.slnx", "docs/guide.md", "locked/secret.md");
        var locked = Path.Combine(folder.Root, "locked");
        File.SetUnixFileMode(locked, UnixFileMode.None);
        try
        {
            // Act
            var files = await folder.WithoutGit().DiscoverAsync();

            // Assert
            Assert.Contains("guide.md", files.Select(file => Path.GetFileName(file.Path)));
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    public async Task It_leaves_build_output_out_of_the_listing()
    {
        // Arrange
        using var folder = LaunchFolder.At(
            "TerminalDotnet.slnx",
            "src/App/bin/Debug/App.dll",
            "src/App/Order.cs");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.DoesNotContain("App.dll", files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_lists_a_folder_that_sits_beneath_a_folder_named_like_build_output()
    {
        // Arrange
        using var folder = LaunchFolder.At("bin/repo/TerminalDotnet.slnx", "bin/repo/Order.cs");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.Contains("Order.cs", files.Select(file => Path.GetFileName(file.Path)));
    }

    [Fact]
    public async Task It_reports_a_deletion_in_a_folder_beneath_a_folder_named_like_build_output()
    {
        // Arrange
        using var folder = LaunchFolder.At("obj/repo/TerminalDotnet.slnx")
            .Changed(" D obj/repo/Gone.cs\0");

        // Act
        var files = await folder.DiscoverAsync();

        // Assert
        Assert.Contains(FileGitStatus.Deleted, files.Select(file => file.GitStatus));
    }

    /// <summary>A repository on disk whose git answers are scripted, so the
    /// backend is exercised without launching git.</summary>
    private sealed class LaunchFolder : IDisposable
    {
        private string status = "";
        private bool tracked = true;

        private LaunchFolder(string root, string target, IReadOnlyList<string> trackedPaths)
        {
            Root = root;
            Target = target;
            TrackedPaths = trackedPaths;
        }

        public string Root { get; }

        private string Target { get; }

        private IReadOnlyList<string> TrackedPaths { get; }

        /// <summary>Lays the files out under a temporary repository and launches
        /// from the folder holding <paramref name="targetPath"/>.</summary>
        public static LaunchFolder At(string targetPath, params string[] trackedPaths)
        {
            var root = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
            // The paths are written the way a reader says them, with forward
            // slashes, and laid out with the separator the platform uses.
            var laidOut = trackedPaths.Select(OnThisPlatform).ToArray();
            foreach (var relativePath in (string[])[OnThisPlatform(targetPath), .. laidOut])
            {
                var path = Path.Combine(root, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "");
            }

            return new LaunchFolder(root, Path.Combine(root, OnThisPlatform(targetPath)), laidOut);
        }

        private static string OnThisPlatform(string relativePath) =>
            relativePath.Replace('/', Path.DirectorySeparatorChar);

        public LaunchFolder Changed(string porcelainStatus)
        {
            status = porcelainStatus;
            return this;
        }

        /// <summary>Every command git was asked to run, settings included.</summary>
        public List<IReadOnlyList<string>> Invocations { get; } = [];

        public LaunchFolder WithoutGit()
        {
            tracked = false;
            return this;
        }

        public Task<IReadOnlyList<FileEntry>> DiscoverAsync() =>
            new LaunchFolderBackend(new ScriptedGit(this)).DiscoverAsync(Target);

        public void Dispose() => Directory.Delete(Root, recursive: true);

        /// <summary>Answers as git does: `ls-files` reports only what lies under
        /// the folder it runs in, named relative to it, while `status` stays
        /// relative to the repository root.</summary>
        private sealed class ScriptedGit(LaunchFolder folder) : ICommandRunner
        {
            public Task<CommandResult> RunAsync(
                CommandRequest request,
                CancellationToken cancellationToken = default)
            {
                folder.Invocations.Add(request.Arguments);
                return Task.FromResult(folder.tracked
                    ? new CommandResult(0, OutputFor(request), "")
                    : new CommandResult(1, "", "not a git repository"));
            }

            private string OutputFor(CommandRequest request)
            {
                if (request.Arguments.Contains("--show-toplevel"))
                {
                    return folder.Root;
                }

                return request.Arguments.Contains("ls-files")
                    ? Listing(request.WorkingDirectory)
                    : folder.status;
            }

            private string Listing(string workingDirectory) => string.Concat(folder.TrackedPaths
                .Select(path => Path.Combine(folder.Root, path))
                .Where(path => path.StartsWith(workingDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Select(path => Path.GetRelativePath(workingDirectory, path) + '\0'));
        }
    }
}
