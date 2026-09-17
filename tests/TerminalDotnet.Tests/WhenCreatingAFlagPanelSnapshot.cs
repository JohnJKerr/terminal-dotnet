using TerminalDotnet.Flags;
using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Flags;

public sealed class WhenCreatingAFlagPanelSnapshot
{
    [Fact]
    public void It_counts_one_flag_in_the_singular()
    {
        // Arrange
        var state = new FlagState([Todo("Order.cs")]);

        // Act
        var snapshot = FlagPanelSnapshot.From(state);

        // Assert
        Assert.Equal(["1 Flag"], snapshot.StatusSegments.Select(segment => segment.Text));
    }

    [Fact]
    public void It_counts_several_flags_in_the_plural()
    {
        // Arrange
        var state = new FlagState([Todo("Order.cs"), Todo("Cart.cs")]);

        // Act
        var snapshot = FlagPanelSnapshot.From(state);

        // Assert
        Assert.Equal(["2 Flags"], snapshot.StatusSegments.Select(segment => segment.Text));
    }

    private static Flag Todo(string file) =>
        new($"/repo/src/{file}", $"src/{file}", 3, FlagKind.Todo, "do this");
}
