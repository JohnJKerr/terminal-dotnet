using TerminalDotnet.Changes;
using TerminalDotnet.Explorer;
using TerminalDotnet.Files;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;

var target = FindTarget(Environment.CurrentDirectory);
if (target is null)
{
    Console.Error.WriteLine("No .sln, .slnx, or .csproj file found in the current directory.");
    return 1;
}

var commandRunner = new ProcessCommandRunner();
var fileSession = new FileExplorerSession(new FileSystemExplorerBackend(commandRunner));
var changesetSession = new ChangesetSession(new GitChangesetBackend(commandRunner));
var sourceProvider = new FileSourceProvider();
var session = new TestExplorerSession(
    new DotnetCliTestBackend(commandRunner, new TemporaryTrxResultStore()),
    sourceProvider,
    new FileTestSourceLocator(sourceProvider));
try
{
    await fileSession.LoadAsync(target);
    await changesetSession.LoadAsync(target);
    await session.LoadAsync(target);
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

var editor = Environment.GetEnvironmentVariable("VISUAL") ??
    Environment.GetEnvironmentVariable("EDITOR") ??
    "omarchy-launch-editor";
var editorLauncher = new EditorLauncher(editor, commandRunner);
new TestRunnerApplication(session, fileSession, changesetSession, target, editorLauncher).Run();
return 0;

static string? FindTarget(string directory)
{
    var candidates = Directory.EnumerateFiles(directory, "*.sln")
        .Concat(Directory.EnumerateFiles(directory, "*.slnx"))
        .Concat(Directory.EnumerateFiles(directory, "*.csproj"))
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();
    return candidates.Length == 1 ? candidates[0] : candidates.FirstOrDefault();
}
