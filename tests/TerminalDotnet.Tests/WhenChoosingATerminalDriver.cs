using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenChoosingATerminalDriver
{
    [Fact]
    public void It_uses_the_driver_the_reader_asked_for()
    {
        // Act
        var driver = TerminalDriverChoice.For(requested: "ansi", onWindows: false);

        // Assert
        Assert.Equal("ansi", driver);
    }

    [Fact]
    public void It_draws_through_the_dotnet_console_away_from_windows()
    {
        // Act
        var driver = TerminalDriverChoice.For(requested: null, onWindows: false);

        // Assert
        Assert.Equal("dotnet", driver);
    }

    [Fact]
    public void It_treats_an_empty_request_as_no_request()
    {
        // Act
        var driver = TerminalDriverChoice.For(requested: "", onWindows: false);

        // Assert
        Assert.Equal("dotnet", driver);
    }

    [Fact]
    public void It_leaves_windows_to_the_toolkit_default()
    {
        // Act
        var driver = TerminalDriverChoice.For(requested: null, onWindows: true);

        // Assert
        Assert.Null(driver);
    }
}
