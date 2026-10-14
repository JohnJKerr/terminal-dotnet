using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace TerminalDotnet.Testing;

public sealed partial class DotnetCliTestBackend(
    ICommandRunner commandRunner,
    ITestResultStore resultStore) : ITestBackend
{
    /// <summary>Windows caps a command line at 32,767 characters, and the
    /// filter reaches vstest.console on its command line whichever way it is
    /// handed to <c>dotnet test</c>. A run whose filter would not fit is split
    /// into runs whose filters do, leaving room for the paths and switches
    /// the test task adds around it.</summary>
    private const int FilterBudget = 20_000;

    public async Task<IReadOnlyList<TestCase>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var request = DotnetProcess.Request(
            ["test", target, "--list-tests", "--no-build", .. DotnetProcess.OneProjectAtATimeSwitches()],
            Path.GetDirectoryName(Path.GetFullPath(target))!);
        var result = await commandRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0 || ListedNothing(result.StandardOutput))
        {
            throw new InvalidOperationException($"Test discovery failed: {Described(result)}");
        }

        return DiscoveredTests(result.StandardOutput.Split('\n'), target);
    }

    private static bool ListedNothing(string output) =>
        NeverListedASuite(output) || HostListedNoTest(output);

    private static bool NeverListedASuite(string output) =>
        !output.Contains("The following Tests are available:", StringComparison.Ordinal);

    private static bool HostListedNoTest(string output) =>
        output.Contains("No test is available", StringComparison.Ordinal) &&
        !output.Split('\n').Any(NamesATest);

    private static string Described(CommandResult result) =>
        string.Join(Environment.NewLine, OutputAndError(result));

    private static IEnumerable<string> OutputAndError(CommandResult result) =>
        new[] { result.StandardOutput, result.StandardError }
            .Select(part => part.Trim())
            .Where(part => part.Length > 0);

    private const string RunHeader = "Test run for ";

    private static IReadOnlyList<TestCase> DiscoveredTests(string[] lines, string target)
    {
        int[] starts = [0, .. lines.Index().Where(line => IsRunHeader(line.Item)).Select(line => line.Index)];
        return starts
            .Zip([.. starts.Skip(1), lines.Length], (start, end) => lines[start..end])
            .SelectMany(section => TestsListedIn(section, target))
            .DistinctBy(test => (test.FullyQualifiedName, test.DisplayName, test.ProjectPath))
            .ToArray();
    }

    private static IEnumerable<TestCase> TestsListedIn(string[] section, string target)
    {
        var project = section is [var header, ..] && IsRunHeader(header) ? TestTarget(header) ?? target : target;
        return section
            .SkipWhile(line => !line.Contains("The following Tests are available:", StringComparison.Ordinal))
            .Skip(1)
            .Where(NamesATest)
            .Select(line => DiscoveredTest(line.Trim(), project));
    }

    private static bool IsRunHeader(string line) => line.StartsWith(RunHeader, StringComparison.Ordinal);

    private static bool NamesATest(string line) =>
        line.Length > 4 && line.StartsWith("    ", StringComparison.Ordinal) &&
        !char.IsWhiteSpace(line[4]);

    private static TestCase DiscoveredTest(string discoveredName, string target)
    {
        var fullyQualifiedName = FullyQualifiedNameFrom(discoveredName);
        return new TestCase(
            fullyQualifiedName,
            DisplayNameFrom(discoveredName, fullyQualifiedName),
            target);
    }

    private static string FullyQualifiedNameFrom(string discoveredName)
    {
        var parameterStart = discoveredName.IndexOf('(');
        return parameterStart < 0 ? discoveredName : discoveredName[..parameterStart].TrimEnd();
    }

    private static string DisplayNameFrom(string discoveredName, string fullyQualifiedName)
    {
        var methodSeparator = fullyQualifiedName.LastIndexOf('.');
        return discoveredName[(methodSeparator + 1)..].Replace('_', ' ');
    }

    private static string? TestTarget(string header)
    {
        var framework = header.LastIndexOf(" (", StringComparison.Ordinal);
        return framework < RunHeader.Length ? null : ProjectTarget(header[RunHeader.Length..framework]);
    }

    private static string ProjectTarget(string assemblyPath)
    {
        var binSegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";
        var bin = assemblyPath.IndexOf(binSegment, StringComparison.OrdinalIgnoreCase);
        if (bin < 0)
        {
            return assemblyPath;
        }

        var projectDirectory = assemblyPath[..bin];
        var projectName = Path.GetFileNameWithoutExtension(assemblyPath);
        return Path.Combine(projectDirectory, $"{projectName}.csproj");
    }

    public Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken = default) =>
        RunAsync(tests, progress: null, cancellationToken);

    public async Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        IProgress<TestRunUpdate>? progress,
        CancellationToken cancellationToken = default)
    {
        if (tests.Count == 0)
        {
            throw new ArgumentException("At least one test is required.", nameof(tests));
        }

        var runs = new List<TestRun>();
        foreach (var project in tests.GroupBy(test => test.ProjectPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            runs.Add(await RunProjectAsync(
                project.Key,
                project.ToArray(),
                progress,
                cancellationToken).ConfigureAwait(false));
        }

        return Combined(runs);
    }

    private async Task<TestRun> RunProjectAsync(
        string target,
        IReadOnlyCollection<TestCase> tests,
        IProgress<TestRunUpdate>? progress,
        CancellationToken cancellationToken)
    {
        var batches = FilterBatches(tests).ToArray();
        var reported = new ReportedTail(progress);
        var runs = new List<TestRun>();
        for (var index = 0; index < batches.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reported.Label(BatchLabel(target, index, batches.Length));
            var build = runs.Count == 0;
            runs.Add(await RunBatchAsync(
                target,
                batches[index],
                build,
                reported.Append,
                cancellationToken).ConfigureAwait(false));
            reported.Flush();
        }

        return Combined(runs);
    }

    private static string BatchLabel(string target, int index, int batchCount) =>
        $"{Path.GetFileNameWithoutExtension(target)} (batch {index + 1} of {batchCount})";

    private static IEnumerable<IReadOnlyList<TestCase>> FilterBatches(IReadOnlyCollection<TestCase> tests)
    {
        var batch = new List<TestCase>();
        var length = 0;
        foreach (var test in tests)
        {
            var clauseLength = FilterClause(test).Length + 1;
            if (batch.Count > 0 && length + clauseLength > FilterBudget)
            {
                yield return batch;
                batch = [];
                length = 0;
            }

            batch.Add(test);
            length += clauseLength;
        }

        yield return batch;
    }

    private static string FilterClause(TestCase test) =>
        $"FullyQualifiedName={FilterValue(test.FullyQualifiedName)}";

    /// <summary>The results file is discarded however the run ends, because
    /// a run cancelled part-way can leave one behind that nobody will read.
    /// </summary>
    private async Task<TestRun> RunBatchAsync(
        string target,
        IReadOnlyList<TestCase> tests,
        bool build,
        Action<string> onOutput,
        CancellationToken cancellationToken)
    {
        var resultPath = resultStore.CreatePath();
        try
        {
            return await RunBatchIntoAsync(
                resultPath,
                target,
                tests,
                build,
                onOutput,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            resultStore.Discard(resultPath);
        }
    }

    private async Task<TestRun> RunBatchIntoAsync(
        string resultPath,
        string target,
        IReadOnlyList<TestCase> tests,
        bool build,
        Action<string> onOutput,
        CancellationToken cancellationToken)
    {
        var filter = string.Join('|', tests.Select(FilterClause));
        var result = await commandRunner.RunAsync(
            DotnetProcess.Request(
                [
                    "test",
                    target,
                    "--filter",
                    filter,
                    "--logger",
                    $"trx;LogFileName={resultPath}",
                    "--nologo",
                    "--tl:off",
                    "-nodeReuse:false",
                    DotnetProcess.CpuSwitch(),
                    .. BuildSwitches(build)
                ],
                Path.GetDirectoryName(Path.GetFullPath(target))!) with
            {
                OnOutput = onOutput,
                OutputLimit = TestOutput.TailLength
            },
            cancellationToken).ConfigureAwait(false);

        var output = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : $"{result.StandardOutput}{Environment.NewLine}{result.StandardError}";
        var recorded = await RecordedResultsAsync(resultPath, tests, cancellationToken).ConfigureAwait(false);
        return new TestRun(result.ExitCode == 0 && recorded.Results.Count > 0, TestOutput.Tail(output.Trim()), recorded.Results)
        {
            Diagnostic = recorded.Diagnostic
        };
    }

    /// <summary>The first part of a split run builds the project, so the
    /// parts after it test that same build rather than rebuilding it.</summary>
    private static string[] BuildSwitches(bool build) => build ? [] : ["--no-build"];

    private static TestRun Combined(IReadOnlyList<TestRun> runs) =>
        runs.Count == 1
            ? runs[0]
            : new TestRun(
                runs.All(run => run.Passed),
                TestOutput.Tail(string.Join(Environment.NewLine, runs.Select(run => run.Output))),
                runs.SelectMany(run => run.Results).ToArray())
            {
                Diagnostic = runs.Select(run => run.Diagnostic).FirstOrDefault(diagnostic => diagnostic is not null)
            };

    /// <summary>A test's name comes from the project being read, and the filter
    /// language gives meaning to some of its characters, so a name holding them
    /// would widen or break the filter. They are escaped as the filter syntax
    /// asks, and a generic type's comma is written the way VSTest expects.
    /// </summary>
    private static string FilterValue(string fullyQualifiedName) =>
        string.Concat(fullyQualifiedName.Select(EscapedFilterCharacter));

    private static string EscapedFilterCharacter(char character) => character switch
    {
        '\\' or '(' or ')' or '&' or '|' or '=' or '!' or '~' => $"\\{character}",
        ',' => "%2C",
        _ => character.ToString()
    };

    private sealed record RecordedResults(IReadOnlyList<TestResult> Results, string? Diagnostic);

    /// <summary>The run's outcomes come from the results file, so a file that
    /// is missing or unreadable is reported as such rather than swallowed:
    /// nothing else can tell an unrun test from a passing one.</summary>
    private async Task<RecordedResults> RecordedResultsAsync(
        string resultPath,
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken)
    {
        try
        {
            var trx = await resultStore.ReadAsync(resultPath, cancellationToken).ConfigureAwait(false);
            var results = ParseResults(trx, tests);
            return results.Count == 0
                ? new RecordedResults([], "No test outcomes were recorded for the selected tests.")
                : new RecordedResults(results, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new RecordedResults([], $"Could not read the test results: {exception.Message}");
        }
    }

    private static IReadOnlyList<TestResult> ParseResults(
        string trx,
        IReadOnlyCollection<TestCase> requestedTests)
    {
        var document = XDocument.Parse(trx);
        var definitions = document
            .Descendants()
            .Where(element => element.Name.LocalName == "UnitTest")
            .ToDictionary(
                element => (string)element.Attribute("id")!,
                element => element.Descendants().Single(child => child.Name.LocalName == "TestMethod"));

        var requestedByName = requestedTests
            .GroupBy(test => test.FullyQualifiedName, StringComparer.Ordinal)
            .ToDictionary(name => name.Key, name => name.ToArray(), StringComparer.Ordinal);

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "UnitTestResult")
            .Select(result => ToTestResult(result, definitions, requestedByName))
            .OfType<TestResult>()
            .ToArray();
    }

    private static TestResult? ToTestResult(
        XElement result,
        IReadOnlyDictionary<string, XElement> definitions,
        IReadOnlyDictionary<string, TestCase[]> requestedByName)
    {
        if (RequestedTestFor(result, definitions, requestedByName) is not { } test)
        {
            return null;
        }

        var stackTrace = ChildText(result, "StackTrace");
        var source = FailureSourceIn(stackTrace);
        return new TestResult(
            test,
            OutcomeOf(result),
            DurationOf(result),
            ChildText(result, "Message"),
            stackTrace,
            source?.File,
            source?.Line,
            ChildText(result, "StdOut"));
    }

    /// <summary>A theory reports a result per case under one test name, so
    /// the case is told apart by the display name it ran as.</summary>
    private static TestCase? RequestedTestFor(
        XElement result,
        IReadOnlyDictionary<string, XElement> definitions,
        IReadOnlyDictionary<string, TestCase[]> requestedByName)
    {
        var testId = (string?)result.Attribute("testId");
        if (testId is null || !definitions.TryGetValue(testId, out var definition))
        {
            return null;
        }

        var fullyQualifiedName = $"{definition.Attribute("className")?.Value}.{definition.Attribute("name")?.Value}";
        if (!requestedByName.TryGetValue(fullyQualifiedName, out var candidates))
        {
            return null;
        }

        var ranAs = result.Attribute("testName")?.Value.Replace('_', ' ');
        return candidates.FirstOrDefault(candidate =>
                ranAs?.EndsWith(candidate.DisplayName, StringComparison.Ordinal) == true)
            ?? candidates[0];
    }

    private static TestOutcome OutcomeOf(XElement result) => result.Attribute("outcome")?.Value switch
    {
        "Passed" => TestOutcome.Passed,
        "NotExecuted" => TestOutcome.Skipped,
        _ => TestOutcome.Failed
    };

    private static TimeSpan DurationOf(XElement result) =>
        TimeSpan.TryParse(result.Attribute("duration")?.Value, out var duration) ? duration : TimeSpan.Zero;

    private static string? ChildText(XElement element, string localName) =>
        element.Descendants().SingleOrDefault(child => child.Name.LocalName == localName)?.Value;

    private sealed record FailureSource(string File, int Line);

    private static FailureSource? FailureSourceIn(string? stackTrace) =>
        stackTrace is not null && SourceLocation().Match(stackTrace) is { Success: true } match
            ? new FailureSource(match.Groups["file"].Value, int.Parse(match.Groups["line"].Value))
            : null;

    [GeneratedRegex(@" in (?<file>.+):line (?<line>\d+)")]
    private static partial Regex SourceLocation();

    private sealed class ReportedTail(IProgress<TestRunUpdate>? progress)
    {
        private readonly object gate = new();
        private string label = "";
        private string tail = "";
        private bool reported;
        private DateTime reportedAt = DateTime.MinValue;

        public void Label(string next)
        {
            lock (gate)
            {
                label = next;
            }

            Report(force: true);
        }

        public void Append(string chunk)
        {
            lock (gate)
            {
                tail = TestOutput.Tail(tail + chunk);
            }

            Report(force: false);
        }

        public void Flush() => Report(force: true);

        private void Report(bool force)
        {
            TestRunUpdate? update = null;
            lock (gate)
            {
                var sinceReported = DateTime.UtcNow - reportedAt;
                if (!force && !TestOutput.ReportDue(reported, sinceReported))
                {
                    return;
                }

                if (label.Length == 0 && tail.Length == 0)
                {
                    return;
                }

                reported = true;
                reportedAt = DateTime.UtcNow;
                update = new TestRunUpdate(label, tail);
            }

            if (update is not null)
            {
                progress?.Report(update);
            }
        }
    }
}
