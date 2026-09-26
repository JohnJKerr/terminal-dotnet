using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenRunningTests
{
    [Fact]
    public async Task It_runs_every_test_beneath_the_selected_class()
    {
        // Arrange
        var backend = new InMemoryTestBackend(
        [
            GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
            GivenA.TestCase("Shop.Tests.CartTests.Removes_item"),
            GivenA.TestCase("Shop.Tests.OrderTests.Submits_order")
        ]);
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(
        [
            "test:Shop.Tests.CartTests.Adds_item",
            "test:Shop.Tests.CartTests.Removes_item",
            "status:Ready",
            "message:Passed"
        ],
            [.. backend.LastRun.Select(test => $"test:{test.FullyQualifiedName}"),
                $"status:{session.State.Status}",
                $"message:{session.State.Message}"]);
    }

    [Fact]
    public async Task It_retains_completed_outcomes_on_the_selected_subtree()
    {
        // Arrange
        var backend = new InMemoryTestBackend(
        [
            GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
            GivenA.TestCase("Shop.Tests.CartTests.Removes_item"),
            GivenA.TestCase("Shop.Tests.OrderTests.Submits_order")
        ]);
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(
        [
            TestNodeOutcome.NotRun,
            TestNodeOutcome.Passed,
            TestNodeOutcome.Passed,
            TestNodeOutcome.Passed,
            TestNodeOutcome.NotRun,
            TestNodeOutcome.NotRun
        ], session.State.VisibleNodes.Select(node => node.Outcome));
    }

    [Fact]
    public async Task It_retains_a_skipped_test_as_skipped_in_the_tree()
    {
        // Arrange
        var test = GivenA.TestCase("Shop.Tests.CartTests.Skips_item");
        var skipped = GivenA.ResultFor(test).Skipped().Taking(TimeSpan.FromMilliseconds(2)).Build();
        var session = await GivenA.TestExplorer()
            .WithBackend(new InMemoryTestBackend([test], new TestRun(true, "Skipped: 1", [skipped])))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(TestNodeOutcome.Skipped, session.State.VisibleNodes[2].Outcome);
    }

    [Fact]
    public async Task It_exposes_the_selected_tests_failure_details_after_a_failed_run()
    {
        // Arrange
        var test = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var failure = GivenA.ResultFor(test)
            .Failed("Expected total to be 10.")
            .Taking(TimeSpan.FromMilliseconds(12))
            .WithStackTrace("at CartTests.Adds_item() in /repo/CartTests.cs:line 42")
            .At("/repo/CartTests.cs", 42)
            .Build();
        var backend = new InMemoryTestBackend([test], new TestRun(false, "1 test failed", [failure]));
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(failure, session.State.LastRun!.Results.Single());
    }

    [Fact]
    public async Task It_marks_the_selected_test_as_failed_after_a_failed_run()
    {
        // Arrange
        var test = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var failure = GivenA.ResultFor(test)
            .Failed("Expected total to be 10.")
            .Taking(TimeSpan.FromMilliseconds(12))
            .WithStackTrace("at CartTests.Adds_item() in /repo/CartTests.cs:line 42")
            .At("/repo/CartTests.cs", 42)
            .Build();
        var session = await GivenA.TestExplorer()
            .WithBackend(new InMemoryTestBackend([test], new TestRun(false, "1 test failed", [failure])))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(TestNodeOutcome.Failed, session.State.VisibleNodes[2].Outcome);
    }

    [Fact]
    public async Task It_locates_the_failure_line_after_a_failed_run()
    {
        // Arrange
        var test = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var failure = GivenA.ResultFor(test)
            .Failed("Expected total to be 10.")
            .At("/repo/CartTests.cs", 42)
            .Build();
        var session = await GivenA.TestExplorer()
            .WithBackend(new InMemoryTestBackend([test], new TestRun(false, "1 test failed", [failure])))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Assert
        Assert.Equal(new SourceLocation("/repo/CartTests.cs", 42), session.State.SourceLocation);
    }

    [Fact]
    public async Task It_selects_the_first_failed_test_when_moving_to_the_next_failure()
    {
        // Arrange
        var first = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var second = GivenA.TestCase("Shop.Tests.CartTests.Removes_item");
        var run = new TestRun(false, "2 tests failed",
        [
            GivenA.ResultFor(first).Failed().Build(),
            GivenA.ResultFor(second).Failed().Build()
        ]);
        var session = await GivenA.TestExplorer()
            .WithBackend(new InMemoryTestBackend([first, second], run))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        await session.DispatchAsync(new ExplorerCommand.NextFailure());

        // Assert
        Assert.Equal("Adds item", session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task It_wraps_to_the_first_failure_when_moving_past_the_last_failure()
    {
        // Arrange
        var first = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var second = GivenA.TestCase("Shop.Tests.CartTests.Removes_item");
        var run = new TestRun(false, "2 tests failed",
        [
            GivenA.ResultFor(first).Failed().Build(),
            GivenA.ResultFor(second).Failed().Build()
        ]);
        var session = await GivenA.TestExplorer()
            .WithBackend(new InMemoryTestBackend([first, second], run))
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.RunSelected());
        await session.DispatchAsync(new ExplorerCommand.NextFailure());
        await session.DispatchAsync(new ExplorerCommand.NextFailure());

        // Act
        await session.DispatchAsync(new ExplorerCommand.NextFailure());

        // Assert
        Assert.Equal("Adds item", session.State.VisibleNodes[session.State.SelectedIndex].Name);
    }

    [Fact]
    public async Task It_uses_the_previous_tests_when_rerunning_after_selection_moves()
    {
        // Arrange
        var backend = new InMemoryTestBackend(
        [
            GivenA.TestCase("Shop.Tests.CartTests.Adds_item"),
            GivenA.TestCase("Shop.Tests.CartTests.Removes_item")
        ]);
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.RunSelected());
        await session.DispatchAsync(new ExplorerCommand.MoveDown());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RerunLast());

        // Assert
        Assert.Equal(
        [
            "Shop.Tests.CartTests.Adds_item",
            "Shop.Tests.CartTests.Adds_item"
        ], backend.RunHistory.SelectMany(run => run).Select(test => test.FullyQualifiedName));
    }

    [Fact]
    public async Task It_runs_only_previously_failed_tests_when_rerunning_failures()
    {
        // Arrange
        var failed = GivenA.TestCase("Shop.Tests.CartTests.Adds_item");
        var passed = GivenA.TestCase("Shop.Tests.CartTests.Removes_item");
        var failure = GivenA.ResultFor(failed).Failed().Build();
        var backend = new InMemoryTestBackend(
            [failed, passed],
            new TestRun(false, "1 test failed", [failure]));
        var session = await GivenA.TestExplorer()
            .WithBackend(backend)
            .LoadedAsync();
        await session.DispatchAsync(new ExplorerCommand.MoveDown());
        await session.DispatchAsync(new ExplorerCommand.RunSelected());

        // Act
        await session.DispatchAsync(new ExplorerCommand.RerunFailed());

        // Assert
        Assert.Equal(
        [
            "run:Shop.Tests.CartTests.Adds_item,Shop.Tests.CartTests.Removes_item",
            "run:Shop.Tests.CartTests.Adds_item"
        ], backend.RunHistory.Select(run => $"run:{string.Join(',', run.Select(test => test.FullyQualifiedName))}"));
    }
}
