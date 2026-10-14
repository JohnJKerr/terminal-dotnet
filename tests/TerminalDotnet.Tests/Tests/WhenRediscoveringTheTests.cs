using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

/// <summary>A rebuild can add, drop or rename tests, so the tree is discovered
/// again. The reader is part-way through it when they ask, so the tree they
/// had stays in front of them until the new one lands.</summary>
public sealed class WhenRediscoveringTheTests
{
    private static readonly TestCase Adds =
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "Shop.Tests.csproj");

    private static readonly TestCase Removes =
        new("Shop.Tests.CartTests.Removes_item", "Removes item", "Shop.Tests.csproj");

    private static readonly TestCase Empties =
        new("Shop.Tests.CartTests.Empties_cart", "Empties cart", "Shop.Tests.csproj");

    [Fact]
    public async Task It_keeps_the_tree_it_had_while_it_looks()
    {
        // Arrange
        var session = GivenA.TestExplorer()
            .WithBackend(new ChangingTestBackend(Adds, Removes))
            .Build();
        await session.LoadAsync("Shop.sln");

        // Act
        session.Rediscovering();

        // Assert
        Assert.NotEmpty(session.State.VisibleNodes);
    }

    [Fact]
    public async Task It_says_it_is_discovering_in_the_status_line()
    {
        // Arrange
        var session = GivenA.TestExplorer()
            .WithBackend(new ChangingTestBackend(Adds, Removes))
            .Build();
        await session.LoadAsync("Shop.sln");

        // Act
        session.Rediscovering();

        // Assert
        Assert.Equal("Discovering tests...", TestPanelSnapshot.From(session.State, "Shop.sln").StatusLine);
    }

    [Fact]
    public async Task It_does_not_lay_the_waiting_message_over_the_tree()
    {
        // Arrange
        var session = GivenA.TestExplorer()
            .WithBackend(new ChangingTestBackend(Adds, Removes))
            .Build();
        await session.LoadAsync("Shop.sln");

        // Act
        session.Rediscovering();

        // Assert
        Assert.Equal("", TestPanelSnapshot.From(session.State, "Shop.sln").EmptyMessage);
    }

    [Fact]
    public async Task It_lists_the_tests_the_rebuild_added()
    {
        // Arrange
        var backend = new ChangingTestBackend(Adds, Removes);
        var session = GivenA.TestExplorer()
            .WithBackend(backend)
            .Build();
        await session.LoadAsync("Shop.sln");
        backend.Tests = [Adds, Empties, Removes];
        session.Rediscovering();

        // Act
        await session.LoadAsync("Shop.sln");

        // Assert
        Assert.Contains(session.State.VisibleNodes, node => node.Name == "Empties cart");
    }

    [Fact]
    public async Task It_keeps_the_tree_when_rediscovery_fails()
    {
        // Arrange
        var backend = new ChangingTestBackend(Adds, Removes);
        var session = GivenA.TestExplorer()
            .WithBackend(backend)
            .Build();
        await session.LoadAsync("Shop.sln");
        session.Rediscovering();
        backend.FailWith("Test discovery failed: No test is available in Cart.Tests.dll.");

        // Act
        await session.LoadAsync("Shop.sln");

        // Assert
        Assert.Contains(session.State.VisibleNodes, node => node.Name == "Adds item");
    }

    [Fact]
    public async Task It_says_why_rediscovery_failed()
    {
        // Arrange
        var backend = new ChangingTestBackend(Adds, Removes);
        var session = GivenA.TestExplorer()
            .WithBackend(backend)
            .Build();
        await session.LoadAsync("Shop.sln");
        session.Rediscovering();
        backend.FailWith("Test discovery failed: No test is available in Cart.Tests.dll.");

        // Act
        await session.LoadAsync("Shop.sln");

        // Assert
        Assert.Equal(
            "Test discovery failed: No test is available in Cart.Tests.dll.",
            session.State.Message);
    }

    [Fact]
    public async Task It_keeps_the_tree_when_rediscovery_reports_nothing()
    {
        // Arrange
        var backend = new ChangingTestBackend(Adds, Removes);
        var session = GivenA.TestExplorer()
            .WithBackend(backend)
            .Build();
        await session.LoadAsync("Shop.sln");
        session.Rediscovering();
        backend.Tests = [];

        // Act
        await session.LoadAsync("Shop.sln");

        // Assert
        Assert.Contains(session.State.VisibleNodes, node => node.Name == "Adds item");
    }

    [Fact]
    public async Task It_does_not_start_a_run_while_the_tests_are_being_rediscovered()
    {
        // Arrange
        var backend = new HeldTestBackend(Adds);
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
        session.Rediscovering();

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected())
            .WaitAsync(TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(0, backend.RunCount);
    }

    [Fact]
    public async Task It_keeps_the_row_the_reader_was_on()
    {
        // Arrange
        var backend = new ChangingTestBackend(Adds, Removes);
        var session = GivenA.TestExplorer()
            .WithBackend(backend)
            .Build();
        await session.LoadAsync("Shop.sln");
        await session.DispatchAsync(new ExplorerCommand.SelectIndex(session.State.VisibleNodes.Count - 1));
        var wasOn = session.State.VisibleNodes[session.State.SelectedIndex].Name;
        backend.Tests = [Adds, Empties, Removes];

        // Act
        await session.LoadAsync("Shop.sln");

        // Assert
        Assert.Equal(wasOn, session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    private sealed class ChangingTestBackend(params TestCase[] tests) : ITestBackend
    {
        public IReadOnlyList<TestCase> Tests { get; set; } = tests;

        private string? failure;

        public void FailWith(string reason) => failure = reason;

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            failure is { } reason
                ? Task.FromException<IReadOnlyList<TestCase>>(new InvalidOperationException(reason))
                : Task.FromResult(Tests);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
