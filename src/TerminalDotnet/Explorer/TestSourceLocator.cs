using System.Text.RegularExpressions;
using TerminalDotnet.Files;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Explorer;

public sealed record SourceLocation(string Path, int HighlightLine);

public interface ITestSourceLocator
{
    Task<SourceLocation?> LocateAsync(
        TestCase test,
        CancellationToken cancellationToken = default);
}

public sealed class FileTestSourceLocator : ITestSourceLocator
{
    public Task<SourceLocation?> LocateAsync(
        TestCase test,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Located(test, cancellationToken));

    private static SourceLocation? Located(TestCase test, CancellationToken cancellationToken)
    {
        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(test.ProjectPath));
        if (projectDirectory is null || !Directory.Exists(projectDirectory))
        {
            return null;
        }

        var parts = test.FullyQualifiedName.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        var className = parts[^2];
        var methodName = parts[^1];
        foreach (var path in SourceFiles(projectDirectory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (FileText.ReadLinesWithin(path) is not { } lines)
            {
                continue;
            }

            var classLine = LineMatching(lines, $@"\bclass\s+{Regex.Escape(className)}\b");
            if (classLine < 0)
            {
                continue;
            }

            var methodLine = LineMatching(
                lines,
                $@"\b{Regex.Escape(methodName)}\s*\(",
                classLine);
            if (methodLine >= 0)
            {
                return new SourceLocation(path, methodLine + 1);
            }
        }

        return null;
    }

    private static IEnumerable<string> SourceFiles(string projectDirectory) =>
        SourceTree.FilesUnder(projectDirectory, "*.cs")
            .OrderBy(path => path, StringComparer.Ordinal);

    private static int LineMatching(
        IReadOnlyList<string> lines,
        string pattern,
        int startIndex = 0)
    {
        for (var index = startIndex; index < lines.Count; index++)
        {
            if (Regex.IsMatch(lines[index], pattern))
            {
                return index;
            }
        }

        return -1;
    }
}
