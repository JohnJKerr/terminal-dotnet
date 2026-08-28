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
