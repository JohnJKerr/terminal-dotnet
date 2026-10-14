using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

public sealed class WhenStartingACommand
{
    [Fact]
    public async Task It_passes_the_request_environment_to_the_process()
    {
        // Arrange
        var runner = new ProcessCommandRunner();
        var request = new CommandRequest(
            OperatingSystem.IsWindows() ? "cmd.exe" : "sh",
            OperatingSystem.IsWindows()
                ? ["/d", "/c", "echo %DOTNET_CLI_UI_LANGUAGE%"]
                : ["-c", "printf '%s' \"$DOTNET_CLI_UI_LANGUAGE\""],
            Environment.CurrentDirectory)
        {
            Environment = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [DotnetProcess.LanguageVariable] = "en"
            }
        };

        // Act
        var result = await runner.RunAsync(request);

        // Assert
        Assert.Equal("en", result.StandardOutput.Trim());
    }
}
