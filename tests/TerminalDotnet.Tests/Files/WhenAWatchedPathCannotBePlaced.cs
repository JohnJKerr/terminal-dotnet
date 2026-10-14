using TerminalDotnet.Files;
using Xunit;

namespace TerminalDotnet.Tests.Files;

/// <summary>The watcher hears every path the operating system reports, including
/// ones that do not belong under the folder it was asked to watch.</summary>
public sealed class WhenAWatchedPathCannotBePlaced
{
    [Fact]
    public void It_reports_a_file_relative_to_the_watched_folder()
    {
        // Arrange
        var folder = Path.Combine(Path.GetTempPath(), "repo");
        var file = Path.Combine(folder, "src", "Order.cs");

        // Act
        var relative = WorkspacePath.RelativeTo(folder, file);

        // Assert
        Assert.Equal(Path.Combine("src", "Order.cs"), relative);
    }

    [Fact]
    public void It_leaves_a_path_it_cannot_place_unreported()
    {
        // Act
        var relative = WorkspacePath.RelativeTo(Path.GetTempPath(), "");

        // Assert
        Assert.Null(relative);
    }
}
