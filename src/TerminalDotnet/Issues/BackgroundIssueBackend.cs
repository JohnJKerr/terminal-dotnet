namespace TerminalDotnet.Issues;

/// <summary>Runs a build away from the terminal loop, including the work
/// before the backend reaches its first await.</summary>
public sealed class BackgroundIssueBackend(IIssueBackend backend) : IIssueBackend
{
    public Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => backend.DiscoverAsync(target, cancellationToken), cancellationToken);
}
