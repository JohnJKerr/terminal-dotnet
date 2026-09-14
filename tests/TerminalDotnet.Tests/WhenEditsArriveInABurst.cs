using TerminalDotnet.Files;
using Xunit;

namespace TerminalDotnet.Tests.Files;

public sealed class WhenEditsArriveInABurst
{
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(400);

    [Fact]
    public void A_tree_nobody_has_touched_has_nothing_to_reload()
    {
        // Arrange
        var burst = new EditBurst(Quiet);

        // Act
        var settled = burst.SettledAt(TimeSpan.FromSeconds(9));

        // Assert
        Assert.False(settled);
    }

    [Fact]
    public void An_edit_is_held_while_the_tree_is_still_being_written_to()
    {
        // Arrange
        var burst = new EditBurst(Quiet);
        burst.Noticed(TimeSpan.FromMilliseconds(100));

        // Act
        var settled = burst.SettledAt(TimeSpan.FromMilliseconds(300));

        // Assert
        Assert.False(settled);
    }

    [Fact]
    public void It_settles_once_the_tree_has_been_quiet()
    {
        // Arrange
        var burst = new EditBurst(Quiet);
        burst.Noticed(TimeSpan.FromMilliseconds(100));

        // Act
        var settled = burst.SettledAt(TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.True(settled);
    }

    [Fact]
    public void A_later_edit_holds_the_burst_open()
    {
        // Arrange
        var burst = new EditBurst(Quiet);
        burst.Noticed(TimeSpan.FromMilliseconds(100));
        burst.Noticed(TimeSpan.FromMilliseconds(450));

        // Act
        var settled = burst.SettledAt(TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.False(settled);
    }

    [Fact]
    public void A_burst_that_has_settled_is_not_reloaded_for_twice()
    {
        // Arrange
        var burst = new EditBurst(Quiet);
        burst.Noticed(TimeSpan.FromMilliseconds(100));
        burst.SettledAt(TimeSpan.FromMilliseconds(500));

        // Act
        var settledAgain = burst.SettledAt(TimeSpan.FromMilliseconds(900));

        // Assert
        Assert.False(settledAgain);
    }

    [Fact]
    public void An_edit_after_a_reload_starts_a_burst_of_its_own()
    {
        // Arrange
        var burst = new EditBurst(Quiet);
        burst.Noticed(TimeSpan.FromMilliseconds(100));
        burst.SettledAt(TimeSpan.FromMilliseconds(500));
        burst.Noticed(TimeSpan.FromMilliseconds(600));

        // Act
        var settled = burst.SettledAt(TimeSpan.FromMilliseconds(1100));

        // Assert
        Assert.True(settled);
    }

    [Fact]
    public void An_edit_noticed_while_a_reload_is_running_settles_after_it()
    {
        // Arrange
        var burst = new EditBurst(Quiet);
        burst.Noticed(TimeSpan.FromMilliseconds(100));
        burst.SettledAt(TimeSpan.FromMilliseconds(500));
        burst.Noticed(TimeSpan.FromMilliseconds(520));

        // Act
        var settled = burst.SettledAt(TimeSpan.FromMilliseconds(1000));

        // Assert
        Assert.True(settled);
    }

    [Fact]
    public void An_edit_worth_reloading_for_starts_a_burst()
    {
        // Arrange
        var burst = new EditBurst(Quiet);
        burst.Noticed("src/Order.cs", TimeSpan.FromMilliseconds(100));

        // Act
        var settled = burst.SettledAt(TimeSpan.FromMilliseconds(600));

        // Assert
        Assert.True(settled);
    }

    [Fact]
    public void Build_output_starts_no_burst_at_all()
    {
        // Arrange
        var burst = new EditBurst(Quiet);
        burst.Noticed("src/obj/Order.g.cs", TimeSpan.FromMilliseconds(100));

        // Act
        var settled = burst.SettledAt(TimeSpan.FromMilliseconds(600));

        // Assert
        Assert.False(settled);
    }

    [Fact]
    public void A_burst_the_panels_have_already_answered_is_forgotten()
    {
        // Arrange
        var burst = new EditBurst(Quiet);
        burst.Noticed(TimeSpan.FromMilliseconds(100));
        burst.Forget();

        // Act
        var settled = burst.SettledAt(TimeSpan.FromMilliseconds(600));

        // Assert
        Assert.False(settled);
    }
}
