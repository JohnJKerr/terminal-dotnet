using TerminalDotnet.Files;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Files;

public sealed class WhenBrowsingTheLaunchFolder
{
    [Fact]
    public async Task It_stands_a_top_level_folder_at_the_root()
    {
        // Arrange
        var session = SessionWithFiles(new FileEntry("folder", "folder/scripts/build.sh", FileGitStatus.Unchanged));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(
            [(0, FileNodeKind.Folder, "scripts"), (1, FileNodeKind.File, "build.sh")],
            session.State.VisibleNodes.Select(node => (node.Depth, node.Kind, node.Name)));
    }

    [Fact]
    public async Task It_stands_a_file_in_the_launch_folder_at_the_root()
    {
        // Arrange
        var session = SessionWithFiles(new FileEntry("folder", "folder/README.md", FileGitStatus.Unchanged));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(
            [(0, FileNodeKind.File, "README.md")],
            session.State.VisibleNodes.Select(node => (node.Depth, node.Kind, node.Name)));
    }

    [Fact]
    public async Task It_gives_the_launch_folder_no_node_of_its_own()
    {
        // Arrange
        var session = SessionWithFiles(new FileEntry("folder", "folder/src/App/Order.cs", FileGitStatus.Unchanged));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.DoesNotContain(FileNodeKind.Project, session.State.VisibleNodes.Select(node => node.Kind));
    }

    [Fact]
    public async Task It_nests_a_node_for_every_folder_below_the_root()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("folder", "folder/.github/workflows/ci.yml", FileGitStatus.Unchanged));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(
        [
            (0, FileNodeKind.Folder, ".github"),
            (1, FileNodeKind.Folder, "workflows"),
            (2, FileNodeKind.File, "ci.yml")
        ],
        session.State.VisibleNodes.Select(node => (node.Depth, node.Kind, node.Name)));
    }

    [Fact]
    public async Task It_keeps_a_file_no_dotnet_project_claims()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("folder", "folder/docs/architecture.md", FileGitStatus.Unchanged),
            new FileEntry("folder", "folder/src/App/Order.cs", FileGitStatus.Unchanged));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Contains("architecture.md", session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_lists_the_top_level_folders_before_the_files_beside_them()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("folder", "folder/README.md", FileGitStatus.Unchanged),
            new FileEntry("folder", "folder/scripts/build.sh", FileGitStatus.Unchanged));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(
            ["scripts", "build.sh", "README.md"],
            session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_hides_a_whole_subtree_when_its_top_level_folder_is_collapsed()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("folder", "folder/.github/workflows/ci.yml", FileGitStatus.Unchanged),
            new FileEntry("folder", "folder/README.md", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleExpanded());

        // Assert
        Assert.Equal([".github", "README.md"], session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_folds_every_top_level_folder_at_once()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("folder", "folder/docs/guide.md", FileGitStatus.Unchanged),
            new FileEntry("folder", "folder/scripts/build.sh", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new FileExplorerCommand.ToggleAllExpanded());

        // Assert
        Assert.Equal(["docs", "scripts"], session.State.VisibleNodes.Select(node => node.Name));
    }

    [Fact]
    public async Task It_counts_every_file_in_the_launch_folder()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("folder", "folder/README.md", FileGitStatus.Unchanged),
            new FileEntry("folder", "folder/scripts/build.sh", FileGitStatus.New),
            new FileEntry("folder", "folder/src/App/Order.cs", FileGitStatus.Modified));

        // Act
        await session.LoadAsync("TerminalDotnet.slnx");

        // Assert
        Assert.Equal(new FileChangeSummary(3, 1, 1, 0), session.State.Changes);
    }

    [Fact]
    public async Task It_searches_across_every_folder()
    {
        // Arrange
        var session = SessionWithFiles(
            new FileEntry("folder", "folder/docs/guide.md", FileGitStatus.Unchanged),
            new FileEntry("folder", "folder/scripts/build.sh", FileGitStatus.Unchanged));
        await session.LoadAsync("TerminalDotnet.slnx");

        // Act
        await session.DispatchAsync(new FileExplorerCommand.Search("build"));

        // Assert
        Assert.Equal(["scripts", "build.sh"], session.State.VisibleNodes.Select(node => node.Name));
    }

    private static FileExplorerSession SessionWithFiles(params FileEntry[] entries) =>
        new(new FixedFiles(entries), FileGrouping.Folder);
}
