using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenDiscoveryFails
{
    [Fact]
    public async Task It_leaves_the_test_panel_in_a_failed_state()
    {
        // Arrange
        var session = GivenA.TestExplorer()
            .WithBackend(new FailingTestBackend("Test discovery failed: no SDK"))
            .Build();

        // Act
        await session.LoadAsync("Shop.sln");

        // Assert
        Assert.Equal(ExplorerStatus.Failed, session.State.Status);
    }

    [Fact]
    public async Task It_reports_why_discovery_failed()
    {
        // Arrange
        var session = GivenA.TestExplorer()
            .WithBackend(new FailingTestBackend("Test discovery failed: no SDK"))
            .Build();

        // Act
        await session.LoadAsync("Shop.sln");

        // Assert
        Assert.Equal("Test discovery failed: no SDK", session.State.Message);
    }

    [Fact]
    public async Task It_leaves_the_tree_empty()
    {
        // Arrange
        var session = GivenA.TestExplorer()
            .WithBackend(new FailingTestBackend("Test discovery failed: no SDK"))
            .Build();

        // Act
        await session.LoadAsync("Shop.sln");

        // Assert
        Assert.Empty(session.State.VisibleNodes);
    }

    private sealed class FailingTestBackend(string reason) : ITestBackend
    {
        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<TestCase>>(new InvalidOperationException(reason));

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
