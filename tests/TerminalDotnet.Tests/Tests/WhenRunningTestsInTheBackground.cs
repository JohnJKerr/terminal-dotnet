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
}
