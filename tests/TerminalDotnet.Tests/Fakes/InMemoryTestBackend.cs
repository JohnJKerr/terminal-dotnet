using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Fakes;

/// <summary>Discovers the tests it was given, and passes every test it is
/// asked to run unless it was handed the run to report instead.</summary>
internal sealed class InMemoryTestBackend(IReadOnlyList<TestCase> tests, TestRun? run = null) : ITestBackend
{
    public IReadOnlyCollection<TestCase> LastRun { get; private set; } = [];

    public List<IReadOnlyCollection<TestCase>> RunHistory { get; } = [];

    public Task<IReadOnlyList<TestCase>> DiscoverAsync(string target, CancellationToken cancellationToken = default) =>
        Task.FromResult(tests);

    public Task<TestRun> RunAsync(IReadOnlyCollection<TestCase> requested, CancellationToken cancellationToken = default)
    {
        LastRun = requested;
        RunHistory.Add(requested);
        return Task.FromResult(run ?? PassingRun(requested));
    }

    private static TestRun PassingRun(IReadOnlyCollection<TestCase> requested) => new(
        true,
        "Passed",
        [.. requested.Select(test => new TestResult(test, TestOutcome.Passed, TimeSpan.Zero, null, null, null, null))]);
}
