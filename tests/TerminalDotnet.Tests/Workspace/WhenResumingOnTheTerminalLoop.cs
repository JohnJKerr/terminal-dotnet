using TerminalDotnet.Terminal;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

public sealed class WhenResumingOnTheTerminalLoop
{
    [Fact]
    public void Work_posted_to_the_loop_waits_for_the_loop_to_run_it()
    {
        // Arrange
        var queued = new List<Action>();
        var context = new TerminalLoopContext(queued.Add);
        var ran = false;

        // Act
        context.Post(_ => ran = true, null);

        // Assert
        Assert.False(ran);
    }

    [Fact]
    public void Work_posted_to_the_loop_runs_when_the_loop_takes_it()
    {
        // Arrange
        var queued = new List<Action>();
        var context = new TerminalLoopContext(queued.Add);
        var ran = false;
        context.Post(_ => ran = true, null);

        // Act
        queued.ForEach(work => work());

        // Assert
        Assert.True(ran);
    }

    [Fact]
    public async Task Work_posted_once_the_loop_has_ended_runs_without_it()
    {
        // Arrange
        var context = new TerminalLoopContext(_ => { });
        var ran = new TaskCompletionSource();
        context.Release();

        // Act
        context.Post(_ => ran.SetResult(), null);

        // Assert
        await ran.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(ran.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Work_the_loop_never_took_runs_once_the_loop_has_ended()
    {
        // Arrange
        var context = new TerminalLoopContext(_ => { });
        var ran = new TaskCompletionSource();
        context.Post(_ => ran.SetResult(), null);

        // Act
        context.Release();

        // Assert
        await ran.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(ran.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public void Work_the_loop_already_ran_is_not_run_again_once_it_has_ended()
    {
        // Arrange
        var queued = new List<Action>();
        var context = new TerminalLoopContext(queued.Add);
        var runs = 0;
        context.Post(_ => Interlocked.Increment(ref runs), null);
        queued.ForEach(work => work());

        // Act
        context.Release();
        Thread.Sleep(50);

        // Assert
        Assert.Equal(1, runs);
    }
}
