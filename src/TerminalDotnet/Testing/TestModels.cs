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
    private (IReadOnlyList<TestResult> Results, TestRunSummary Summary) recorded = Summarize(Results);

    public IReadOnlyList<TestResult> Results
    {
        get => recorded.Results;
        init => recorded = Summarize(value);
    }

    public TestRun(bool passed, string output) : this(passed, output, [])
    {
    }

    /// <summary>Why the run's results could not be read. Set when the results
    /// file was missing or unreadable, which says nothing about how the
    /// individual tests went, whatever the exit code was.</summary>
    public string? Diagnostic { get; init; }

    public TestRunSummary Summary => recorded.Summary;

    private static (IReadOnlyList<TestResult>, TestRunSummary) Summarize(IReadOnlyList<TestResult> results)
    {
        var frozen = Snapshot.Of(results);
        var counts = frozen.CountBy(result => result.Outcome).ToDictionary();
        return (frozen, new TestRunSummary(
            CountOf(counts, TestOutcome.Passed),
            CountOf(counts, TestOutcome.Failed),
            CountOf(counts, TestOutcome.Skipped)));
    }

    private static int CountOf(IReadOnlyDictionary<TestOutcome, int> counts, TestOutcome outcome) =>
        counts.TryGetValue(outcome, out var count) ? count : 0;
}

public interface ITestBackend
{
    Task<IReadOnlyList<TestCase>> DiscoverAsync(string target, CancellationToken cancellationToken = default);

    Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken = default);
}
