using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Terminal;

public sealed class WhenHoldingTheNavigationKey
{
    [Fact]
    public void It_locks_navigation_while_g_is_held()
    {
        // Arrange
        var prefix = new NavigationPrefix();

        // Act
        prefix.Pressed(keyReleasesReported: true);

        // Assert
        Assert.True(prefix.IsWaiting);
    }

    [Fact]
    public void It_unlocks_navigation_as_soon_as_g_is_let_go()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Pressed(keyReleasesReported: true);

        // Act
        prefix.Released();

        // Assert
        Assert.False(prefix.IsWaiting);
    }

    [Fact]
    public void It_stays_locked_while_a_held_g_repeats()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Pressed(keyReleasesReported: true);

        // Act
        prefix.Pressed(keyReleasesReported: true);

        // Assert
        Assert.True(prefix.IsWaiting);
    }

    [Fact]
    public void It_unlocks_navigation_once_a_repeating_g_is_let_go()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Pressed(keyReleasesReported: true);
        prefix.Pressed(keyReleasesReported: true);

        // Act
        prefix.Released();

        // Assert
        Assert.False(prefix.IsWaiting);
    }

    [Fact]
    public void It_never_locks_navigation_where_no_release_is_ever_reported()
    {
        // Arrange
        var prefix = new NavigationPrefix();

        // Act
        prefix.Pressed(keyReleasesReported: false);

        // Assert
        Assert.False(prefix.IsWaiting);
    }

    [Fact]
    public void It_gives_up_the_lock_when_asked()
    {
        // Arrange
        var prefix = new NavigationPrefix();
        prefix.Pressed(keyReleasesReported: true);

        // Act
        prefix.Stop();

        // Assert
        Assert.False(prefix.IsWaiting);
    }
}
