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
    public async Task It_escapes_filter_syntax_in_a_test_name()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, "", ""));
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));

        // Act
        await backend.RunAsync([new TestCase(@"Shop.Box<A,B>.Odd|Name=(x)&!~\y", "Odd", "/repo/Shop.sln")]);

        // Assert
        Assert.Equal(
            @"FullyQualifiedName=Shop.Box<A%2CB>.Odd\|Name\=\(x\)\&\!\~\\y",
            runner.LastRequest!.Arguments[3]);
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
    public async Task It_keeps_each_command_within_the_windows_command_line_limit()
    {
        // Arrange
        var runner = new QueuedCommandRunner();
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));

        // Act
        await backend.RunAsync(ManyLongNamedTests());

        // Assert
        Assert.All(runner.Requests, request => Assert.True(CommandLineLength(request) < WindowsCommandLineLimit));
    }

    [Fact]
    public async Task It_asks_for_every_test_across_a_split_run()
    {
        // Arrange
        var runner = new QueuedCommandRunner();
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));
        var tests = ManyLongNamedTests();

        // Act
        await backend.RunAsync(tests);

        // Assert
        Assert.Equal(
            tests.Select(test => $"FullyQualifiedName={test.FullyQualifiedName}"),
            runner.Requests.SelectMany(request => request.Arguments[3].Split('|')));
    }

    [Fact]
    public async Task It_reports_a_failed_run_when_any_part_of_a_split_run_fails()
    {
        // Arrange
        var runner = new QueuedCommandRunner(new CommandResult(0, "", ""), new CommandResult(1, "", ""));
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));

        // Act
        var run = await backend.RunAsync(ManyLongNamedTests());

        // Assert
        Assert.False(run.Passed);
    }

    [Fact]
    public async Task It_reads_the_results_of_every_part_of_a_split_run()
    {
        // Arrange
        var tests = ManyLongNamedTests();
        var backend = new DotnetCliTestBackend(
            new QueuedCommandRunner(),
            new InMemoryTestResultStore(PassingResultsFor(tests[0], tests[^1])));

        // Act
        var run = await backend.RunAsync(tests);

        // Assert
        Assert.Equal([tests[0], tests[^1]], run.Results.Select(result => result.Test));
    }

    [Fact]
    public async Task It_builds_only_once_across_a_split_run()
    {
        // Arrange
        var runner = new QueuedCommandRunner();
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));

        // Act
        await backend.RunAsync(ManyLongNamedTests());

        // Assert
        Assert.All(runner.Requests.Skip(1), request => Assert.Contains("--no-build", request.Arguments));
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
        var runner = new InMemoryCommandRunner(new CommandResult(0, $"""
            Test run for {TestPaths.In("Cart.Tests", "bin", "Debug", "net10.0", "Cart.Tests.dll")} (.NETCoreApp,Version=v10.0)
            The following Tests are available:
                Shop.Cart.Tests.CartTests.Adds_item(value: 1)
                Shop.Cart.Tests.CartTests.Adds_item(value: 2)
            Test run for {TestPaths.In("Order.Tests", "bin", "Debug", "net10.0", "Order.Tests.dll")} (.NETCoreApp,Version=v10.0)
            The following Tests are available:
                Shop.Order.Tests.OrderTests.Submits_order
            """, ""));
        var backend = new DotnetCliTestBackend(runner);

        // Act
        var tests = await backend.DiscoverAsync(TestPaths.In("Shop.sln"));

        // Assert
        Assert.Equal(
        [
            TestPaths.In("Cart.Tests", "Cart.Tests.csproj"),
            TestPaths.In("Cart.Tests", "Cart.Tests.csproj"),
            TestPaths.In("Order.Tests", "Order.Tests.csproj")
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
            new CommandResult(0, $"""
                Test run for {TestPaths.In("Cart.Tests", "bin", "Debug", "net10.0", "Cart.Tests.dll")} (.NETCoreApp,Version=v10.0)
                The following Tests are available:
                    Shop.Cart.Tests.CartTests.Adds_item
                """, ""),
            new CommandResult(0, "1 test passed", ""));
        var backend = new DotnetCliTestBackend(runner, new InMemoryTestResultStore("<TestRun />"));
        var test = (await backend.DiscoverAsync(TestPaths.In("Shop.sln"))).Single();

        // Act
        await backend.RunAsync([test]);

        // Assert
        Assert.Equal(TestPaths.In("Cart.Tests", "Cart.Tests.csproj"), runner.Requests[1].Arguments[1]);
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

    private const int WindowsCommandLineLimit = 32_767;

    private static TestCase[] ManyLongNamedTests() =>
        Enumerable.Range(0, 1_000)
            .Select(index => new TestCase(
                $"Konquest.Integration.Api.Tests.Orders.WhenPlacingAnOrderThroughTheApi.It_accepts_order_{index:D4}",
                $"It accepts order {index:D4}",
                "/repo/Api.IntegrationTests.csproj"))
            .ToArray();

    private static string PassingResultsFor(params TestCase[] tests)
    {
        var results = tests.Select((test, index) =>
            $"""<UnitTestResult testId="test-{index}" testName="{test.FullyQualifiedName}" outcome="Passed" />""");
        var definitions = tests.Select((test, index) =>
        {
            var method = test.FullyQualifiedName.LastIndexOf('.');
            return $"""<UnitTest id="test-{index}"><TestMethod className="{test.FullyQualifiedName[..method]}" name="{test.FullyQualifiedName[(method + 1)..]}" /></UnitTest>""";
        });
        return $"<TestRun><Results>{string.Concat(results)}</Results><TestDefinitions>{string.Concat(definitions)}</TestDefinitions></TestRun>";
    }

    private static int CommandLineLength(CommandRequest request) =>
        string.Join(' ', [request.FileName, .. request.Arguments.Select(argument => $"\"{argument}\"")]).Length;

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
            return Task.FromResult(remaining.TryDequeue(out var result) ? result : new CommandResult(0, "", ""));
        }
    }
}
