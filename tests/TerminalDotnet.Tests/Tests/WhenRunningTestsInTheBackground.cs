using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenRunningTestsInTheBackground
{
    [Fact]
    public async Task It_starts_discovery_away_from_the_calling_thread()
    {
        // Arrange
        var caller = 0;
        var backend = new RecordingTests();

        // Act
        await Task.Run(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            new BackgroundTestBackend(backend).DiscoverAsync("Shop.sln").GetAwaiter().GetResult();
        });

        // Assert
        Assert.NotEqual(caller, backend.DiscoveryThread);
    }

    [Fact]
    public async Task It_starts_a_run_away_from_the_calling_thread()
    {
        // Arrange
        var caller = 0;
        var backend = new RecordingTests();
        var test = new TestCase("Shop.Tests.Adds", "Adds", "Shop.csproj");

        // Act
        await Task.Run(() =>
        {
            caller = Environment.CurrentManagedThreadId;
            new BackgroundTestBackend(backend).RunAsync([test]).GetAwaiter().GetResult();
        });

        // Assert
        Assert.NotEqual(caller, backend.RunThread);
    }

    [Fact]
    public async Task It_passes_running_output_through()
    {
        // Arrange
        var reported = new List<TestRunUpdate>();
        var test = new TestCase("Shop.Tests.Adds", "Adds", "Shop.csproj");

        // Act
        await new BackgroundTestBackend(new ReportingTests())
            .RunAsync([test], new Progress<TestRunUpdate>(reported.Add));

        // Assert
        Assert.Equal("Cart.Tests (batch 1 of 1)", reported.Single().Label);
    }

    private sealed class RecordingTests : ITestBackend
    {
        public int DiscoveryThread { get; private set; }
        public int RunThread { get; private set; }

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default)
        {
            DiscoveryThread = Environment.CurrentManagedThreadId;
            return Task.FromResult<IReadOnlyList<TestCase>>([]);
        }

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default)
        {
            RunThread = Environment.CurrentManagedThreadId;
            return Task.FromResult(new TestRun(true, "Passed", []));
        }
    }

    private sealed class ReportingTests : ITestBackend
    {
        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TestRun(true, "Passed"));

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            IProgress<TestRunUpdate>? progress,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(new TestRunUpdate("Cart.Tests (batch 1 of 1)", "running"));
            return Task.FromResult(new TestRun(true, "Passed"));
        }
    }
}
