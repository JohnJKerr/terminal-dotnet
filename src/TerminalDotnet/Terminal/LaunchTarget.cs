namespace TerminalDotnet.Terminal;

/// <summary>
/// The solution or project the app opens from the folder it was launched in.
/// A solution speaks for the projects beside it, and the XML solution for a
/// classic one it is replacing. Two of the same kind leave nothing to choose
/// between them, so the reader is told which rather than one being guessed.
/// </summary>
public abstract record LaunchTarget
{
    public sealed record Found(string Path) : LaunchTarget;

    public sealed record Ambiguous(IReadOnlyList<string> Candidates) : LaunchTarget;

    public sealed record Missing : LaunchTarget;

    private static readonly string[] Preference = [".slnx", ".sln", ".csproj"];

    public static IReadOnlyList<string> SearchPatterns => [.. Preference.Select(extension => $"*{extension}")];

    public static LaunchTarget From(IReadOnlyList<string> paths) => Preference
        .Select(extension => OfKind(paths, extension))
        .FirstOrDefault(candidates => candidates.Count > 0) switch
    {
        null => new Missing(),
        [var only] => new Found(only),
        var several => new Ambiguous(several)
    };

    private static IReadOnlyList<string> OfKind(IReadOnlyList<string> paths, string extension) => paths
        .Where(path => Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
        .Order(StringComparer.Ordinal)
        .ToArray();
}
