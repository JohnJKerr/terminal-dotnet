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
        Assert.Equal("dotnet", runner.LastRequest!.FileName);
        Assert.Equal(
            ["test", "/repo/Shop.sln", "--list-tests", "--nologo", "--tl:off"],
            runner.LastRequest.Arguments);
        Assert.Equal(
        [
            "Shop.Tests.CartTests.Adds_item",
            "Shop.Tests.CartTests.Removes_item"
        ], tests.Select(test => test.FullyQualifiedName));
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
            "test",
            "/repo/Shop.sln",
            "--filter",
            "FullyQualifiedName=Shop.Tests.CartTests.Adds_item|FullyQualifiedName=Shop.Tests.CartTests.Removes_item",
            "--logger",
            "trx;LogFileName=/tmp/terminal-dotnet.trx",
            "--nologo",
            "--tl:off"
        ], runner.LastRequest!.Arguments);
        Assert.True(run.Passed);
        Assert.Equal("2 tests passed", run.Output);
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
        var failure = Assert.Single(run.Results);
        Assert.Equal(TestOutcome.Failed, failure.Outcome);
        Assert.Equal("Expected total to be 10.", failure.ErrorMessage);
        Assert.Equal("/repo/CartTests.cs", failure.SourceFile);
        Assert.Equal(42, failure.SourceLine);
        Assert.Equal(TimeSpan.FromMilliseconds(12), failure.Duration);
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
