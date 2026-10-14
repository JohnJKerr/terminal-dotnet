using System.Text.RegularExpressions;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Issues;

/// <summary>
/// The issues are read from a build's output. Every build compiles from
/// scratch: an incremental build skips a project that is already up to date,
/// and a project that is not compiled reports none of its warnings, so they
/// would vanish from the panel until its next edit.
/// </summary>
public sealed partial class DotnetBuildIssueBackend(ICommandRunner runner) : IIssueBackend
{
    public async Task<IReadOnlyList<CompilationIssue>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(target))!;
        var result = await runner.RunAsync(
            DotnetProcess.Request(
                ["build", target, "--no-incremental", .. DotnetProcess.Switches()],
                directory),
            cancellationToken).ConfigureAwait(false);
        var parsed = Lines(result.StandardOutput, result.StandardError)
            .Select(line => Parsed(line, directory))
            .OfType<CompilationIssue>()
            .Distinct()
            .ToList();
        if (result.ExitCode != 0 && parsed.All(issue => issue.Severity != IssueSeverity.Error))
        {
            parsed.Add(UnparsedFailure(result, directory));
        }

        return parsed;
    }

    private static IEnumerable<string> Lines(params string[] output) => output
        .SelectMany(text => text.Split('\n', StringSplitOptions.RemoveEmptyEntries));

    private static CompilationIssue? Parsed(string line, string directory)
    {
        var trimmed = line.Trim();
        var located = IssueLine().Match(trimmed);
        if (located.Success)
        {
            return LocatedIssue(located, directory);
        }

        var package = PackageLine().Match(trimmed);
        return package.Success ? PackageIssue(package, directory) : null;
    }

    private static CompilationIssue LocatedIssue(Match match, string directory)
    {
        var path = Path.GetFullPath(match.Groups["path"].Value, directory);
        return new CompilationIssue(
            path,
            Path.GetRelativePath(directory, path),
            int.Parse(match.Groups["line"].Value),
            int.Parse(match.Groups["column"].Value),
            match.Groups["code"].Value,
            match.Groups["message"].Value.Trim(),
            SeverityOf(match));
    }

    private static CompilationIssue PackageIssue(Match match, string directory)
    {
        var named = match.Groups["path"].Value;
        var path = named.Length == 0 ? directory : Path.GetFullPath(named, directory);
        return new CompilationIssue(
            path,
            named.Length == 0 ? "build" : Path.GetRelativePath(directory, path),
            0,
            0,
            match.Groups["code"].Value,
            match.Groups["message"].Value.Trim(),
            SeverityOf(match));
    }

    private static CompilationIssue UnparsedFailure(CommandResult result, string directory)
    {
        var reported = FirstReportedLine(result.StandardOutput, result.StandardError);
        return new CompilationIssue(
            directory,
            "build",
            0,
            0,
            "BUILD",
            reported.Length == 0 ? "Build failed." : reported,
            IssueSeverity.Error);
    }

    private static string FirstReportedLine(params string[] output) =>
        Lines(output).Select(line => line.Trim()).FirstOrDefault(line => line.Length > 0) ?? "";

    private static IssueSeverity SeverityOf(Match match) =>
        match.Groups["severity"].Value == "error" ? IssueSeverity.Error : IssueSeverity.Warning;

    [GeneratedRegex(@"^(?<path>.+)\((?<line>\d+),(?<column>\d+)\): (?<severity>error|warning) (?<code>[^: ]+): (?<message>.*?)(?: \[[^\]]+\])?$")]
    private static partial Regex IssueLine();

    [GeneratedRegex(@"^(?:(?<path>.+?)\s+:\s+)?(?<severity>error|warning)\s+(?<code>NU\d+):\s+(?<message>.+?)(?:\s+\[[^\]]+\])?$")]
    private static partial Regex PackageLine();
}
