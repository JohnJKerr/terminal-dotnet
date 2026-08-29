using Xunit;

namespace TerminalDotnet.DemoTests;

public sealed class FailureDemoTests
{
    [Fact]
    public void Opening_a_failure_in_the_configured_editor()
    {
        // Assert
        Assert.Fail("Intentional demo failure: press 'o' in TerminalDotnet to open this line.");
    }
}
