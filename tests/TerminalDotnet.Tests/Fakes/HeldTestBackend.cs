using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>Holds each run open until the test lets it finish, so a test can
/// act while a run is still going.</summary>
internal sealed class HeldTestBackend(TestCase test) : ITestBackend
{
    private readonly TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int RunCount { get; private set; }

    public void Finish() => finished.TrySetResult();

    public Task<IReadOnlyList<TestCase>> DiscoverAsync(string target, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TestCase>>([test]);

    public async Task<TestRun> RunAsync(IReadOnlyCollection<TestCase> tests, CancellationToken cancellationToken = default)
    {
        RunCount++;
        Started.TrySetResult();
        await finished.Task;
        return new TestRun(true, "Passed");
    }
}
