using Xunit;

namespace TerminalDotnet.DemoTests;

public sealed class FailureDemoTests
{
    [Fact]
    public void Opening_a_failure_in_the_configured_editor()
    {
        // Assert
        Assert.Fail("Intentional demo failure: press 'p' in TerminalDotnet to preview this line.");
    }

    [Fact]
    public void Displaying_a_second_failure()
    {
        // Assert
        Assert.Fail("Intentional second demo failure.");
    }

    [Fact]
    public void Displaying_a_passing_test()
    {
        // Assert
        Assert.True(true);
    }
}
