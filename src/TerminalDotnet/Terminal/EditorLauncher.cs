using TerminalDotnet.Testing;

namespace TerminalDotnet.Terminal;

public sealed class EditorLauncher
{
    private readonly string executable;
    private readonly IReadOnlyList<string> configuredArguments;
    private readonly ICommandRunner commandRunner;

    public EditorLauncher(string editor, ICommandRunner commandRunner)
    {
        var command = editor.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        executable = command[0];
        configuredArguments = command[1..];
        this.commandRunner = commandRunner;
    }

    public Task OpenAsync(string path, int line, CancellationToken cancellationToken = default) =>
        commandRunner.RunAsync(
            new CommandRequest(
                executable,
                [.. configuredArguments, $"+{line}", path],
                Path.GetDirectoryName(Path.GetFullPath(path))!,
                CaptureOutput: false),
            cancellationToken);
}
