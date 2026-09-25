using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Testing;

public sealed class WhenARunProducesNoResults
{
    [Fact]
    public async Task It_keeps_the_output_the_command_produced()
    {
        // Arrange
        var backend = new DotnetCliTestBackend(
            new RecordingCommandRunner(new CommandResult(1, "error CS1002: ; expected", "")),
            new MissingTestResultStore());

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        Assert.Equal("error CS1002: ; expected", run.Output);
    }

    [Fact]
    public async Task It_reports_no_test_results()
    {
        // Arrange
        var backend = new DotnetCliTestBackend(
            new RecordingCommandRunner(new CommandResult(1, "error CS1002: ; expected", "")),
            new MissingTestResultStore());

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        Assert.Empty(run.Results);
    }

    [Fact]
    public async Task It_reports_why_the_results_could_not_be_read()
    {
        // Arrange
        var backend = new DotnetCliTestBackend(
            new RecordingCommandRunner(new CommandResult(0, "", "")),
            new MissingTestResultStore());

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        Assert.Contains("No results were written.", run.Diagnostic);
    }

    [Fact]
    public async Task It_keeps_the_output_when_the_results_cannot_be_read()
    {
        // Arrange
        var backend = new DotnetCliTestBackend(
            new RecordingCommandRunner(new CommandResult(1, "1 test failed", "")),
            new InMemoryTestResultStore("<TestRun"));

        // Act
        var run = await backend.RunAsync([AddsItem()]);

        // Assert
        Assert.Equal("1 test failed", run.Output);
    }

    private static TestCase AddsItem() =>
        new("Shop.Tests.CartTests.Adds_item", "Adds item", "/repo/Shop.sln");

    private sealed class MissingTestResultStore : ITestResultStore
    {
        public string CreatePath() => "/tmp/terminal-dotnet.trx";

        public Task<string> ReadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new FileNotFoundException("No results were written.", path));

        public void Discard(string path)
        {
        }
    }
}
