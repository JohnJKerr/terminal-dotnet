namespace TerminalDotnet.Tests.Builders;

/// <summary>
/// A folder of files laid out on disk for one test and removed after it.
/// Paths are written the way a reader says them, with forward slashes, and
/// laid out with the separator the platform uses.
/// </summary>
internal sealed class TemporaryWorkspace : IDisposable
{
    private TemporaryWorkspace(string root)
    {
        Root = root;
        Directory.CreateDirectory(root);
    }

    public string Root { get; }

    public static TemporaryWorkspace Create() =>
        new(Path.Combine(Path.GetTempPath(), $"terminal-dotnet-{Guid.NewGuid():N}"));

    /// <summary>A solution listing one project at src/App, with the project
    /// file itself in place.</summary>
    public static TemporaryWorkspace WithAppSolution() => Create()
        .WithFile("TerminalDotnet.slnx", "<Solution><Project Path=\"src/App/App.csproj\" /></Solution>")
        .WithFile("src/App/App.csproj", "<Project />");

    public string PathTo(string relativePath) =>
        Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public TemporaryWorkspace WithFile(string relativePath, string contents = "")
    {
        var path = PathTo(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return this;
    }

    public TemporaryWorkspace WithFolder(string relativePath)
    {
        Directory.CreateDirectory(PathTo(relativePath));
        return this;
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
