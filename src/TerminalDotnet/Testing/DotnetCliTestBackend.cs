namespace TerminalDotnet.Testing;

public sealed class DotnetCliTestBackend(ICommandRunner commandRunner) : ITestBackend
{
    public async Task<IReadOnlyList<TestCase>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var request = new CommandRequest(
            "dotnet",
            ["test", target, "--list-tests", "--nologo", "--tl:off"],
            Path.GetDirectoryName(Path.GetFullPath(target))!);
        var result = await commandRunner.RunAsync(request, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Test discovery failed: {result.StandardError}");
        }

        var discovered = false;
        var tests = new List<TestCase>();
        foreach (var line in result.StandardOutput.Split('\n'))
        {
            if (line.Contains("The following Tests are available:", StringComparison.Ordinal))
            {
                discovered = true;
                continue;
            }

            if (!discovered || string.IsNullOrWhiteSpace(line) || !char.IsWhiteSpace(line[0]))
            {
                continue;
            }

            var fullyQualifiedName = line.Trim();
            tests.Add(new TestCase(
                fullyQualifiedName,
                fullyQualifiedName.Split('.')[^1].Replace('_', ' '),
                target));
        }

        return tests;
    }

    public async Task<TestRun> RunAsync(
        IReadOnlyCollection<TestCase> tests,
        CancellationToken cancellationToken = default)
    {
        if (tests.Count == 0)
        {
            throw new ArgumentException("At least one test is required.", nameof(tests));
        }

        var target = tests.First().ProjectPath;
        if (tests.Any(test => test.ProjectPath != target))
        {
            throw new ArgumentException("All tests in a run must have the same target.", nameof(tests));
        }

        var filter = string.Join(
            '|',
            tests.Select(test => $"FullyQualifiedName={test.FullyQualifiedName}"));
        var result = await commandRunner.RunAsync(
            new CommandRequest(
                "dotnet",
                ["test", target, "--filter", filter, "--nologo", "--tl:off"],
                Path.GetDirectoryName(Path.GetFullPath(target))!),
            cancellationToken);

        var output = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : $"{result.StandardOutput}{Environment.NewLine}{result.StandardError}";
        return new TestRun(result.ExitCode == 0, output.Trim());
    }
}
