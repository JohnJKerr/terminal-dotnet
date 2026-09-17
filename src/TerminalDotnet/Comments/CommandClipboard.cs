using System.ComponentModel;
using TerminalDotnet.Testing;

namespace TerminalDotnet.Comments;

/// <summary>
/// The system clipboard, reached through whichever tool the machine has. A
/// desktop carries one of these and not the others, and which one cannot be
/// told apart from a session that has no clipboard at all, so they are tried
/// in turn and a copy that lands nowhere is reported rather than claimed.
///
/// Their output is left alone: a tool that owns the selection forks and keeps
/// running to serve it, so reading its output to the end would wait for the
/// next thing to be copied.
/// </summary>
public sealed class CommandClipboard(ICommandRunner commandRunner, string workingDirectory)
    : ICommentClipboard
{
    private static readonly IReadOnlyList<ClipboardTool> Tools =
    [
        new("wl-copy", []),
        new("xclip", ["-selection", "clipboard"]),
        new("xsel", ["--clipboard", "--input"]),
        new("pbcopy", []),
        new("clip", [])
    ];

    public async Task<bool> TryCopyAsync(string text, CancellationToken cancellationToken = default)
    {
        foreach (var tool in Tools)
        {
            if (await TookAsync(tool, text, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> TookAsync(
        ClipboardTool tool,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await commandRunner.RunAsync(
                new CommandRequest(
                    tool.Executable,
                    tool.Arguments,
                    workingDirectory,
                    CaptureOutput: false)
                {
                    StandardInput = text
                },
                cancellationToken);
            return result.ExitCode == 0;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private sealed record ClipboardTool(string Executable, IReadOnlyList<string> Arguments);
}
