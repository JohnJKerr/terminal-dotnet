using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenUsingTheDotnetCliTestBackend
{
    [Fact]
    public async Task It_issues_a_list_tests_command_when_discovering()
    {
        // Arrange
        var runner = ListingCartTests();
        var backend = new DotnetCliTestBackend(runner);

        // Act
        await backend.DiscoverAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(
            ["dotnet", "test", "/repo/Shop.sln", "--list-tests", "--nologo", "--tl:off"],
            [runner.LastRequest!.FileName, .. runner.LastRequest.Arguments]);
    }

    [Fact]
    public async Task It_returns_the_reported_tests_when_discovering()
    {
        // Arrange
        var backend = new DotnetCliTestBackend(ListingCartTests());

        // Act
        var tests = await backend.DiscoverAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(
            ["Shop.Tests.CartTests.Adds_item", "Shop.Tests.CartTests.Removes_item"],
            tests.Select(test => test.FullyQualifiedName));
    }

    [Fact]
    public async Task It_issues_one_exact_filter_command_when_running_tests()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, "2 tests passed", ""));
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));

        // Act
        var run = await backend.RunAsync(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "/repo/Shop.sln"),
            new TestCase("Shop.Tests.CartTests.Removes_item", "Removes item", "/repo/Shop.sln")
        ]);

        // Assert
        Assert.Equal(
        [
            "test",
            "/repo/Shop.sln",
            "--filter",
            "FullyQualifiedName=Shop.Tests.CartTests.Adds_item|FullyQualifiedName=Shop.Tests.CartTests.Removes_item",
            "--logger",
            "trx;LogFileName=/tmp/terminal-dotnet.trx",
            "--nologo",
            "--tl:on"
        ],
            runner.LastRequest!.Arguments);
    }

    [Fact]
    public async Task It_reports_a_passing_run_when_the_command_succeeds()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, "2 tests passed", ""));
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        Assert.True(run.Passed);
    }

    [Fact]
    public async Task It_reports_what_the_command_wrote_when_running_tests()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, "2 tests passed", ""));
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        Assert.Equal("2 tests passed", run.Output);
    }

    [Fact]
    public async Task It_reads_the_outcome_of_a_failed_test()
    {
        // Arrange
        var backend = BackendWithAFailedRun();

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        var failure = run.Results.Single();
        Assert.Equal(TestOutcome.Failed, failure.Outcome);
    }

    [Fact]
    public async Task It_reads_the_message_a_failure_reported()
    {
        // Arrange
        var backend = BackendWithAFailedRun();

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        var failure = run.Results.Single();
        Assert.Equal("Expected total to be 10.", failure.ErrorMessage);
    }

    [Fact]
    public async Task It_reads_the_file_a_failure_came_from()
    {
        // Arrange
        var backend = BackendWithAFailedRun();

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        var failure = run.Results.Single();
        Assert.Equal("/repo/CartTests.cs", failure.SourceFile);
    }

    [Fact]
    public async Task It_reads_the_line_a_failure_came_from()
    {
        // Arrange
        var backend = BackendWithAFailedRun();

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        var failure = run.Results.Single();
        Assert.Equal(42, failure.SourceLine);
    }

    [Fact]
    public async Task It_reads_how_long_a_test_took()
    {
        // Arrange
        var backend = BackendWithAFailedRun();

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        var failure = run.Results.Single();
        Assert.Equal(TimeSpan.FromMilliseconds(12), failure.Duration);
    }

    [Fact]
    public async Task It_reads_the_output_a_test_wrote()
    {
        // Arrange
        var backend = BackendWithAFailedRun();

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        var failure = run.Results.Single();
        Assert.Equal("Cart total: 9", failure.Output);
    }

    [Fact]
    public async Task It_retains_the_module_each_test_belongs_to_when_discovering()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, """
            Test run for /repo/Cart.Tests/bin/Debug/net10.0/Cart.Tests.dll (.NETCoreApp,Version=v10.0)
            The following Tests are available:
                Shop.Cart.Tests.CartTests.Adds_item(value: 1)
                Shop.Cart.Tests.CartTests.Adds_item(value: 2)
            Test run for /repo/Order.Tests/bin/Debug/net10.0/Order.Tests.dll (.NETCoreApp,Version=v10.0)
            The following Tests are available:
                Shop.Order.Tests.OrderTests.Submits_order
            """, ""));
        var backend = new DotnetCliTestBackend(runner);

        // Act
        var tests = await backend.DiscoverAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(
        [
            "/repo/Cart.Tests/Cart.Tests.csproj",
            "/repo/Cart.Tests/Cart.Tests.csproj",
            "/repo/Order.Tests/Order.Tests.csproj"
        ], tests.Select(test => test.ProjectPath));
    }

    [Fact]
    public async Task It_normalizes_parameterized_identities_when_discovering()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, """
            Test run for /repo/Cart.Tests/bin/Debug/net10.0/Cart.Tests.dll (.NETCoreApp,Version=v10.0)
            The following Tests are available:
                Shop.Cart.Tests.CartTests.Adds_item(value: 1)
                Shop.Cart.Tests.CartTests.Adds_item(value: 2)
            """, ""));
        var backend = new DotnetCliTestBackend(runner);

        // Act
        var tests = await backend.DiscoverAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(
        [
            "Shop.Cart.Tests.CartTests.Adds_item|Adds item(value: 1)",
            "Shop.Cart.Tests.CartTests.Adds_item|Adds item(value: 2)"
        ], tests.Select(test => $"{test.FullyQualifiedName}|{test.DisplayName}"));
    }

    [Theory]
    [InlineData("Shop.Tests.PriceTests.Accepts_price(value: 1.50)", "Shop.Tests.PriceTests.Accepts_price|Accepts price(value: 1.50)")]
    [InlineData("Shop.Tests.PriceTests.Accepts_price(1.50)", "Shop.Tests.PriceTests.Accepts_price|Accepts price(1.50)")]
    [InlineData("Shop.Tests.PriceTests.Accepts_price (1.50)", "Shop.Tests.PriceTests.Accepts_price|Accepts price (1.50)")]
    public async Task It_preserves_parameter_values_in_the_display_name_when_discovering(
        string reportedName,
        string expected)
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, $"""
            The following Tests are available:
                {reportedName}
            """, ""));
        var backend = new DotnetCliTestBackend(runner);

        // Act
        var test = (await backend.DiscoverAsync("/repo/Shop.sln")).Single();

        // Assert
        Assert.Equal(expected, $"{test.FullyQualifiedName}|{test.DisplayName}");
    }

    [Fact]
    public async Task It_targets_the_owning_project_instead_of_the_assembly()
    {
        // Arrange
        var runner = new QueuedCommandRunner(
            new CommandResult(0, """
                Test run for /repo/Cart.Tests/bin/Debug/net10.0/Cart.Tests.dll (.NETCoreApp,Version=v10.0)
                The following Tests are available:
                    Shop.Cart.Tests.CartTests.Adds_item
                """, ""),
            new CommandResult(0, "1 test passed", ""));
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));
        var test = (await backend.DiscoverAsync("/repo/Shop.sln")).Single();

        // Act
        await backend.RunAsync([test]);

        // Assert
        Assert.Equal("/repo/Cart.Tests/Cart.Tests.csproj", runner.Requests[1].Arguments[1]);
    }

    [Fact]
    public async Task It_maps_parameterized_results_to_their_distinct_display_cases()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, "2 tests passed", ""));
        var results = new InMemoryTestResultStore("""
            <TestRun>
              <Results>
                <UnitTestResult testId="case-1" testName="Shop.Tests.PriceTests.Accepts_price(value: 1)" outcome="Passed" />
                <UnitTestResult testId="case-2" testName="Shop.Tests.PriceTests.Accepts_price(value: 2)" outcome="Passed" />
              </Results>
              <TestDefinitions>
                <UnitTest id="case-1"><TestMethod className="Shop.Tests.PriceTests" name="Accepts_price" /></UnitTest>
                <UnitTest id="case-2"><TestMethod className="Shop.Tests.PriceTests" name="Accepts_price" /></UnitTest>
              </TestDefinitions>
            </TestRun>
            """);
        var backend = new DotnetCliTestBackend(runner, results);

        // Act
        var run = await backend.RunAsync(
        [
            new TestCase("Shop.Tests.PriceTests.Accepts_price", "Accepts price(value: 1)", "/repo/Price.Tests.csproj"),
            new TestCase("Shop.Tests.PriceTests.Accepts_price", "Accepts price(value: 2)", "/repo/Price.Tests.csproj")
        ]);

        // Assert
        Assert.Equal(
        [
            "Accepts price(value: 1)",
            "Accepts price(value: 2)"
        ], run.Results.Select(result => result.Test.DisplayName));
    }

    private static InMemoryCommandRunner ListingCartTests() => new(new CommandResult(0, """
        Determining projects to restore...
        The following Tests are available:
            Shop.Tests.CartTests.Adds_item
            Shop.Tests.CartTests.Removes_item
        """, ""));

    private static DotnetCliTestBackend BackendWithAFailedRun() => new(
        new InMemoryCommandRunner(new CommandResult(1, "1 test failed", "")),
        new InMemoryTestResultStore("""
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
                <UnitTestResult testId="test-1" testName="Adds_item" outcome="Failed" duration="00:00:00.012">
                  <Output>
                    <StdOut>Cart total: 9</StdOut>
                    <ErrorInfo>
                      <Message>Expected total to be 10.</Message>
                      <StackTrace>at Shop.Tests.CartTests.Adds_item() in /repo/CartTests.cs:line 42</StackTrace>
                    </ErrorInfo>
                  </Output>
                </UnitTestResult>
              </Results>
              <TestDefinitions>
                <UnitTest id="test-1" name="Adds_item">
                  <TestMethod className="Shop.Tests.CartTests" name="Adds_item" />
                </UnitTest>
              </TestDefinitions>
            </TestRun>
            """));

    private static TestCase AddsItem() =>
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "/repo/Shop.sln");

    private sealed class InMemoryCommandRunner(CommandResult result) : ICommandRunner
    {
        public CommandRequest? LastRequest { get; private set; }

        public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private sealed class InMemoryTestResultStore(string contents) : ITestResultStore
    {
        public string CreatePath() => "/tmp/terminal-dotnet.trx";

        public Task<string> ReadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(contents);
    }

    private sealed class QueuedCommandRunner(params CommandResult[] results) : ICommandRunner
    {
        private readonly Queue<CommandResult> remaining = new(results);

        public List<CommandRequest> Requests { get; } = [];

        public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(remaining.Dequeue());
        }
    }
}
