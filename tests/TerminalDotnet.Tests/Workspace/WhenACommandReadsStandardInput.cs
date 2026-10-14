using System.Runtime.InteropServices;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

[CollectionDefinition(nameof(WhenACommandReadsStandardInput), DisableParallelization = true)]
public sealed class WhenACommandReadsStandardInputHoldsTheProcess;

[Collection(nameof(WhenACommandReadsStandardInput))]
public sealed class WhenACommandReadsStandardInput
{
    private const string NeedsAPosixShell =
        "The command reads standard input held open by a POSIX pipe.";

    [PosixFact(NeedsAPosixShell, Timeout = 10_000)]
    public async Task The_next_command_starts_once_it_has_nothing_left_to_read()
    {
        // Arrange
        using var heldOpen = new HeldStandardInput();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var runner = new SerialCommandRunner(new ProcessCommandRunner());
        var reading = runner.RunAsync(Shell("read line; printf '%s' first"), cancellation.Token);
        var following = runner.RunAsync(Shell("printf '%s' second"), cancellation.Token);

        // Act
        var results = await Task.WhenAll(reading, following);

        // Assert
        Assert.Equal("second", results[1].StandardOutput);
    }

    private static CommandRequest Shell(string script) =>
        new("sh", ["-c", script], Environment.CurrentDirectory);

    private sealed class HeldStandardInput : IDisposable
    {
        private readonly int saved;
        private readonly int readEnd;
        private readonly int writeEnd;

        public HeldStandardInput()
        {
            if (pipe(out var ends) != 0)
            {
                throw new InvalidOperationException("Could not hold standard input open.");
            }

            readEnd = ends.Read;
            writeEnd = ends.Write;
            saved = dup(0);
            if (saved < 0 || dup2(readEnd, 0) < 0)
            {
                throw new InvalidOperationException("Could not hold standard input open.");
            }
        }

        public void Dispose()
        {
            _ = dup2(saved, 0);
            _ = close(saved);
            _ = close(readEnd);
            _ = close(writeEnd);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PipeEnds
        {
            public int Read;
            public int Write;
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int pipe(out PipeEnds ends);

        [DllImport("libc", SetLastError = true)]
        private static extern int dup(int fd);

        [DllImport("libc", SetLastError = true)]
        private static extern int dup2(int oldFd, int newFd);

        [DllImport("libc", SetLastError = true)]
        private static extern int close(int fd);
    }
}
