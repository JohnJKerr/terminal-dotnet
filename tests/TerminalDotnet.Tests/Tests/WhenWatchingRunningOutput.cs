using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Builders;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenWatchingRunningOutput
{
    private static readonly TestCase Cart =
        new("Shop.CartTests.Adds_item", "Adds item", "/repo/Cart.Tests.csproj");

    private static readonly TestCase Orders =
        new("Shop.OrderTests.Submits_order", "Submits order", "/repo/Orders.Tests.csproj");

    [Fact]
    public async Task It_shows_output_from_the_project_that_is_still_running()
    {
        // Arrange
        var backend = new ReportingSecondProject(Cart, Orders);
        var session = await GivenA.TestExplorer().WithBackend(backend).LoadedAsync();

        // Act
        var run = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.Reported.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.EndsWith("Submits order running", session.State.RunningOutput);

        backend.Finish();
        await run;
    }

    [Fact]
    public async Task It_shows_the_output_as_soon_as_the_project_reports_it()
    {
        // Arrange
        var backend = new ReportingSecondProject(Cart, Orders);
        var session = await GivenA.TestExplorer().WithBackend(backend).LoadedAsync();
        var shownWhenReported = "";
        backend.OnReported = () => shownWhenReported = session.State.RunningOutput;

        // Act
        var run = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.Reported.Task.WaitAsync(TimeSpan.FromSeconds(2));
        backend.Finish();
        await run;

        // Assert
        Assert.EndsWith("Submits order running", shownWhenReported);
    }

    [Fact]
    public async Task It_keeps_the_output_of_the_projects_that_already_finished()
    {
        // Arrange
        var backend = new ReportingSecondProject(Cart, Orders);
        var session = await GivenA.TestExplorer().WithBackend(backend).LoadedAsync();

        // Act
        var run = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.Reported.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Assert
        Assert.Equal($"Adds item passed{Environment.NewLine}Submits order running", session.State.RunningOutput);

        backend.Finish();
        await run;
    }

    [Fact]
    public async Task It_labels_that_output_with_the_batch_in_progress()
    {
        // Arrange
        var backend = new ReportingSecondProject(Cart, Orders);
        var session = await GivenA.TestExplorer().WithBackend(backend).LoadedAsync();

        // Act
        var run = session.DispatchAsync(new ExplorerCommand.RunSelected());
        await backend.Reported.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var snapshot = TestPanelSnapshot.From(session.State, "/repo/Shop.sln");

        // Assert
        Assert.Equal("Test Output — Orders.Tests (batch 1 of 1)", snapshot.SelectedOutputTitle);

        backend.Finish();
        await run;
    }

    private sealed class ReportingSecondProject(TestCase first, TestCase second) : ITestBackend
    {
        private readonly TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Reported { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Action? OnReported { get; set; }

        public void Finish() => finished.TrySetResult();

        public Task<IReadOnlyList<TestCase>> DiscoverAsync(
            string target,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TestCase>>([first, second]);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            CancellationToken cancellationToken = default) =>
            RunAsync(tests, null, cancellationToken);

        public Task<TestRun> RunAsync(
            IReadOnlyCollection<TestCase> tests,
            IProgress<TestRunUpdate>? progress,
            CancellationToken cancellationToken = default)
        {
            if (tests.Any(test => test.ProjectPath == second.ProjectPath))
            {
                progress?.Report(new TestRunUpdate("Orders.Tests (batch 1 of 1)", "Submits order running"));
                OnReported?.Invoke();
                Reported.TrySetResult();
                return Hold(tests);
            }

            return Task.FromResult(Passed(tests, "Adds item passed"));
        }

        private async Task<TestRun> Hold(IReadOnlyCollection<TestCase> tests)
        {
            await finished.Task;
            return Passed(tests, "Submits order passed");
        }

        private static TestRun Passed(IReadOnlyCollection<TestCase> tests, string output) => new(
            true,
            output,
            [.. tests.Select(test => new TestResult(test, TestOutcome.Passed, TimeSpan.Zero, null, null, null, null))]);
    }
}
