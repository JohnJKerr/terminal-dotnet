using TerminalDotnet.Testing;

namespace TerminalDotnet.Terminal;

public sealed class EditorLauncher(string editor, ICommandRunner commandRunner)
{
    public Task OpenAsync(string path, int line, CancellationToken cancellationToken = default) =>
        commandRunner.RunAsync(
            new CommandRequest(
                editor,
                [$"+{line}", path],
                Path.GetDirectoryName(Path.GetFullPath(path))!,
                CaptureOutput: false),
            cancellationToken);
}
