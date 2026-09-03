using Xunit;

namespace TerminalDotnet.DemoTests.Passing;

public sealed class PassingDemoTests
{
    [Fact]
    public void Displaying_the_first_passing_test()
    {
        // Assert
        Assert.True(true);
    }

    [Fact]
    public void Displaying_the_second_passing_test()
    {
        // Assert
        Assert.True(true);
    }

    [Fact]
    public void Displaying_the_third_passing_test()
    {
        // Assert
        Assert.True(true);
    }
}
