using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Explorer;

public sealed class WhenCreatingAFilePanelSnapshot
{
    [Fact]
    public void It_highlights_modified_files_blue_and_new_files_green()
    {
        // Arrange
        var modified = new FileEntry("App.csproj", "App", "Changed.cs", FileGitStatus.Modified);
        var added = new FileEntry("App.csproj", "App", "Added.cs", FileGitStatus.New);
        var state = new FileExplorerState(
        [
            new VisibleFileNode(0, FileNodeKind.Project, "App", [modified, added]),
            new VisibleFileNode(1, FileNodeKind.Namespace, "App", [modified, added]),
            new VisibleFileNode(2, FileNodeKind.File, "Changed.cs", [modified]),
            new VisibleFileNode(2, FileNodeKind.File, "Added.cs", [added])
        ]);

        // Act
        var snapshot = FilePanelSnapshot.From(state);

        // Assert
        Assert.Equal(
            [("    • Changed.cs", FileRowTone.Modified), ("    • Added.cs", FileRowTone.New)],
            snapshot.Rows.Skip(2).Select(row => (row.Text, row.Tone)));
    }
}
