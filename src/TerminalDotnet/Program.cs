using TerminalDotnet.Explorer;
using TerminalDotnet.Terminal;
using TerminalDotnet.Testing;

var target = FindTarget(Environment.CurrentDirectory);
if (target is null)
{
    Console.Error.WriteLine("No .sln, .slnx, or .csproj file found in the current directory.");
    return 1;
}

var commandRunner = new ProcessCommandRunner();
var session = new TestExplorerSession(
    new DotnetCliTestBackend(commandRunner, new TemporaryTrxResultStore()),
    new FileSourceProvider());
try
{
    await session.LoadAsync(target);
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

var editor = Environment.GetEnvironmentVariable("EDITOR");
var editorLauncher = string.IsNullOrWhiteSpace(editor)
    ? null
    : new EditorLauncher(editor, commandRunner);
new TestRunnerApplication(session, target, editorLauncher).Run();
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
