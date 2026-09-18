using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenTheConsoleIsTooNarrowForADialog
{
    [Fact]
    public void It_draws_a_dialog_in_a_console_with_room_for_one()
    {
        // Act
        var fits = DialogSpace.Fits(consoleWidth: 80);

        // Assert
        Assert.True(fits);
    }

    [Fact]
    public void It_draws_a_dialog_at_the_narrowest_width_the_toolkit_can_size()
    {
        // Act
        var fits = DialogSpace.Fits(consoleWidth: 3);

        // Assert
        Assert.True(fits);
    }

    [Fact]
    public void It_refuses_a_console_the_toolkit_would_size_a_dialog_negatively_in()
    {
        // Act
        var fits = DialogSpace.Fits(consoleWidth: 2);

        // Assert
        Assert.False(fits);
    }

    [Fact]
    public void It_refuses_a_console_that_reports_no_width_at_all()
    {
        // Act
        var fits = DialogSpace.Fits(consoleWidth: 0);

        // Assert
        Assert.False(fits);
    }
}
