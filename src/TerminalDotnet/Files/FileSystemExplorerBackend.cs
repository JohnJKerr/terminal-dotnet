using System.Text.RegularExpressions;
using System.Xml.Linq;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Files;

public sealed partial class FileSystemExplorerBackend(ICommandRunner commandRunner) : IFileExplorerBackend
{
    private readonly GitFileListing listing = new(commandRunner);

    public async Task<IReadOnlyList<FileEntry>> DiscoverAsync(
        string target,
        CancellationToken cancellationToken = default)
    {
        var workingDirectory = Path.GetDirectoryName(Path.GetFullPath(target))!;
        var root = await listing.RootAsync(workingDirectory, cancellationToken).ConfigureAwait(false);
        var gitStatuses = await listing.StatusesAsync(root, cancellationToken).ConfigureAwait(false);
        var projectPaths = ProjectPaths(target);
        var tracked = await TrackedFilesAsync(projectPaths, cancellationToken).ConfigureAwait(false);
        return [.. projectPaths.SelectMany(projectPath => ProjectEntries(projectPath, tracked, gitStatuses))];
    }

    /// <summary>Git is asked once, from the folder every project sits
    /// beneath, rather than once per project: a solution of many projects
    /// would otherwise start a process for each on every reload.</summary>
    private async Task<IReadOnlyList<string>?> TrackedFilesAsync(
        IReadOnlyList<string> projectPaths,
        CancellationToken cancellationToken) =>
        SharedFolderOf(projectPaths.Select(ProjectDirectoryOf).ToArray()) is { } shared
            ? await listing.TrackedFilesUnderAsync(shared, cancellationToken).ConfigureAwait(false)
            : null;

    /// <summary>Outside git, each project's own folder is walked, rather than
    /// whatever else sits in the folder the projects share.</summary>
    private static IEnumerable<FileEntry> ProjectEntries(
        string projectPath,
        IReadOnlyList<string>? tracked,
        GitStatuses gitStatuses)
    {
        var projectDirectory = ProjectDirectoryOf(projectPath);
        var paths = tracked?.Where(path => GitFileListing.IsUnder(path, projectDirectory))
            ?? GitFileListing.FilesOnDisk(projectDirectory);
        return gitStatuses.EntriesFor(projectPath, projectDirectory, paths);
    }

    private static string ProjectDirectoryOf(string projectPath) => Path.GetDirectoryName(projectPath)!;

    /// <returns>The deepest folder holding every one of the folders, or null
    /// when they share none, such as projects on different drives.</returns>
    private static string? SharedFolderOf(IReadOnlyList<string> folders)
    {
        var shared = folders.FirstOrDefault();
        while (shared is not null && !folders.All(folder => IsWithin(folder, shared)))
        {
            shared = Path.GetDirectoryName(shared);
        }

        return shared;
    }

    private static bool IsWithin(string folder, string ancestor) =>
        folder == ancestor || folder.StartsWith(WithTrailingSeparator(ancestor), StringComparison.Ordinal);

    /// <summary>A drive's root already ends in a separator; every other folder
    /// is given one, so a sibling sharing its name as a prefix is not taken
    /// for something inside it.</summary>
    private static string WithTrailingSeparator(string folder) =>
        Path.EndsInDirectorySeparator(folder) ? folder : folder + Path.DirectorySeparatorChar;

    /// <summary>The solution is read like any other file from the repository,
    /// so one that is too large, or is not a file at all, lists no projects.
    /// </summary>
    private static IReadOnlyList<string> ProjectPaths(string target)
    {
        var fullTarget = Path.GetFullPath(target);
        if (HasExtension(fullTarget, ".csproj"))
        {
            return [fullTarget];
        }

        var solution = FileText.ReadWithin(fullTarget) ?? "";
        var directory = Path.GetDirectoryName(fullTarget)!;
        return HasExtension(fullTarget, ".slnx")
            ? XmlSolutionProjects(solution, directory)
            : ClassicSolutionProjects(solution, directory);
    }

    private static bool HasExtension(string path, string extension) =>
        Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<string> XmlSolutionProjects(string solution, string directory) =>
        solution.Length == 0
            ? []
            : XDocument.Parse(solution)
                .Descendants("Project")
                .Select(project => project.Attribute("Path")?.Value)
                .OfType<string>()
                .Select(path => ProjectPathFrom(path, directory))
                .ToArray();

    private static IReadOnlyList<string> ClassicSolutionProjects(string solution, string directory) =>
        solution.ReplaceLineEndings("\n")
            .Split('\n')
            .Select(line => SolutionProjectPath().Match(line))
            .Where(match => match.Success)
            .Select(match => ProjectPathFrom(match.Groups[1].Value, directory))
            .ToArray();

    private static string ProjectPathFrom(string declaredPath, string solutionDirectory) =>
        Path.GetFullPath(
            declaredPath.Replace('\\', Path.DirectorySeparatorChar),
            solutionDirectory);

    [GeneratedRegex("Project\\([^)]*\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.csproj)\"")]
    private static partial Regex SolutionProjectPath();
}
