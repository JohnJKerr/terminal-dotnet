namespace TerminalDotnet.Testing;

public sealed record TestCase(string FullyQualifiedName, string DisplayName, string ProjectPath)
{
    public string TestClass
    {
        get
        {
            var methodSeparator = FullyQualifiedName.LastIndexOf('.');
            return methodSeparator < 0 ? FullyQualifiedName : FullyQualifiedName[..methodSeparator];
        }
    }

    public string ClassName
    {
        get
        {
            var parts = FullyQualifiedName.Split('.');
            return parts.Length > 1 ? parts[^2] : FullyQualifiedName;
        }
    }
}

public enum TestOutcome
{
    Passed,
    Failed,
    Skipped
}

public sealed record TestResult(
    TestCase Test,
    TestOutcome Outcome,
    TimeSpan Duration,
    string? ErrorMessage,
    string? StackTrace,
    string? SourceFile,
    int? SourceLine,
    string? Output = null);

public sealed record TestRunSummary(int Passed, int Failed, int Skipped);

public sealed record TestRun(bool Passed, string Output, IReadOnlyList<TestResult> Results)
{
    public TestRun(bool passed, string output) : this(passed, output, [])
    {
    }

    /// <summary>Why the run's results could not be read. Set when the results
    /// file was missing or unreadable, which says nothing about how the
    /// individual tests went, whatever the exit code was.</summary>
    public string? Diagnostic { get; init; }

    public TestRunSummary Summary => new(
        CountOf(TestOutcome.Passed),
        CountOf(TestOutcome.Failed),
        CountOf(TestOutcome.Skipped));

    private int CountOf(TestOutcome outcome) => Results.Count(result => result.Outcome == outcome);
}

public interface ITestBackend
{
    Task<IReadOnlyList<TestCase>> DiscoverAsync(string target, CancellationToken cancellationToken = default);

    Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken = default);
}
