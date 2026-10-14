namespace TerminalDotnet.Testing;

/// <summary>Runs discovery and execution away from the terminal loop,
/// including work before the backend reaches its first await.</summary>
public sealed class BackgroundTestBackend(ITestBackend backend) : ITestBackend
{
    public Task<IReadOnlyList<TestCase>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => backend.DiscoverAsync(target, cancellationToken), cancellationToken);

    public Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => backend.RunAsync(tests, cancellationToken), cancellationToken);

    public Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        IProgress<TestRunUpdate>? progress,
        CancellationToken cancellationToken = default) =>
        Task.Run(() => backend.RunAsync(tests, progress, cancellationToken), cancellationToken);
}
