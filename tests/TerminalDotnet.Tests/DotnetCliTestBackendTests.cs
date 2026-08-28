using TerminalDotnet.Testing;
using Xunit;

namespace TerminalDotnet.Tests;

public sealed class DotnetCliTestBackendTests
{
    [Fact]
    public async Task Discovery_issues_a_list_tests_command_and_returns_the_reported_tests()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(0, """
            Determining projects to restore...
            The following Tests are available:
                Shop.Tests.CartTests.Adds_item
                Shop.Tests.CartTests.Removes_item
            """, ""));
        var backend = new DotnetCliTestBackend(runner);

        // Act
        var tests = await backend.DiscoverAsync("/repo/Shop.sln");

        // Assert
        Assert.Equal(
        [
            "command:dotnet",
            "argument:test",
            "argument:/repo/Shop.sln",
            "argument:--list-tests",
            "argument:--nologo",
            "argument:--tl:off",
            "test:Shop.Tests.CartTests.Adds_item",
            "test:Shop.Tests.CartTests.Removes_item"
        ],
            [$"command:{runner.LastRequest!.FileName}",
                .. runner.LastRequest.Arguments.Select(argument => $"argument:{argument}"),
                .. tests.Select(test => $"test:{test.FullyQualifiedName}")]);
    }

    [Fact]
    public async Task Running_tests_issues_one_exact_filter_command()
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
            "argument:test",
            "argument:/repo/Shop.sln",
            "argument:--filter",
            "argument:FullyQualifiedName=Shop.Tests.CartTests.Adds_item|FullyQualifiedName=Shop.Tests.CartTests.Removes_item",
            "argument:--logger",
            "argument:trx;LogFileName=/tmp/terminal-dotnet.trx",
            "argument:--nologo",
            "argument:--tl:off",
            "passed:True",
            "output:2 tests passed"
        ],
            [.. runner.LastRequest!.Arguments.Select(argument => $"argument:{argument}"),
                $"passed:{run.Passed}",
                $"output:{run.Output}"]);
    }

    [Fact]
    public async Task A_failed_run_returns_structured_failure_details()
    {
        // Arrange
        var runner = new InMemoryCommandRunner(new CommandResult(1, "1 test failed", ""));
        var results = new InMemoryTestResultStore("""
            <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
              <Results>
                <UnitTestResult testId="test-1" testName="Adds_item" outcome="Failed" duration="00:00:00.012">
                  <Output>
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
            """);
        var backend = new DotnetCliTestBackend(runner, results);

        // Act
        var run = await backend.RunAsync(
        [
            new TestCase("Shop.Tests.CartTests.Adds_item", "Adds item", "/repo/Shop.sln")
        ]);

        // Assert
        var failure = run.Results.Single();
        Assert.Equal(
            (TestOutcome.Failed, "Expected total to be 10.", "/repo/CartTests.cs", 42, TimeSpan.FromMilliseconds(12)),
            (failure.Outcome, failure.ErrorMessage, failure.SourceFile, failure.SourceLine, failure.Duration));
    }

    [Fact]
    public async Task Discovery_retains_test_modules_and_normalizes_parameterized_test_identities()
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
            "/repo/Cart.Tests/bin/Debug/net10.0/Cart.Tests.dll|Shop.Cart.Tests.CartTests.Adds_item|Adds item(value: 1)",
            "/repo/Cart.Tests/bin/Debug/net10.0/Cart.Tests.dll|Shop.Cart.Tests.CartTests.Adds_item|Adds item(value: 2)",
            "/repo/Order.Tests/bin/Debug/net10.0/Order.Tests.dll|Shop.Order.Tests.OrderTests.Submits_order|Submits order"
        ], tests.Select(test => $"{test.ProjectPath}|{test.FullyQualifiedName}|{test.DisplayName}"));
    }

    [Theory]
    [InlineData("Shop.Tests.PriceTests.Accepts_price(value: 1.50)", "Shop.Tests.PriceTests.Accepts_price|Accepts price(value: 1.50)")]
    [InlineData("Shop.Tests.PriceTests.Accepts_price(1.50)", "Shop.Tests.PriceTests.Accepts_price|Accepts price(1.50)")]
    [InlineData("Shop.Tests.PriceTests.Accepts_price (1.50)", "Shop.Tests.PriceTests.Accepts_price|Accepts price (1.50)")]
    public async Task Discovery_preserves_parameter_values_in_the_display_name(
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
}
