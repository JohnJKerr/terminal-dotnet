using TerminalDotnet.Testing;

namespace TerminalDotnet.Git;

public static class GitRepository
{
    /// <returns>The top of the repository holding the folder, or null when
    /// the folder is outside one or git cannot answer.</returns>
    public static async Task<string?> RootAsync(
        ICommandRunner commandRunner,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync(
            GitRequest.For(["rev-parse", "--show-toplevel"], folder),
            cancellationToken).ConfigureAwait(false);
        return result.ExitCode == 0 && result.StandardOutput.Trim() is { Length: > 0 } root ? root : null;
    }
}
