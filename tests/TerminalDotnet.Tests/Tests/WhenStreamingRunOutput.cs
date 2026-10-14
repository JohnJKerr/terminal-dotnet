using TerminalDotnet.Testing;
using TerminalDotnet.Tests.Fakes;
using Xunit;

namespace TerminalDotnet.Tests.Tests;

public sealed class WhenStreamingRunOutput
{
    [Fact]
    public async Task It_names_the_batch_the_output_belongs_to()
    {
        // Arrange
        var reported = new List<TestRunUpdate>();
        var backend = new DotnetCliTestBackend(
            new StreamingCommandRunner("Adds item running"),
            new InMemoryTestResultStore());

        // Act
        await backend.RunAsync([Cart], new Progress<TestRunUpdate>(reported.Add));

        // Assert
        Assert.Equal("Cart.Tests (batch 1 of 1)", reported[^1].Label);
    }

    [Fact]
    public async Task It_keeps_only_the_tail_of_the_running_output()
    {
        // Arrange
        var written = new string('a', 8) + new string('b', TestOutput.TailLength);
        var reported = new List<TestRunUpdate>();
        var backend = new DotnetCliTestBackend(
            new StreamingCommandRunner(written),
            new InMemoryTestResultStore());

        // Act
        await backend.RunAsync([Cart], new Progress<TestRunUpdate>(reported.Add));

        // Assert
        Assert.Equal(new string('b', TestOutput.TailLength), reported[^1].Output);
    }

    [Fact]
    public void The_first_report_is_due_immediately()
    {
        // Act
        var due = TestOutput.ReportDue(reported: false, TimeSpan.Zero);

        // Assert
        Assert.True(due);
    }

    [Fact]
    public void A_further_report_waits_for_the_interval()
    {
        // Act
        var due = TestOutput.ReportDue(reported: true, TimeSpan.Zero);

        // Assert
        Assert.False(due);
    }

    [Fact]
    public void A_further_report_is_due_once_the_interval_has_passed()
    {
        // Act
        var due = TestOutput.ReportDue(reported: true, TestOutput.ReportInterval);

        // Assert
        Assert.True(due);
    }

    private static readonly TestCase Cart =
        new("Shop.CartTests.Adds_item", "Adds item", "/repo/Cart.Tests.csproj");

    private sealed class StreamingCommandRunner(string output) : ICommandRunner
    {
        public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken = default)
        {
            request.OnOutput?.Invoke(output);
            return Task.FromResult(new CommandResult(0, output, ""));
        }
    }
}
