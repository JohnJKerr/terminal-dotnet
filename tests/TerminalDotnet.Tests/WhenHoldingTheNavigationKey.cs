using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenHoldingTheNavigationKey
{
    [Fact]
    public void It_waits_once_g_is_pressed()
    {
        // Arrange
        var prefix = new NavigationPrefix();

        // Act
        prefix.Arm();

        // Assert
        Assert.True(prefix.IsWaiting);
    }

    [Fact]
    public void It_stops_waiting_when_g_is_released_after_somewhere_was_named()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Arm();
        prefix.Reached();

        // Act
        prefix.Released();

        // Assert
        Assert.False(prefix.IsWaiting);
    }

    [Fact]
    public void A_tapped_g_waits_for_the_key_that_follows_it()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Arm();

        // Act
        prefix.Released();

        // Assert
        Assert.True(prefix.IsWaiting);
    }

    [Fact]
    public void A_tapped_g_stops_waiting_once_it_has_taken_the_reader_somewhere()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Arm();
        prefix.Released();

        // Act
        prefix.Reached();

        // Assert
        Assert.False(prefix.IsWaiting);
    }

    [Fact]
    public void A_held_g_keeps_waiting_after_it_has_taken_the_reader_somewhere()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Arm();

        // Act
        prefix.Reached();

        // Assert
        Assert.True(prefix.IsWaiting);
    }

    [Fact]
    public void It_keeps_waiting_while_one_place_after_another_is_named()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Arm();

        // Act
        prefix.Reached();

        // Assert
        Assert.True(prefix.IsWaiting);
    }

    [Fact]
    public void It_keeps_waiting_when_a_held_g_repeats_between_destinations()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Arm();
        prefix.Reached();

        // Act
        prefix.Arm();

        // Assert
        Assert.True(prefix.IsWaiting);
    }

    [Fact]
    public void It_still_stops_on_release_after_a_held_g_repeats()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Arm();
        prefix.Reached();
        prefix.Arm();

        // Act
        prefix.Released();

        // Assert
        Assert.False(prefix.IsWaiting);
    }

    [Fact]
    public void It_stops_waiting_once_given_up()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Arm();
        prefix.Reached();

        // Act
        prefix.Stop();

        // Assert
        Assert.False(prefix.IsWaiting);
    }

    [Fact]
    public void It_forgets_where_it_went_once_given_up()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Arm();
        prefix.Reached();
        prefix.Stop();
        prefix.Arm();

        // Act
        prefix.Released();

        // Assert
        Assert.True(prefix.IsWaiting);
    }
}
