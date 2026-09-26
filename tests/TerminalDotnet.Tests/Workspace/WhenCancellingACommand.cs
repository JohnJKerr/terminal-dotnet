using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

public sealed class WhenCancellingACommand
{
    [Fact]
    public async Task It_stops_the_whole_process_tree_it_started()
    {
        // Arrange
        using var harness = new SleepingCommand();
        var runner = new ProcessCommandRunner();
        using var cancellation = new CancellationTokenSource();
        var run = runner.RunAsync(harness.Request, cancellation.Token);
        var processIds = await harness.StartedProcessIdsAsync();

        // Act
        await cancellation.CancelAsync();
        await Cancelled(run);

        // Assert
        Assert.Equal([false, false], await GoneAsync(processIds));
    }

    [Fact]
    public async Task It_reports_the_cancellation_to_the_caller()
    {
        // Arrange
        using var harness = new SleepingCommand();
        var runner = new ProcessCommandRunner();
        using var cancellation = new CancellationTokenSource();
        var run = runner.RunAsync(harness.Request, cancellation.Token);
        await harness.StartedProcessIdsAsync();

        // Act
        await cancellation.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
    }

    private static async Task Cancelled(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task<IReadOnlyList<bool>> GoneAsync(IReadOnlyList<int> processIds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && processIds.Any(IsRunning))
        {
            await Task.Delay(25);
        }

        return [.. processIds.Select(IsRunning)];
    }

    private static bool IsRunning(int processId) => Directory.Exists($"/proc/{processId}");

    private sealed class SleepingCommand : IDisposable
    {
        private readonly string directory = Path.Combine(
            Path.GetTempPath(),
            $"terminal-dotnet-{Guid.NewGuid():N}");

        public SleepingCommand()
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(
                Path.Combine(directory, "sleep.sh"),
                """
                sleep 60 &
                echo "$$ $!" > pids
                wait
                """);
        }

        public CommandRequest Request => new("sh", ["sleep.sh"], directory);

        public async Task<IReadOnlyList<int>> StartedProcessIdsAsync()
        {
            var path = Path.Combine(directory, "pids");
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(path) &&
                    (await File.ReadAllTextAsync(path)).Trim() is { Length: > 0 } recorded)
                {
                    return [.. recorded.Split(' ').Select(int.Parse)];
                }

                await Task.Delay(25);
            }

            throw new InvalidOperationException("The sleeping command never started.");
        }

        public void Dispose() => Directory.Delete(directory, recursive: true);
    }
}
