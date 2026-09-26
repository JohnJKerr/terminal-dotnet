using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Launch;

public sealed class WhenChoosingWhatToOpen
{
    [Fact]
    public void It_opens_the_only_project_in_the_folder()
    {
        // Act
        var choice = LaunchTarget.From(["/repo/App.csproj"]);

        // Assert
        Assert.Equal(new LaunchTarget.Found("/repo/App.csproj"), choice);
    }

    [Fact]
    public void It_opens_the_solution_rather_than_the_project_beside_it()
    {
        // Act
        var choice = LaunchTarget.From(["/repo/App.csproj", "/repo/App.sln"]);

        // Assert
        Assert.Equal(new LaunchTarget.Found("/repo/App.sln"), choice);
    }

    [Fact]
    public void It_opens_the_xml_solution_rather_than_the_classic_one_beside_it()
    {
        // Act
        var choice = LaunchTarget.From(["/repo/App.sln", "/repo/App.slnx"]);

        // Assert
        Assert.Equal(new LaunchTarget.Found("/repo/App.slnx"), choice);
    }

    [Fact]
    public void It_names_every_solution_when_it_cannot_tell_which_to_open()
    {
        // Act
        var choice = LaunchTarget.From(["/repo/Shop.sln", "/repo/Admin.sln", "/repo/App.csproj"]);

        // Assert
        Assert.Equal(
            ["/repo/Admin.sln", "/repo/Shop.sln"],
            (choice as LaunchTarget.Ambiguous)?.Candidates);
    }

    [Fact]
    public void It_finds_nothing_in_a_folder_with_no_solution_or_project()
    {
        // Act
        var choice = LaunchTarget.From([]);

        // Assert
        Assert.IsType<LaunchTarget.Missing>(choice);
    }
}
