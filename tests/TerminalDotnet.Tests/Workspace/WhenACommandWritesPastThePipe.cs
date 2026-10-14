using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

public sealed class WhenACommandWritesPastThePipe
{
    private const string NeedsAPosixShell = "The command writes through a POSIX pipe.";

    [PosixFact(NeedsAPosixShell, Timeout = 10_000)]
    public async Task It_returns_when_both_streams_fill_past_the_pipe()
    {
        // Arrange
        var directory = Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var payload = new string('x', 256 * 1024);
        await File.WriteAllTextAsync(Path.Combine(directory, "payload"), payload);
        var runner = new ProcessCommandRunner();

        try
        {
            // Act
            var result = await runner.RunAsync(new CommandRequest(
                "sh",
                ["-c", "cat payload & cat payload >&2 & wait"],
                directory)).WaitAsync(TimeSpan.FromSeconds(5));

            // Assert
            Assert.Equal(payload.Length, result.StandardOutput.Length);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [PosixFact(NeedsAPosixShell, Timeout = 10_000)]
    public async Task It_reports_output_before_the_command_finishes()
    {
        // Arrange
        var seen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner = new ProcessCommandRunner();
        var run = runner.RunAsync(new CommandRequest(
            "sh",
            ["-c", "printf 'started\\n'; sleep 0.3; printf 'done\\n'"],
            Environment.CurrentDirectory)
        {
            OnOutput = chunk =>
            {
                if (chunk.Contains("started", StringComparison.Ordinal))
                {
                    seen.TrySetResult();
                }
            }
        });

        // Act
        await seen.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.False(run.IsCompleted);
        await run;
    }
}
