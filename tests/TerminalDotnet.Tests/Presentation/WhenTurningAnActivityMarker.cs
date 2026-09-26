using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenTurningAnActivityMarker
{
    [Fact]
    public void It_turns_as_the_wait_goes_on()
    {
        // Act
        var marker = ActivityMarker.MarkerAt(ActivityMarker.FrameDuration);

        // Assert
        Assert.NotEqual(ActivityMarker.MarkerAt(TimeSpan.Zero), marker);
    }

    [Fact]
    public void It_holds_each_frame_for_its_whole_turn()
    {
        // Act
        var marker = ActivityMarker.MarkerAt(ActivityMarker.FrameDuration - TimeSpan.FromTicks(1));

        // Assert
        Assert.Equal(ActivityMarker.MarkerAt(TimeSpan.Zero), marker);
    }

    [Fact]
    public void It_returns_to_the_first_frame_after_a_full_turn()
    {
        // Act
        var marker = ActivityMarker.MarkerAt(ActivityMarker.FrameDuration * ActivityMarker.FrameCount);

        // Assert
        Assert.Equal(ActivityMarker.MarkerAt(TimeSpan.Zero), marker);
    }

    [Fact]
    public void It_marks_the_message_it_is_given()
    {
        // Act
        var marked = ActivityMarker.Marking("Discovering tests...", TimeSpan.Zero);

        // Assert
        Assert.Equal($"{ActivityMarker.MarkerAt(TimeSpan.Zero)} Discovering tests...", marked);
    }
}
