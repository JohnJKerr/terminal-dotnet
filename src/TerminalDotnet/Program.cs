using TerminalDotnet.Explorer;
using TerminalDotnet.Testing;

var target = FindTarget(Environment.CurrentDirectory);
if (target is null)
{
    Console.Error.WriteLine("No .sln, .slnx, or .csproj file found in the current directory.");
    return 1;
}

var session = new TestExplorerSession(new DotnetCliTestBackend(new ProcessCommandRunner()));
try
{
    await session.LoadAsync(target);
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

while (true)
{
    Render(session.State, target);
    var key = Console.ReadKey(intercept: true);
    if (key.Key is ConsoleKey.Q || key.Key is ConsoleKey.Escape)
    {
        break;
    }

    ExplorerCommand? command = key switch
    {
        { Key: ConsoleKey.UpArrow } or { KeyChar: 'k' } => new ExplorerCommand.MoveUp(),
        { Key: ConsoleKey.DownArrow } or { KeyChar: 'j' } => new ExplorerCommand.MoveDown(),
        { KeyChar: 'r' } or { Key: ConsoleKey.Enter } => new ExplorerCommand.RunSelected(),
        _ => null
    };

    if (command is not null)
    {
        await session.DispatchAsync(command);
    }
}

Console.Clear();
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

static void Render(ExplorerState state, string target)
{
    Console.Clear();
    Console.WriteLine(" TerminalDotnet — THROWAWAY PROTOTYPE");
    Console.WriteLine($" {Path.GetFileName(target)}");
    Console.WriteLine(new string('─', Math.Max(20, Math.Min(Console.WindowWidth - 1, 80))));

    for (var index = 0; index < state.VisibleNodes.Count; index++)
    {
        var node = state.VisibleNodes[index];
        var cursor = index == state.SelectedIndex ? ">" : " ";
        var marker = node.Outcome switch
        {
            TestNodeOutcome.Running => "◌",
            TestNodeOutcome.Passed => "✓",
            TestNodeOutcome.Failed => "✗",
            _ when node.Kind == TestNodeKind.Test => "•",
            _ => "▼"
        };
        Console.WriteLine($"{cursor} {new string(' ', node.Depth * 2)}{marker} {node.Name}");
    }

    Console.WriteLine();
    Console.WriteLine(new string('─', Math.Max(20, Math.Min(Console.WindowWidth - 1, 80))));
    Console.WriteLine(state.Message);
    Console.WriteLine();
    Console.WriteLine(" ↑/k up   ↓/j down   r/Enter run subtree   q/Esc quit");
}
