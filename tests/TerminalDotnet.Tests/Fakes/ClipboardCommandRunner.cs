using System.ComponentModel;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Tests.Fakes;

internal sealed class ClipboardCommandRunner : ICommandRunner
{
    public List<CommandRequest> Requests { get; } = [];

    /// <summary>The tools the machine has. One missing throws the way a
    /// missing executable does.</summary>
    public IReadOnlyList<string> Installed { get; init; } =
        ["wl-copy", "xclip", "xsel", "pbcopy"];

    public bool BrokenPipe { get; init; }

    /// <summary>The one tool that takes the text. The rest run and refuse.</summary>
    public string Takes { get; init; } = "wl-copy";

    public Task<CommandResult> RunAsync(
        CommandRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        if (!Installed.Contains(request.FileName))
        {
            throw new Win32Exception($"{request.FileName} not found");
        }

        if (BrokenPipe && request.FileName == "wl-copy")
        {
            throw new IOException("Broken pipe");
        }

        return Task.FromResult(
            new CommandResult(request.FileName == Takes ? 0 : 1, "", ""));
    }
}
