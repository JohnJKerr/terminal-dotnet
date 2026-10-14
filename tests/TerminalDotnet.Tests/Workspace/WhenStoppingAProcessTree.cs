using System.ComponentModel;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

public sealed class WhenStoppingAProcessTree
{
    [Fact]
    public void It_lets_a_process_that_has_already_exited_go()
    {
        // Arrange
        var stopped = false;

        // Act
        ProcessTree.Stop(() =>
        {
            stopped = true;
            throw new Win32Exception();
        });

        // Assert
        Assert.True(stopped);
    }
}
