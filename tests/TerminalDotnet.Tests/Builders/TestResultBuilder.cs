using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Builders;

/// <summary>A test's result: passed, instantly and silently, unless the test
/// says otherwise.</summary>
internal sealed class TestResultBuilder(TestCase test)
{
    private TestOutcome outcome = TestOutcome.Passed;
    private TimeSpan duration = TimeSpan.Zero;
    private string? message;
    private string? stackTrace;
    private string? sourceFile;
    private int? sourceLine;
    private string? output;

    public TestResultBuilder Failed(string because = "Failed")
    {
        outcome = TestOutcome.Failed;
        message = because;
        return this;
    }

    public TestResultBuilder Skipped()
    {
        outcome = TestOutcome.Skipped;
        return this;
    }

    public TestResultBuilder Taking(TimeSpan took)
    {
        duration = took;
        return this;
    }

    public TestResultBuilder WithStackTrace(string trace)
    {
        stackTrace = trace;
        return this;
    }

    public TestResultBuilder At(string file, int line)
    {
        sourceFile = file;
        sourceLine = line;
        return this;
    }

    public TestResultBuilder WithOutput(string written)
    {
        output = written;
        return this;
    }

    public TestResult Build() => new(test, outcome, duration, message, stackTrace, sourceFile, sourceLine, output);
}
