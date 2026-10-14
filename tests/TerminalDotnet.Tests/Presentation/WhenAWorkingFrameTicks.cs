using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Presentation;

public sealed class WhenAWorkingFrameTicks
{
    [Fact]
    public void It_brings_the_label_up_to_date()
    {
        // Arrange
        var clock = new RecordingClock();
        var refreshed = false;
        WorkingIndicator.Turn(clock, () => true, () => refreshed = true);

        // Act
        clock.Tick();

        // Assert
        Assert.True(refreshed);
    }

    [Fact]
    public void It_does_not_redraw_the_whole_window()
    {
        // Arrange
        var clock = new RecordingClock();
        WorkingIndicator.Turn(clock, () => true, () => { });

        // Act
        clock.Tick();

        // Assert
        Assert.Equal(["AddTimeout"], clock.Calls);
    }

    [Fact]
    public void It_stops_once_the_work_has_finished()
    {
        // Arrange
        var clock = new RecordingClock();
        WorkingIndicator.Turn(clock, () => false, () => { });

        // Act
        var again = clock.Tick();

        // Assert
        Assert.False(again);
    }

    private sealed class RecordingClock : IFrameClock
    {
        public List<string> Calls { get; } = [];

        private Func<bool>? tick;

        public void AddTimeout(TimeSpan interval, Func<bool> callback)
        {
            Calls.Add(nameof(AddTimeout));
            tick = callback;
        }

        public bool Tick() => tick!.Invoke();
    }
}
