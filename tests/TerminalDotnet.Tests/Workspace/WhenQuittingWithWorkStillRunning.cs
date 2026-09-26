using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

public sealed class WhenQuittingWithWorkStillRunning
{
    [Fact]
    public void It_waits_for_work_that_has_not_ended()
    {
        // Arrange
        var running = new TaskCompletionSource();
        var background = new BackgroundWork();
        background.Track(running.Task);

        // Act
        var ended = background.EndedAsync();

        // Assert
        Assert.False(ended.IsCompleted);
    }

    [Fact]
    public async Task It_finishes_once_the_work_it_tracked_has_ended()
    {
        // Arrange
        var running = new TaskCompletionSource();
        var background = new BackgroundWork();
        background.Track(running.Task);
        var ended = background.EndedAsync();

        // Act
        running.SetResult();
        await ended;

        // Assert
        Assert.True(ended.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task It_waits_out_work_that_was_cancelled_on_the_way_out()
    {
        // Arrange
        var cancelled = new TaskCompletionSource();
        var background = new BackgroundWork();
        background.Track(cancelled.Task);
        var ended = background.EndedAsync();

        // Act
        cancelled.SetCanceled();
        await ended;

        // Assert
        Assert.True(ended.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task It_waits_out_work_that_ended_in_a_failure()
    {
        // Arrange
        var failed = new TaskCompletionSource();
        var background = new BackgroundWork();
        background.Track(failed.Task);
        var ended = background.EndedAsync();

        // Act
        failed.SetException(new InvalidOperationException("dotnet test could not start"));
        await ended;

        // Assert
        Assert.True(ended.IsCompletedSuccessfully);
    }

    [Fact]
    public void It_has_nothing_to_wait_for_when_no_work_started()
    {
        // Arrange
        var background = new BackgroundWork();

        // Act
        var ended = background.EndedAsync();

        // Assert
        Assert.True(ended.IsCompleted);
    }
}
