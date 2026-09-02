using System.Text.RegularExpressions;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Explorer;

public sealed record SourceContext(
    string Path,
    int StartLine,
    int HighlightLine,
    IReadOnlyList<string> Lines);

public interface ISourceProvider
{
    Task<SourceContext> ReadAsync(
        string path,
        int line,
        CancellationToken cancellationToken = default);
}

public interface ITestSourceLocator
{
    Task<SourceContext?> LocateAsync(
        TestCase test,
        CancellationToken cancellationToken = default);
}

public sealed class FileSourceProvider(int contextLines = 3) : ISourceProvider
{
    public async Task<SourceContext> ReadAsync(
        string path,
        int line,
        CancellationToken cancellationToken = default)
    {
        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        var startLine = Math.Max(1, line - contextLines);
        var length = Math.Min(lines.Length - startLine + 1, contextLines * 2 + 1);
        return new SourceContext(
            path,
            startLine,
            line,
            lines.Skip(startLine - 1).Take(length).ToArray());
    }
}

public sealed class FileTestSourceLocator(ISourceProvider sourceProvider) : ITestSourceLocator
{
    public async Task<SourceContext?> LocateAsync(
        TestCase test,
        CancellationToken cancellationToken = default)
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
            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
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
                return await sourceProvider.ReadAsync(path, methodLine + 1, cancellationToken);
            }
        }

        return null;
    }

    private static IEnumerable<string> SourceFiles(string projectDirectory) =>
        Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path, projectDirectory))
            .OrderBy(path => path, StringComparer.Ordinal);

    private static bool IsBuildOutput(string path, string projectDirectory)
    {
        var relative = Path.GetRelativePath(projectDirectory, path);
        var firstDirectory = relative.Split(Path.DirectorySeparatorChar)[0];
        return firstDirectory is "bin" or "obj";
    }

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
