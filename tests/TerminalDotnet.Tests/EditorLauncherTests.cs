using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests;

public sealed class EditorLauncherTests
{
    [Fact]
    public async Task Opening_source_issues_the_configured_editor_command_at_the_failure_line()
    {
        // Arrange
        var runner = new InMemoryCommandRunner();
        var launcher = new EditorLauncher("code", runner);

        // Act
        await launcher.OpenAsync("/repo/CartTests.cs", 42);

        // Assert
        Assert.Equal(
            "code|+42|/repo/CartTests.cs|/repo|capture:False",
            runner.LastRequest is null
                ? null
                : string.Join('|',
                    [
                        runner.LastRequest.FileName,
                        .. runner.LastRequest.Arguments,
                        runner.LastRequest.WorkingDirectory,
                        $"capture:{runner.LastRequest.CaptureOutput}"
                    ]));
    }

    private sealed class InMemoryCommandRunner : ICommandRunner
    {
        public CommandRequest? LastRequest { get; private set; }

        public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new CommandResult(0, "", ""));
        }
    }
}
