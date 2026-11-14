using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Workspace;

[CollectionDefinition("Executable lookup", DisableParallelization = true)]
public sealed class ExecutableLookupCollection;

[Collection("Executable lookup")]
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
    [PosixFact("Executable search is exercised with a POSIX shell script.")]
    public async Task It_does_not_start_an_executable_from_the_launch_folder()
    {
        // Arrange
        var folder = Directory.CreateTempSubdirectory("terminal-dotnet-executable-");
        var originalDirectory = Environment.CurrentDirectory;
        try
        {
            var executable = Path.Combine(folder.FullName, "sh");
            await File.WriteAllTextAsync(executable, "#!/bin/sh\nprintf planted");
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            Environment.CurrentDirectory = folder.FullName;
            var runner = new ProcessCommandRunner();
            var request = new CommandRequest("sh", ["-c", "printf trusted"], folder.FullName);

            // Act
            var result = await runner.RunAsync(request);

            // Assert
            Assert.Equal("trusted", result.StandardOutput);
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            folder.Delete(recursive: true);
        }
    }
}
